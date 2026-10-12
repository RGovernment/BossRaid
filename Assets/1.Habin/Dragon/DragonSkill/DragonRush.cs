using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DragonRush : MonoBehaviour
{
    private const float Half = 0.5f;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bossRoot;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject attackPrefab;

    [Header("돌진")]
    [Min(0.01f)]
    [SerializeField] private float rushDuration = 0.8f;

    [Header("준비")]
    [Min(0.01f)]
    [SerializeField] private float preparationTime = 2f;

    [Min(0.01f)]
    [SerializeField] private float minimumTargetDistance = 0.01f;

    [Header("장판")]
    [Min(0.01f)]
    [SerializeField] private float rushWidth = 4f;

    [SerializeField] private float telegraphHeightOffset = 0.03f;

    [Header("애니메이션")]
    [SerializeField]
    private string preparationState =
        "Base Layer.Idle";

    [SerializeField]
    private string rushState =
        "Base Layer.Run";

    [SerializeField]
    private string endState =
        "Base Layer.Idle";

    [Min(0f)]
    [SerializeField] private float transitionTime = 0.1f;

    [Header("테스트")]
    [SerializeField] private KeyCode testKey = KeyCode.R;

    private GameObject currentTelegraph;
    private Coroutine rushRoutine;
    private Tween moveTween;

    private bool isAttacking;
    private float attackHeight;
    private Vector3 rushDestination;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(testKey))
            Rush();
    }

    public void Rush()
    {
        if (isAttacking)
            return;

        if (!HasRequiredReferences())
            return;

        isAttacking = true;
        attackHeight = bossRoot.position.y;

        rushRoutine = StartCoroutine(RushRoutine());
    }

    private bool HasRequiredReferences()
    {
        if (animator == null ||
            bossRoot == null ||
            target == null ||
            attackPrefab == null)
        {
            Debug.LogError(
                "Animator, Boss Root, Target, Attack Prefab을 연결하세요.",
                this
            );

            return false;
        }

        return true;
    }

    private IEnumerator RushRoutine()
    {
        PlayAnimation(preparationState);
        currentTelegraph = Instantiate(attackPrefab);

        float elapsed = 0f;

        // 준비 중에는 타겟을 따라 경로를 갱신합니다.
        while (elapsed < preparationTime)
        {
            if (target == null)
            {
                FinishAttack();
                yield break;
            }

            UpdateAim();

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target == null)
        {
            FinishAttack();
            yield break;
        }

        // 이 시점의 플레이어 위치를 최종 목적지로 고정합니다.
        UpdateAim();

        PlayAnimation(rushState);

        moveTween = bossRoot.DOMove(
            rushDestination,
            rushDuration
        )
        .SetEase(Ease.Linear)
        .SetLink(gameObject);

        yield return moveTween.WaitForCompletion();

        FinishAttack();
    }

    private void UpdateAim()
    {
        Vector3 startPosition = bossRoot.position;
        startPosition.y = attackHeight;

        bossRoot.position = startPosition;

        rushDestination = target.position;
        rushDestination.y = attackHeight;

        Vector3 direction = rushDestination - startPosition;
        float distance = direction.magnitude;

        float yaw = bossRoot.eulerAngles.y;

        if (distance > minimumTargetDistance)
        {
            yaw = Mathf.Atan2(
                direction.x,
                direction.z
            ) * Mathf.Rad2Deg;
        }

        // X·Z 회전은 유지하고 Y축 방향만 변경합니다.
        Vector3 bossAngles = bossRoot.eulerAngles;
        bossAngles.y = yaw;
        bossRoot.eulerAngles = bossAngles;

        // 장판은 수평으로 배치합니다.
        Quaternion telegraphRotation =
            Quaternion.Euler(0f, yaw, 0f);

        Vector3 telegraphCenter =
            (startPosition + rushDestination) * Half;

        telegraphCenter.y =
            attackHeight + telegraphHeightOffset;

        currentTelegraph.transform.SetPositionAndRotation(
            telegraphCenter,
            telegraphRotation
        );

        // 기본 크기가 1인 장판 기준입니다.
        currentTelegraph.transform.localScale =
            new Vector3(rushWidth, 1f, distance);
    }

    private void PlayAnimation(string stateName)
    {
        animator.CrossFadeInFixedTime(
            stateName,
            transitionTime
        );
    }

    private void FinishAttack()
    {
        RemoveTelegraph();
        PlayAnimation(endState);

        moveTween = null;
        rushRoutine = null;
        isAttacking = false;
    }

    private void RemoveTelegraph()
    {
        if (currentTelegraph == null)
            return;

        Destroy(currentTelegraph);
        currentTelegraph = null;
    }

    private void OnDisable()
    {
        if (rushRoutine != null)
            StopCoroutine(rushRoutine);

        moveTween?.Kill();
        RemoveTelegraph();

        rushRoutine = null;
        moveTween = null;
        isAttacking = false;
    }
}