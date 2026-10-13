using System.Collections.Generic;
using UnityEngine;
using SF = UnityEngine.SerializeField;

[RequireComponent(typeof(ParticleSystem))]
public class SkillProjectileTrigger : MonoBehaviour
{
    [SF] private SkillBase skillRoot;
    [SF] private ParticleSystem ps;
    private readonly List<ParticleCollisionEvent> collisionEvents = new();

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // 파티클이 무언가와 부딪혔을 때 유니티가 자동으로 실행하는 함수
    void OnParticleCollision(GameObject other)
    {
        if (other.TryGetComponent(out SkillBase.Player player))
        {
            skillRoot.DamageTrigger(player);
            skillRoot.HealTrigger(player);
            skillRoot.BuffTrigger(player);
            skillRoot.DebuffTrigger(player);

            int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);

            // 최초 충돌 위치만 가져옴
            if (numCollisionEvents > 0 && collisionEvents.Count > 0)
            {
                Vector3 hitPoint = collisionEvents[0].intersection;
                skillRoot.hitEffect.transform.position = hitPoint;
                skillRoot.hitEffect.gameObject.SetActive(true);
            }

        }

       
    }
}

