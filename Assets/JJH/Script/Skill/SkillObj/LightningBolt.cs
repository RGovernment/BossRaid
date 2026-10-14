using UnityEngine;
using SF = UnityEngine.SerializeField;
public class LightningBolt : SkillBase, IProjectile
{
    [Header("직렬화 컴포넌트")]
    [SF] private Rigidbody rb;
    [SF] private float projectileSpeed = 20;

    public void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEnable()
    {
        Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0);

        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        Vector3 targetPosition;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            targetPosition = hit.point;
        else
            targetPosition = ray.origin + (ray.direction * 100f);

        Vector3 direction = targetPosition.normalized;

        Move(direction, projectileSpeed);
    }

    public override void DamageTrigger(Player player)
    {
        int damage =
            Mathf.RoundToInt(player.atkPoint
            * (SkillDataLoader.Instance.SkillData[Id].Value / 100f));

        player.TakeDamage(damage);
        hitEffect.gameObject.SetActive(true);
    }

    public void Move(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
    }
}
