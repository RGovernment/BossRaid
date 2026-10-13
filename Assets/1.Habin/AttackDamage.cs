using UnityEngine;
using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;

public class AttackDamage : MonoBehaviour
{
    private LayerMask layerMask; //player
    [SerializeField] private DragonBoss boss;
    private void OnTriggerEnter(Collider other)
    {
        int ret = (other.gameObject.layer & layerMask);
        if (ret == 0)
            return;

        var tInfo = new tAttackInfo(transform.position, boss.Attack);
        //레이어 플레이어 
        other.GetComponent<Player>().TakeDamage(tInfo);
    }
}
