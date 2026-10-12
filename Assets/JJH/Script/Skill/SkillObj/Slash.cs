using UnityEngine;

public class Slash : SkillBase
{
    public Slash(int id, string name, int value, int cooldown, string description, int upgradeType = 0, int otherValue = 0) 
        : base(id, name, value, cooldown, description, upgradeType, otherValue)
    {
    }

    public override void DamageTrigger(int Damage)
    {
        
    }
}
