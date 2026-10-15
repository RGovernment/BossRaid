using System;
using static Enums_JJH;

[Serializable]
public class SkillData
{
    public string Name;
    public int Id;
    public int Value;
    public int OtherValue;
    public int Cooldown;
    public int UpgradeType;
    public int projectileCount;
    public SkillAnimationType AnimationType;
    public string Description;
}
