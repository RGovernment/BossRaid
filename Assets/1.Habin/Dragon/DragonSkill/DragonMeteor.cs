using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonMeteor : MonoBehaviour
{
    private const float RadiusToDiameter = 2f;

    [Header("참조")]
    [SerializeField] private Transform target;
    [SerializeField] private CircleTelegraph telegraphPrefab;
    [SerializeField] private MeteorProjectile meteorPrefab;

    [Header("운석 수")]
    [Min(1)]
    [SerializeField] private int meteorCount = 10;
    [Header("범위")]
    [Min(0f)]
    [SerializeField] private float scatterRadius = 10f;
    [Header("운석 장판넓이")]
    [Min(0.01f)]
    [SerializeField] private float impactRadius = 2f;
    [Header("높이")]
    [Min(0.01f)]
    [SerializeField] private float spawnHeight = 15f;

    [Header("예고시간")]
    [Min(0.01f)]
    [SerializeField] private float warningTime = 1.5f;
    [Header("간격")]
    [Min(0f)]
    [SerializeField] private float meteorInterval = 0.2f;
    [Header("장판 높이")]
    [SerializeField] private float telegraphHeightOffset = 0.03f;

    [Header("바닥 탐색")]
    [SerializeField] private LayerMask groundLayers;

    [Min(0.01f)]
    [SerializeField] private float groundSearchHeight = 30f;

    [Min(0.01f)]
    [SerializeField] private float groundSearchDepth = 60f;

    [Header("테스트")]
    [SerializeField] private KeyCode testKey = KeyCode.M;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string summonState = "Base Layer.Fire Head 2";
    [SerializeField] private string idleState = "Base Layer.Idle";
    [SerializeField] private float transitionTime = 0.1f;

    private readonly List<CircleTelegraph> telegraphs = new List<CircleTelegraph>();
    private readonly List<MeteorProjectile> meteors = new List<MeteorProjectile>();

    private Coroutine attackRoutine;
    private bool isAttacking;

    public bool IsAttacking => isAttacking;
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(testKey))
            Meteor();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void Meteor()
    {
        if (isAttacking)
            return;

        if (target == null || telegraphPrefab == null || meteorPrefab == null)
        {
            Debug.LogError("Target, Telegraph Prefab, Meteor Prefab을 연결하세요.", this);
            return;
        }

        isAttacking = true;
        animator.CrossFadeInFixedTime(summonState, transitionTime);

        // 공격 시작 순간의 어글자 위치를 중심으로 고정합니다.
        Vector3 center = target.position;
        attackRoutine = StartCoroutine(MeteorRoutine(center));
    }

    private IEnumerator MeteorRoutine(Vector3 center)
    {
        for (int i = 0; i < meteorCount; i++)
        {
            CreateWarning(center);

            if (i < meteorCount - 1 && meteorInterval > 0f)
                yield return new WaitForSeconds(meteorInterval);
        }

        // 모든 장판과 운석이 사라질 때까지 기다립니다.
        while (true)
        {
            telegraphs.RemoveAll(item => item == null);
            meteors.RemoveAll(item => item == null);

            if (telegraphs.Count == 0 && meteors.Count == 0)
                break;

            yield return null;
        }

        isAttacking = false;
        attackRoutine = null;

        Debug.Log("운석 공격 종료", this);
    }

    private void CreateWarning(Vector3 center)
    {
        Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
        Vector3 candidate = center + new Vector3(randomOffset.x, 0f, randomOffset.y);
        Vector3 rayOrigin = candidate + Vector3.up * groundSearchHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundSearchDepth, groundLayers, QueryTriggerInteraction.Ignore))
            return;

        Vector3 impactPoint = hit.point;

        CircleTelegraph telegraph = Instantiate(telegraphPrefab, impactPoint + Vector3.up * telegraphHeightOffset, Quaternion.identity);

        float diameter = impactRadius * RadiusToDiameter;
        telegraph.transform.localScale = new Vector3(diameter, 1f, diameter);

        telegraphs.Add(telegraph);

        telegraph.Play(warningTime, () => SpawnMeteor(telegraph, impactPoint));
    }

    private void SpawnMeteor(CircleTelegraph telegraph, Vector3 impactPoint)
    {
        if (!isAttacking || telegraph == null)
            return;

        Destroy(telegraph.gameObject);

        Vector3 spawnPosition = impactPoint + Vector3.up * spawnHeight;
        MeteorProjectile meteor = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);

        meteors.Add(meteor);
    }

    private void OnDisable()
    {
        isAttacking = false;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        foreach (CircleTelegraph telegraph in telegraphs)
        {
            if (telegraph != null)
                Destroy(telegraph.gameObject);
        }

        foreach (MeteorProjectile meteor in meteors)
        {
            if (meteor != null)
                Destroy(meteor.gameObject);
        }

        telegraphs.Clear();
        meteors.Clear();
        attackRoutine = null;
    }
    public void MeteorSummonEnd()
    {
        if (!isAttacking)
            return;

        animator.CrossFadeInFixedTime(idleState, transitionTime);
    }
}