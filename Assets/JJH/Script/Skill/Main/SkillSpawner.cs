using Photon.Pun;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
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
        obj.gameObject.SetActive(true);
        obj.rootParticle.Play(true);
        obj.hitEffect.gameObject.SetActive(true);
    }

    private void EffectEnd(SkillBase obj)
    {
        obj.rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        obj.gameObject.SetActive(false);
        obj.hitEffect.gameObject.SetActive(false);
    }

}
