using UnityEngine;
using static Constant_JJH;

public class SkillHitTrigger : MonoBehaviour
{
    private SkillBase parentSkill;
    private LayerMask bossLayer;

    private void Awake()
    {
        parentSkill = GetComponentInParent<SkillBase>();
        bossLayer = LayerMask.NameToLayer(BOSS_TAG);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(BOSS_TAG)) return;
        if (parentSkill == null) { Debug.LogWarning("스킬 정보가 없습니다."); return; }

        // 이후 플레이어 객체가 정해질 경우 보스만 피격되도록 변경
        if (other.TryGetComponent(out ITakeDamgeable target) && 
            other.gameObject.layer == bossLayer)
        {
            parentSkill.DamageTrigger(target, parentSkill.UsePlayer);
            parentSkill.HealTrigger(parentSkill.UsePlayer);
            parentSkill.DebuffTrigger(parentSkill.UsePlayer);
            parentSkill.BuffTrigger(parentSkill.UsePlayer);

            if(parentSkill.hitEffect != null)
            {
                parentSkill.hitEffect.transform.position = other.transform.position;
                parentSkill.hitEffect.gameObject.SetActive(true);
            }   
        }
    }
}
