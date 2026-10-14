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
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Pool;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI;
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
    #endregion

    #region 필드 변수

    public IReadOnlyList<SkillBase> useableSkillList;

    private Dictionary<int, IObjectPool<SkillBase>> effectPool;

    private SkillBase currentActiveSkill;
    private Vector3? targetPosition;
    private Vector3 networkRayPosition;

    private Dictionary<Player, SkillBase[]> playerCooldownGroup;

    public bool IsSkillActive => currentActiveSkill != null;

    public event Action OnSkillEnd; 
    private LayerMask rayMask;
    private int layerMask;

    #endregion
    private void Awake()
    {
        if(player != null)
            player = GetComponentInParent<Player>();
        useableSkillList = prefabs.ToList();
    }


    void Start()
    {
        effectPool = new();
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var item in PhotonNetwork.PlayerList)
            {

            }
        }
        

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

    public bool UseSkill(int id)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        //photonView.RPC("UseSkillOther", RpcTarget.Others, id);
        return true;
    }

    [PunRPC]
    public void UseSkillOther(int id)
    {
        queuedSkillId = id;
        //player.함수(id);
    }

    [PunRPC]
    public void UseSkillAOEOther(int id, Vector3 pos)
    {
        queuedSkillId = id;
        targetPosition = pos;
        //player.함수(id);
    }

    public bool UseSkillAOE(int id, Vector3 pos)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        targetPosition = pos;
        //photonView.RPC("UseSkillAOEOther", RpcTarget.Others, id, pos);
        return true;
    }

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
                            await UniTask.Delay(100);
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
