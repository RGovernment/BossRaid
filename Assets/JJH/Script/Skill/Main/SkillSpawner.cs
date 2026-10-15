using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Hovl;
using Photon.Pun;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using Unity.AppUI.UI;
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

    public Dictionary<Player, Dictionary<int, int>> skillCooldownList;

    public IReadOnlyList<SkillBase> useableSkillList;

    private Dictionary<int, IObjectPool<SkillBase>> effectPool;

    private SkillBase currentActiveSkill;
    private Vector3? targetPosition;
    private Vector3 networkRayPosition;

    public bool IsSkillActive => currentActiveSkill != null;

    /// <summary>
    /// 스킬 종료시 호출 되는 이벤트 콜백
    /// </summary>
    public event Action OnSkillEnd;
    private event Action OnJoined;
    

    private float skillCooldown;
    // 임시
    public Player GetPlayer => player;

    #endregion
    private void Awake()
    {
        if(player == null)
            player = GetComponentInParent<Player>();
        useableSkillList = prefabs.ToList();
        skillCooldownList = new();

        foreach (var skill in useableSkillList)
            // 스킬 쿨타임은 0초로 초기화해 등록
            skillCooldownList[player]= new()
            {
                [skill.Id] = 0
            };
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && skillCooldown > 0f)
        {
            skillCooldown -= Time.deltaTime;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        // 유니티 씬 로드 이벤트 연결
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnJoined += NewPlayerSkillCooldownObserve;
    }

    public void NewPlayerSkillCooldownObserve()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Player[] allPlayerScripts = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var item in allPlayerScripts)
            {
                if (item.TryGetComponent(out PhotonView view) && view.IsMine) continue;

                if (skillCooldownList.Count > 0 && 
                    skillCooldownList.ContainsKey(item)) 
                    continue;
                IReadOnlyList<SkillBase> skills = useableSkillList;


                skillCooldownList[item] = useableSkillList.ToDictionary(x => x.Id, x => 0);
            }
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        // 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnJoined -= NewPlayerSkillCooldownObserve;
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player targetKey = null;

        // 일치 여부 확인
        foreach (var item in skillCooldownList.Keys)
        {
            if (item != null && item.TryGetComponent(out PhotonView view))
            {
                if (view.Owner == otherPlayer)
                {
                    targetKey = item;
                    break;
                }
            }
        }

        // 일치하는 캐릭터 제거 (마스터만)
        if (targetKey != null)
            skillCooldownList.Remove(targetKey);
        
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneType.BattleScene.ToString())
        {
            OnJoined?.Invoke();
        }
    }

    void Start()
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
    }
    #region 이전 버전용
    /// <summary>
    /// 스킬 사용 시 호출할 함수
    /// </summary>
    /// /// <param name="player">요청자</param>
    /// <param name="id">스킬 id</param>
    /// <returns></returns>
    public bool UseSkill(int id)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        photonView.RPC("UseSkillOther", RpcTarget.Others, id);
        return true;
    }

    /// <summary>
    /// 스킬 사용시 호출할 함수(type : 대상 지정형 범위 스킬)
    /// </summary>
    /// /// <param name="player">요청자</param>
    /// <param name="id">스킬 id</param>
    /// <param name="pos">스킬이 사용될 위치</param>
    /// <returns></returns>
    public bool UseSkillAOE(int id, Vector3 pos)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        targetPosition = pos;
        photonView.RPC("UseSkillAOEOther", RpcTarget.Others, id, pos);
        return true;
    }
    #endregion


    /// <summary>
    /// 스킬 사용 시 호출할 함수
    /// </summary>
    /// /// <param name="player">요청자</param>
    /// <param name="id">스킬 id</param>
    /// <returns></returns>
    public bool UseSkill(Player player, int id)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        //photonView.RPC("UseSkillOther", RpcTarget.Others, id);
        return true;
    }

   

    /// <summary>
    /// 스킬 사용시 호출할 함수(type : 대상 지정형 범위 스킬)
    /// </summary>
    /// /// <param name="player">요청자</param>
    /// <param name="id">스킬 id</param>
    /// <param name="pos">스킬이 사용될 위치</param>
    /// <returns></returns>
    public bool UseSkillAOE(Player player, int id, Vector3 pos)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        targetPosition = pos;
        //photonView.RPC("UseSkillAOEOther", RpcTarget.Others, id, pos);
        return true;
    }

    /// <summary>
    /// 스킬 사용 시 사용한 스킬의 Id를 네트워크에 전송하는 함수
    /// </summary>
    /// <param name="id"></param>
    [PunRPC]
    public void UseSkillOther(int id)
    {
        queuedSkillId = id;
        //player.함수(id);
    }

    /// <summary>
    /// 스킬 사용 시 사용한 스킬의 Id와 위치를 네트워크에 전송하는 함수
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    [PunRPC]
    public void UseSkillAOEOther(int id, Vector3 pos)
    {
        queuedSkillId = id;
        targetPosition = pos;
        //player.함수(id);
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
                    // 투사체가 여러개일 경우 0.1초의 간격으로 발사
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
/*            if (photonView.IsMine)
            {*/
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

                //photonView.RPC("ShootProjectileOther", RpcTarget.Others, rayPosition);
/*            }
            else
            {
                if (networkRayPosition == Vector3.zero)
                    finalRayPosition = transform.position + (transform.forward * 100f);
                else
                    finalRayPosition = networkRayPosition;
                
            }*/

            projectile.Shoot(finalRayPosition);

            //if (!photonView.IsMine) networkRayPosition = Vector3.zero;
        }

    }

    [PunRPC]
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

}
