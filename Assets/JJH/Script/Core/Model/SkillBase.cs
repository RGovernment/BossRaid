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

    /// <summary>
    /// 피해를 주는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void DamageTrigger(int value)
    {

    }

    /// <summary>
    /// 회복 시키는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void HealTrigger(int value)
    {

    }

    /// <summary>
    /// 디버프를 부여하는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void DebuffTrigger()
    {

    }

    /// <summary>
    /// 버프를 부여하는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void BuffTrigger()
    {

    }
}
