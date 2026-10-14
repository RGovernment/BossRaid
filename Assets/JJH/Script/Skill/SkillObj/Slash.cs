using Photon.Realtime;
using Unity.VisualScripting;
using UnityEngine;

public class Slash : SkillBase
{
    public override void DamageTrigger(ITakeDamgeable target, Player player)
    {
        int damage =
            Mathf.RoundToInt(player.ObjectInfo.CurrentHP
            * (SkillDataLoader.Instance.SkillData[Id].Value / 100f));

        tAttackInfo info = new()
        {
            Pos = Vector3.zero,
            Damage = damage
        };

        target.TakeDamage(info);
        hitEffect.gameObject.SetActive(true);
    }
}
