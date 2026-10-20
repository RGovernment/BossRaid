using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EnergyBeam : MonoBehaviour
{
    [Header("연결 위치")]
    [SerializeField] private Transform sourcePoint;
    [SerializeField] private Transform targetPoint;

    [Header("두께")]
    [Min(0f)]
    [SerializeField] private float beamWidth = 0.15f;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
    }

    private void LateUpdate()
    {
        if (sourcePoint == null || targetPoint == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, sourcePoint.position);
        lineRenderer.SetPosition(1, targetPoint.position);
    }

    private void OnDisable()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
    public void SetTarget(Transform target)
    {
        targetPoint = target;
    }
}