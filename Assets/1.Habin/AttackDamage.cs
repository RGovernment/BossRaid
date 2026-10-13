using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;

public class AttackDamage : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private DragonBoss boss;


    private readonly HashSet<Player> hitPlayers = new HashSet<Player>();
    public void ResetHits()
    {
        hitPlayers.Clear();
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"HitBox 접촉: {other.name}");
        int ret = (1 << other.gameObject.layer) & layerMask.value;
        if (ret == 0 || boss == null)
            return;
        Player player = other.GetComponentInParent<Player>();

        if (player == null)
            return;
        if (!hitPlayers.Add(player))
            return;

        var tInfo = new tAttackInfo(transform.position, boss.Attack);
        player.TakeDamage(tInfo);
        Debug.Log($"대미지 피해 {tInfo.Damage} 위치:{tInfo.Pos}");
    }
    public void SetBoss(DragonBoss newBoss)
    {
        boss = newBoss;
    }
}
