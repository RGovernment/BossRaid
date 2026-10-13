using UnityEngine;

public class SlashWave : SkillBase
{
    public override void DamageTrigger(Player player)
    {
        int damage =
            Mathf.RoundToInt(player.atkPoint
            * (SkillDataLoader.Instance.SkillData[Id].Value / 100f));

        player.TakeDamage(damage);
        hitEffect.gameObject.SetActive(true);
    }
}
