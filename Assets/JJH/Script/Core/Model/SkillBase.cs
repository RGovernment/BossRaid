using UnityEngine;

public abstract class SkillBase
{
    public string Name;
    public int Id;
    public int Value;
    public int OtherValue;
    public int Cooldown;
    public int UpgradeType;
    public string Description;

    public SkillBase(int id, string name, int value, int cooldown, string description, int upgradeType = 0, int otherValue = 0)
    {
        Id = id;
        Name = name;
        Value = value;
        Cooldown = cooldown;
        Description = description;
        UpgradeType = upgradeType;
        OtherValue = otherValue;
    }

    public void Init()
    {

    }

    public void DamageTrigger()
    {

    }

    public void HealTrigger()
    {

    }

    public void DebuffTrigger()
    {

    }

    public void BuffTrigger()
    {

    }
}
