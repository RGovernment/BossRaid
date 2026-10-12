using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;

using SF = UnityEngine.SerializeField;
using HIn = UnityEngine.HideInInspector;

public class SkillSpawner : MonoBehaviour
{

    #region 베이스 데이터

    [Serializable]
    public struct EffectBase
    {
        public int id;
        public float hitTime;
        public GameObject prefab;
    }
    #endregion

    #region 직렬화 데이터
    [Header("플레이어 관련")]
    [SF] private Transform PlayerTransform;
    [SF] private Transform PlayerStaffTransformByHorizontal;
    [SF] private Transform PlayerStaffTransformByVertical;

    [Header("이펙트 관련")]
    [SF] private EffectBase[] prefabs;

    #endregion

    #region 필드 변수
    private Dictionary<int, IObjectPool<GameObject>> effectPool;

    [HIn] public bool ProjectileHit;
    private int skillIdCk;

    /// <summary>
    /// 스킬 사용 후, 적이 피격해 이펙트가 끝날 경우 작동
    /// </summary>
    public event Action OnEffectFinished;
    #endregion


    void Start()
    {
        effectPool = new();

        foreach (var prefab in prefabs)
        {
            int effectId = prefab.id;

            //오브젝트 풀링(각 스킬 별로 별도 생성)
            IObjectPool<GameObject> effect = new ObjectPool<GameObject>(
                createFunc: CreateProjectile,
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 5,
                maxSize: 10
            );

            effectPool[effectId] = effect;
        }
    }

    /// <summary>
    /// 오브젝트 풀링 전용 스킬 이펙트 생성 함수
    /// </summary>
    /// <returns></returns>
    private GameObject CreateProjectile()
    {
        EffectBase baseCk = new();

        foreach (var item in prefabs)
        {
            if (item.id == skillIdCk)
            {
                baseCk = item;
                break;
            }
        }

        GameObject dummy = Instantiate(baseCk.prefab, transform);

        return dummy;
    }

}
