using Unity.Behavior;
using UnityEngine;
using UnityEngine.Rendering;

public class DragonBoss : MonoBehaviour, ITakeDamgeable
{
    [Header("설정")]
    [SerializeField] private MonsterData monsterData;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private BehaviorGraphAgent behaviorAgent;
    [SerializeField] private Collider bodyCollider;

    [Header("사망 시 중단할 스킬")]
    [SerializeField] private MonoBehaviour[] skillScripts;

    [Header("사망 애니메이션")]
    [SerializeField] private string deathState = "Base Layer.Death";
    [SerializeField] private float deathTransitionTime = 0.1f;
    [SerializeField] private float deathAnimationSpeed = 1f;

    [Header("현재 상태")]
    [SerializeField] private float currentHp;
    [SerializeField] private float attack;

    private bool deathHandled;

    public float Attack => attack;
    public float CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0f;
    [Header("피격 테스트")]
    [SerializeField] private KeyCode damageTestKey = KeyCode.H;
    [SerializeField] private float testDamage = 100f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (behaviorAgent == null)
            behaviorAgent = GetComponent<BehaviorGraphAgent>();
        if (monsterData == null)
        {
            Debug.LogError("Monster Data를 연결하세요.", this);
            return;
        }
        currentHp = monsterData.MaxHp;
        attack = monsterData.Attack;
    }
    private void Update()
    {
        if (Input.GetKeyDown(damageTestKey))
            TakeDamage(new tAttackInfo(transform.position, testDamage));
    }
    public void TakeDamage(tAttackInfo attackInfo)
    {
        if (monsterData == null || IsDead)
            return;

        float damage = Mathf.Max(0f, attackInfo.Damage);
        currentHp = Mathf.Max(0f, currentHp - damage);

        Debug.Log($"보스 피격: {damage}, 남은 HP: {currentHp}", this);

        if (IsDead)
            Die();
    }

    //DragonBoss boss = other.GetComponentInParent<DragonBoss>();

    //boss.TakeDamage(tInfo);

    private void Die()
    {
        if (deathHandled)
            return;

        deathHandled = true;
        if (behaviorAgent != null)
            behaviorAgent.enabled = false;
        if (skillScripts != null)
        {
            foreach (MonoBehaviour skill in skillScripts)
            {
                if (skill != null)
                    skill.enabled = false;
            }
        }
        if (animator == null)
        {
            Debug.LogError("사망 모션 실패: Animator 연결 없음", this);
        }
        else
        {
            int deathHash = Animator.StringToHash(deathState);
            bool hasDeathState = animator.HasState(0, deathHash);
            if (hasDeathState)
            {
                animator.speed = deathAnimationSpeed;
                animator.Play(deathHash, 0, 0f);
            }
        }
        if (bodyCollider != null)
            bodyCollider.enabled = false;
        Debug.Log("보스 사망", this);
    }

}