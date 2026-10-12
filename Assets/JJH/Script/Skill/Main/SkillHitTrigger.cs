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

        // 닿은 충돌체(other) 정보를 부모의 특정 함수로 토스해줍니다.
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
