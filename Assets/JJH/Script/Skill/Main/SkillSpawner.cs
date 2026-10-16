using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Hovl;
using Photon.Pun;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using TMPro;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using static Constant_JJH;
using static Enums_JJH;

using HIn = UnityEngine.HideInInspector;
using SF = UnityEngine.SerializeField;

public class SkillSpawner : MonoBehaviourPunCallbacks
{
    #region 직렬화 데이터
    [Header("플레이어 관련")]
    [SF] private Transform PlayerTransform;
    [SF] private Transform PlayerWeaponTransform;

    [Header("이펙트 관련")]
    [SF] private SkillBase[] prefabs;
    [HIn] public bool ProjectileHit;
    [SF] private int queuedSkillId;
    [SF] private Player player;
    [SF] private LayerMask rayMask;
    #endregion

    #region 필드 변수

    public Dictionary<int, Dictionary<int, float>> skillCooldownList;
    public Dictionary<int, (float cooldown, bool isDelay)> localCooldownList;
    public List<Player> allSkillUseablePlayers;

    public IReadOnlyList<SkillBase> useableSkillList;

    private Dictionary<int, IObjectPool<SkillBase>> effectPool;

    private SkillBase currentActiveSkill;
    private Vector3? targetPosition;
    private Vector3 networkRayPosition;
    private int[] cachedSkillIds;
    public bool IsSkillActive => currentActiveSkill != null;

    private CancellationTokenSource cooldownLoopToken;
    private CancellationTokenSource localCooldownLoopToken;

    /// <summary>
    /// 스킬 종료시 호출 되는 이벤트 콜백
    /// </summary>
    public event Action OnSkillEnd;
    private event Action OnJoined;

    #endregion

    private void Awake()
    {
        if(player == null)
            player = GetComponentInParent<Player>();
        useableSkillList = prefabs.ToList();
        skillCooldownList = new();
        localCooldownList = new();
    }

    private void Start()
    {
        effectPool = new();

        foreach (var prefab in prefabs)
        {
            int effectId = prefab.Id;

            int currentId = effectId;
            // 오브젝트 풀링 (각 스킬 별로 별도 생성)
            IObjectPool<SkillBase> poolInstance = new ObjectPool<SkillBase>(
                createFunc: () => CreateSkillEffect(currentId),
                actionOnGet: EffectStart,
                actionOnRelease: EffectEnd,
                actionOnDestroy: (obj) => Destroy(obj.gameObject),
                defaultCapacity: 5,
                maxSize: 10
            );

            effectPool[effectId] = poolInstance;
        }

        if (PhotonNetwork.IsMasterClient && player.IsMine)
        {
            skillCooldownList[photonView.OwnerActorNr] 
                = useableSkillList.ToDictionary(x => x.Id, x => 0f);
        }

        localCooldownList = useableSkillList.ToDictionary(x => x.Id, x => (0f, false));
        cachedSkillIds 
            = SkillDataLoader.Instance.SkillDataList.Select(x => x.Id).ToArray();
        if (PhotonNetwork.IsMasterClient && player.photonView.IsMine)
            StartCooldownSyncLoop();
        if(player.photonView.IsMine)
            StartLocalCooldownSyncLoop();

        // 마을에서 전투 씬으로 변경되는게 확정되면 변경
        NotifyMySceneLoaded().Forget();
    }

    public void StartCooldownSyncLoop()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        StopCooldownSyncLoop();
        cooldownLoopToken = new CancellationTokenSource();

