using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using SF = UnityEngine.SerializeField;
public class Lightning : SkillBase
{
    [SF] Animator animator;

    public override void OnEnable()
    {
        base.OnEnable();
        if (photonView == null) return;

        if (photonView.IsMine)
            animator.enabled = true;
        
        else
            animator.enabled = false;
        
    }

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
