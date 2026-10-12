using System;
using UnityEngine;
using UnityEngine.Pool;
using SF = UnityEngine.SerializeField;
public abstract class SkillBase : MonoBehaviour
{
    /// <summary>
    /// 더미용 플레이어 임시 객체
    /// </summary>
    public class Player : MonoBehaviour
    {
        public int atkPoint;
        public int hp;
        public void TakeDamage(int damage)
        {
            hp = Mathf.Max(0, hp - damage);
        }
    }

    public Collider hitbox;
    public ParticleSystem rootParticle;
    public IObjectPool<SkillBase> pool;
    public HitEffect hitEffect;
    public int Id;

    // 활성화/ 비활성화시 상위 이벤트에 구독

    public void Init(int id,IObjectPool<SkillBase> pool)
    {
        Id = id;
        this.pool = pool;
    }

    public void SkillEffectOn()
    {
        pool.Get();
    }

    public void SkillColliderOn()
    {
        hitbox.enabled = true;
    }

    public void SkillColliderOff()
    {
        hitbox.enabled = false;
    }

    /// <summary>
    /// 피해를 주는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void DamageTrigger(Player player)
    {

    }

    /// <summary>
    /// 회복 시키는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void HealTrigger(Player player)
    {

    }

    /// <summary>
    /// 디버프를 부여하는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void DebuffTrigger(Player player)
    {

    }

    /// <summary>
    /// 버프를 부여하는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void BuffTrigger(Player player)
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
        {
            DamageTrigger(player);
            HealTrigger(player);
            DebuffTrigger(player);
            BuffTrigger(player);
        }
    }

    private void OnParticleSystemStopped()
    {
        pool.Release(this);
    }
}
