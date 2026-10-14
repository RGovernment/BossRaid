using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(AttackDamage))]
public class ShockwaveRing : MonoBehaviour
{
    private const float FullCircleRadians = Mathf.PI * 2f;
    private const float Half = 0.5f;

    [Header("고리")]
    [Min(3)]
    [SerializeField] private int segmentCount = 64;

    [Min(0f)]
    [SerializeField] private float startRadius = 1f;

    [Min(0.01f)]
    [SerializeField] private float endRadius = 20f;

    [Min(0.01f)]
    [SerializeField] private float ringWidth = 0.3f;

    [Header("높이")]
    [SerializeField] private float heightOffset = 0.15f;

    [Min(0.01f)]
    [SerializeField] private float attackHeight = 0.3f;

    [Header("확산")]
    [Min(0.01f)]
    [SerializeField] private float expandDuration = 3f;

    private LineRenderer lineRenderer;
    private AttackDamage attackDamage;
    private Vector3[] points;
    private Tween expandTween;
    private float previousRadius;

    public float EndRadius => endRadius;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        attackDamage = GetComponent<AttackDamage>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.widthMultiplier = 1f;
        lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, ringWidth);

        points = new Vector3[segmentCount];
        lineRenderer.positionCount = segmentCount;
    }

    public void SetBoss(DragonBoss boss)
    {
        attackDamage.SetBoss(boss);
    }

    private void Start()
    {
        attackDamage.ResetHits();
        previousRadius = startRadius;

        UpdateRing(startRadius);

        expandTween = DOVirtual.Float(startRadius, endRadius, expandDuration, UpdateRing).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).SetLink(gameObject).OnComplete(() => Destroy(gameObject));
    }

    private void UpdateRing(float radius)
    {
        DrawRing(radius);
        CheckPlayers(previousRadius, radius);

        previousRadius = radius;
    }

    private void DrawRing(float radius)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = FullCircleRadians * i / segmentCount;
            points[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -heightOffset);
        }

        lineRenderer.SetPositions(points);
    }

    private void CheckPlayers(float fromRadius, float toRadius)
    {
        float innerRadius = Mathf.Max(0f, Mathf.Min(fromRadius, toRadius) - ringWidth * Half);
        float outerRadius = Mathf.Max(fromRadius, toRadius) + ringWidth * Half;

        Vector3 center = transform.position + Vector3.up * heightOffset;
        Vector3 halfExtents = new Vector3(outerRadius, attackHeight * Half, outerRadius);

        Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, attackDamage.PlayerLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider playerCollider in colliders)
        {
            Bounds bounds = playerCollider.bounds;
            Vector3 offset = bounds.center - center;
            offset.y = 0f;
            float distance = offset.magnitude;
            float playerRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);

            bool outsideRing = distance - playerRadius > outerRadius;
            bool insidePassedArea = distance + playerRadius < innerRadius;

            if (outsideRing || insidePassedArea)
                continue;

            attackDamage.TryDamage(playerCollider);
        }
    }

    private void OnDestroy()
    {
        expandTween?.Kill();
    }
}