        SyncCooldownLoopAsync(cooldownLoopToken.Token).Forget();
    }

    public void StartLocalCooldownSyncLoop()
    {
        StopLocalCooldownSyncLoop();
        localCooldownLoopToken = new CancellationTokenSource();

        SyncLocalCooldownLoopAsync(localCooldownLoopToken.Token).Forget();
    }

    public void StopCooldownSyncLoop()
    {
        if (cooldownLoopToken == null) return;
        cooldownLoopToken.Cancel();
        cooldownLoopToken.Dispose();
        cooldownLoopToken = null;
    }

    public void StopLocalCooldownSyncLoop()
    {
        if (localCooldownLoopToken == null) return;
        localCooldownLoopToken.Cancel();
        localCooldownLoopToken.Dispose();
        localCooldownLoopToken = null;
    }

    private async UniTaskVoid SyncCooldownLoopAsync(CancellationToken token)
    {
        TimeSpan delayTimeSpan = TimeSpan.FromMilliseconds(COOLDOWN_RECIVE_TIMER);
        float minusCount = COOLDOWN_RECIVE_TIMER / 1000f;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(delayTimeSpan, cancellationToken: token);

                if (!PhotonNetwork.IsMasterClient) break;

                for (int i = allSkillUseablePlayers.Count - 1; i >= 0; i--)
                {
                    Player actor = allSkillUseablePlayers[i];
                    if (actor == null || actor.photonView == null) continue;

                    int actorNr = actor.photonView.OwnerActorNr;

                    // 포톤 룸에서 유저 존재 여부 및 마스터 딕셔너리 키 검증
                    var photonPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNr);
                    if (photonPlayer == null) continue;
                    if (!skillCooldownList.ContainsKey(actorNr)) continue;

                    for (int j = 0; j < cachedSkillIds.Length; j++)
                    {
                        int skillId = cachedSkillIds[j];

                        if (!skillCooldownList[actorNr].ContainsKey(skillId)) continue;

                        float currentCooldown = skillCooldownList[actorNr][skillId];
                        Debug.Log($"마스터({actorNr}) : {skillId} / {currentCooldown}");
                        if (currentCooldown > 0f)
                        {
                            skillCooldownList[actorNr][skillId] -= minusCount;

                            if (skillCooldownList[actorNr][skillId] < 0f)
                                skillCooldownList[actorNr][skillId] = 0f;
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("쿨타임 동기화 루프가 정상 취소되었습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"루프 에러: {ex.Message}");
        }
    }
    private async UniTaskVoid SyncLocalCooldownLoopAsync(CancellationToken token)
    {
        TimeSpan delayTimeSpan = TimeSpan.FromMilliseconds(COOLDOWN_RECIVE_TIMER);

        float minusCount = COOLDOWN_RECIVE_TIMER / 1000f;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(delayTimeSpan, cancellationToken: token);

                foreach (var skill in useableSkillList)
                {
                    int skillId = skill.Id;
                    float currentCooldown = localCooldownList[skillId].cooldown;

                    if (currentCooldown > 0f)
                    {
                        float nowTime = currentCooldown - minusCount;

                        if (nowTime < 0f)
                        {
                            if (PhotonNetwork.IsMasterClient)
                            {
                                localCooldownList[skillId] = (nowTime, false);
                            }
                            else if(!localCooldownList[skillId].isDelay)
                            {
                                localCooldownList[skillId] = (COOLDOWN_DELAY_DEADLINE, true);

                                photonView.RPC("MasterCooldownCheck",
                                    RpcTarget.MasterClient, skillId,
                                    player.photonView.OwnerActorNr);
                            }
                        }
                        else
                            localCooldownList[skillId] = (nowTime, false);
                    }
                }
                
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("쿨타임 동기화 루프가 정상 취소되었습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"루프 에러: {ex.Message}");
        }
    }

    public async UniTaskVoid NotifyMySceneLoaded()
    {
        await UniTask.Delay(COOLDOWN_RECIVE_TIMER);

        player.photonView.RPC("PlayerLoadSet", RpcTarget.MasterClient);
    }

    public void PlayerLoadSet()
    {
        PlayerSkillCooldownLoad();
    }

    public void PlayerSkillCooldownLoad()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            allSkillUseablePlayers = FindObjectsByType<Player>(FindObjectsSortMode.None).ToList();
            Dictionary<int , Dictionary<int, float>> masterList = new();
            foreach (var item in allSkillUseablePlayers)
            {
                if (item.photonView.IsMine)
                    masterList = item.SkillSpawner.skillCooldownList;
            }

            foreach (var item in allSkillUseablePlayers)
            {
                if (masterList.Count > 0 &&
                    masterList.ContainsKey(item.photonView.OwnerActorNr)) 
                    continue;

                masterList[item.photonView.OwnerActorNr]
                    = item.SkillSpawner.useableSkillList.ToDictionary(x => x.Id, x => 0f);
            }
        }
    }

    public void MasterCooldownSet(int acNr, int id, float cooldown)
    {
        foreach (var item in allSkillUseablePlayers)
        {
            if (item.photonView.IsMine && item.SkillSpawner
                .skillCooldownList.TryGetValue(acNr, out Dictionary<int, float> value))
            {
                Debug.Log($"{acNr} : {value}[{id}] = {cooldown}");
                value[id] = cooldown;
                break;
            }
        }
    }

    public void MasterCooldownCheck(int id, int acNr)
    {
        foreach (var item in allSkillUseablePlayers)
        {
            if (item.photonView.IsMine)
            {
                if (item.SkillSpawner.skillCooldownList
                    .TryGetValue(acNr, out Dictionary<int, float> data)&& 
                    data
                    .TryGetValue(id, out float cooldown))
                    photonView.RPC("IsSkillUseable",
                        PhotonNetwork.CurrentRoom.GetPlayer(acNr), id, cooldown <= 0f);
                break;
            }
        }
    }

    public void IsSkillUseable(int id, bool useable)
    {
        if (useable)
            localCooldownList[id] = (0, false);
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        /// 플레이어 리스트 내 제거
        Player temp = null;
        foreach (var item in allSkillUseablePlayers)
        {
            if (item.photonView.OwnerActorNr == otherPlayer.ActorNumber)
                temp = item; break;
        }

        if (temp != null) 
            allSkillUseablePlayers.Remove(temp);
        

        int targetKey = -1;

        // 일치 여부 확인
        foreach (var item in skillCooldownList.Keys)
        {
            if (item  == otherPlayer.ActorNumber)
            {
                targetKey = item;
                break;
            }
            
        }

        // 일치하는 캐릭터 스킬 리스트 제거 (마스터만)
        if (targetKey > -1)
            skillCooldownList.Remove(targetKey);
    }

    /// <summary>
    /// 스킬 사용 시 호출할 함수
    /// </summary>
    /// <param name="id">스킬 id</param>
    /// <returns></returns>
    public bool UseSkill(int id, int acNr = -1)
    {
        if (currentActiveSkill != null) return false;
        
        if (localCooldownList.TryGetValue(id, out (float cooldown, bool isDelay) cool) 
            && cool.cooldown > 0f) {
            Debug.Log(cool.cooldown);
            return false;
        }

        queuedSkillId = id;
        float cooldown = SkillDataLoader.Instance.SkillData[id].Cooldown;

        if(player.IsMine)
            localCooldownList[id] = (cooldown, false);

        photonView.RPC("UseSkillOther", RpcTarget.Others, id, acNr);
        photonView.RPC("MasterCooldownSet", RpcTarget.MasterClient, acNr, id, cooldown);
        return true;
    }

    /// <summary>
    /// 스킬 사용시 호출할 함수(type : 대상 지정형 범위 스킬)
    /// </summary>
    /// <param name="id">스킬 id</param>
    /// <param name="pos">스킬이 사용될 위치</param>
    /// <returns></returns>
    public bool UseSkillAOE(int id, Vector3 pos, int acNr = -1)
    {
        if (currentActiveSkill != null) return false;
        if (localCooldownList.TryGetValue(id, out (float cooldown, bool isDelay) cool) 
            && cool.cooldown > 0f)
        {
            Debug.Log(cool.cooldown);
            return false;
        }

        queuedSkillId = id;
        targetPosition = pos;
        float cooldown = SkillDataLoader.Instance.SkillData[id].Cooldown;

        if (player.IsMine)
            localCooldownList[id] = (cooldown, false);

        photonView.RPC("UseSkillAOEOther", RpcTarget.Others, id, pos, acNr);
        photonView.RPC("MasterCooldownSet", RpcTarget.MasterClient, acNr, id, cooldown);
        return true;
    }

    /// <summary>
    /// 스킬 사용 시 사용한 스킬의 Id를 네트워크에 전송하는 함수
    /// </summary>
    /// <param name="id"></param>
    public void UseSkillOther(int id, int acNr)
    {
        if (acNr < 0) return;
        queuedSkillId = id;
    }

    /// <summary>
    /// 스킬 사용 시 사용한 스킬의 Id와 위치를 네트워크에 전송하는 함수
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    public void UseSkillAOEOther(int id, Vector3 pos, int acNr)
    {
        if(acNr < 0) return;
        queuedSkillId = id;
        targetPosition = pos;
    }


    /// <summary>
    /// 애니메이션 클립 이벤트를 호출받는 함수
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public async UniTask ReceiveAnimEvent(string eventName)
    {
        switch (eventName)
        {
            case "SkillOn":
                if (queuedSkillId != 0 && 
                    effectPool.TryGetValue(queuedSkillId, out var pool))
                {
                    
                    int projectiles = SkillDataLoader.Instance.SkillData[queuedSkillId].projectileCount;

                    if (projectiles > 1)
                    {
                        for(int i = 0; i < projectiles; i++)
                        {
                            await UniTask.Delay(PROJECTILE_SKILL_GAP_TIMER);
                            currentActiveSkill = pool.Get();
                        }
                    }
                    else
                        currentActiveSkill = pool.Get();
                }
                break;

            case "ColliderOn":
                if (currentActiveSkill != null && queuedSkillId != 0)
                    currentActiveSkill.SkillColliderOn();
                break;

            case "ColliderOff":
                if (currentActiveSkill != null)
                    currentActiveSkill.SkillColliderOff();
                break;
            case "SkillOff":
                currentActiveSkill = null;
                queuedSkillId = 0;
                targetPosition = null;
                OnSkillEnd?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 피격 등 기타 사유로 인해 스킬이 강제 취소되었을 경우 초기화 함수
    /// </summary>
    public void SkillUseUnLock()
    {
        currentActiveSkill = null;
        queuedSkillId = 0;
    }

    /// <summary>
    /// 오브젝트 풀링 전용 스킬 이펙트 생성 함수
    /// </summary>
    /// <returns></returns>
    private SkillBase CreateSkillEffect(int id)
    {
        SkillBase baseCk = null;
        foreach (var item in prefabs)
        {
            if (item.Id == id)
            {
                baseCk = item;
                break;
            }
        }

        if (baseCk == null)
        {
            Debug.LogError($"[SkillSpawner] 프리팹 배열에서 ID {id}를 찾을 수 없습니다.");
            return null;
        }

        SkillBase dummy = Instantiate(baseCk, transform);

        dummy.Init(id, effectPool[id], player);
        if(dummy.TryGetComponent(out Lightning com))
            com.AnimatorCk();
        
        return dummy;
    }

    private void EffectStart(SkillBase obj)
    {
        if (obj.TryGetComponent(out IProjectile projectile)) 
            obj.transform.position = PlayerWeaponTransform.position;
        else if (targetPosition != null)
            obj.transform.position = targetPosition.Value;
        obj.transform.SetParent(null, true);
        obj.gameObject.SetActive(true);
        obj.rootParticle.Play(true);
        

        if (projectile != null)
        {
            Vector3 finalRayPosition;
            if (player.photonView.IsMine)
            {
                if (Camera.main == null)
                {
                    Debug.LogWarning("메인 카메라를 찾을 수 없습니다.");
                    return;
                }
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                Vector3 rayPosition;
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, rayMask))
                    rayPosition = hit.point;
                else
                    rayPosition = ray.origin + (ray.direction * 100f);

                finalRayPosition = rayPosition;

                player.photonView.RPC("ShootProjectileOther", RpcTarget.Others, rayPosition);
            }
            else
            {
                if (networkRayPosition == Vector3.zero)
                    finalRayPosition = transform.position + (transform.forward * 100f);
                else
                    finalRayPosition = networkRayPosition;
                
            }

            projectile.Shoot(finalRayPosition);

            if (!photonView.IsMine) networkRayPosition = Vector3.zero;
        }

    }

    public void ShootProjectileOther(Vector3 rayPosition)
    {
        networkRayPosition = rayPosition;
    }

    private void EffectEnd(SkillBase obj)
    {
        obj.rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        obj.gameObject.SetActive(false);
        if(obj.hitEffect != null)
            obj.hitEffect.gameObject.SetActive(false);
        obj.transform.SetParent(transform);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private void OnDestroy()
    {
        StopCooldownSyncLoop();
        StopLocalCooldownSyncLoop();
    }

}
