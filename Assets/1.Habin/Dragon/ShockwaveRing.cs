using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ShockwaveRing : MonoBehaviour
{
    private const float FullCircleRadians = Mathf.PI * 2f;

    [Header("고리")]
    [Min(3)]
    [SerializeField] private int segmentCount = 64;
    [Header("시작 고리 반경")]
    [Min(0f)]
    [SerializeField] private float startRadius = 1f;
    [Header("최대 반경")]
    [Min(0.01f)]
    [SerializeField] private float endRadius = 20f;
    [Header("고리 두께")]
    [Min(0.01f)]
    [SerializeField] private float ringWidth = 0.3f;
    [Header("높이")]
    [SerializeField] private float heightOffset = 0.15f;
    [Header("확산")]
    [Min(0.01f)]
    [SerializeField] private float expandDuration = 3f;

    private LineRenderer lineRenderer;
    private Vector3[] points;
    private Tween expandTween;

    public float EndRadius => endRadius;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.widthMultiplier = 1f;
        lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, ringWidth);

        points = new Vector3[segmentCount];
        lineRenderer.positionCount = segmentCount;
    }

    private void Start()
    {
        DrawRing(startRadius);

        expandTween = DOVirtual.Float(startRadius, endRadius, expandDuration, DrawRing).SetEase(Ease.Linear).SetLink(gameObject).OnComplete(() => Destroy(gameObject));
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

    private void OnDestroy()
    {
        expandTween?.Kill();
    }
}