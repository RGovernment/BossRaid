using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
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

    #endregion

    #region 필드 변수
    private Dictionary<int, IObjectPool<SkillBase>> effectPool;

    [HIn] public bool ProjectileHit;
    [SF] private int queuedSkillId;
    private SkillBase currentActiveSkill;
    private Vector3? targetPosition;
    #endregion


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

    public bool UseSkill(int id)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        return true;
    }

    public bool UseSkillAOE(int id, Vector3 pos)
    {
        if (currentActiveSkill != null) return false;

        queuedSkillId = id;
        targetPosition = pos;
        return true;
    }

    public void ReceiveAnimEvent(string eventName)
    {
        switch (eventName)
        {
            case "SkillOn":
                if (queuedSkillId != 0 && 
                    effectPool.TryGetValue(queuedSkillId, out var pool))
                {
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

        dummy.Init(id, effectPool[id]);

        return dummy;
    }

    private void EffectStart(SkillBase obj)
    {
        if(targetPosition != null)
            obj.transform.position = targetPosition.Value;
        obj.gameObject.SetActive(true);
        obj.rootParticle.Play(true);
        obj.transform.SetParent(null, true);
    }

    private void EffectEnd(SkillBase obj)
    {
        obj.rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        obj.gameObject.SetActive(false);
        if(obj.hitEffect != null)
            obj.hitEffect.gameObject.SetActive(false);
        obj.transform.SetParent(transform);
    }

}
