using UnityEngine;

public class BossRangeSettings : MonoBehaviour
{
    [Header("범위")]
    [Min(0f)]
    [SerializeField] private float detectionRange = 20f;
    [Min(0f)]
    [SerializeField] private float meleeRange = 7f;
    [Min(0f)]
    [SerializeField] private float rangedRange = 15f;

    public float DetectionRange => detectionRange;
    public float MeleeRange => meleeRange;
    public float RangedRange => rangedRange;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangedRange);
    }
}