using UnityEngine;

public class SkillHitTrigger : MonoBehaviour
{
    private SkillBase parentSkill;

    private void Awake()
    {
        parentSkill = GetComponentInParent<SkillBase>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parentSkill == null) return;

        // 이후 플레이어 객체가 정해질 경우 보스만 피격되도록 변경
        if (other.TryGetComponent(out SkillBase.Player player))
        {
            parentSkill.DamageTrigger(player);
            parentSkill.HealTrigger(player);
            parentSkill.DebuffTrigger(player);
            parentSkill.BuffTrigger(player);

            parentSkill.hitEffect.transform.position = parentSkill.hitbox.bounds.center;
            parentSkill.hitEffect.gameObject.SetActive(true);
        }
    }
}
