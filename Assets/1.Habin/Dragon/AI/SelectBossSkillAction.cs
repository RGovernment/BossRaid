using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SelectBossSkill", story: "Select [SelectedSkill] for [Self] against [Target] excluding [LastSkill]", category: "Action", id: "343e17a0c561efd7efba6afb3187e88d")]
public partial class SelectBossSkillAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<BossSkillType> SelectedSkill;
    [SerializeReference] public BlackboardVariable<BossSkillType> LastSkill;

    private BossSkillType[] meleeSkills = { BossSkillType.Slam, BossSkillType.TailWhip, BossSkillType.Meteor };
    private BossSkillType[] rangedSkills = { BossSkillType.Rush, BossSkillType.Meteor };


    protected override Status OnStart()
    {
        if (Self == null || Self.Value == null || Target == null || Target.Value == null || SelectedSkill == null || LastSkill == null)
            return Status.Failure;

        SelectedSkill.Value = BossSkillType.None;

        BossRangeSettings ranges = Self.Value.GetComponent<BossRangeSettings>();

        if (ranges == null)
            return Status.Failure;

        Vector3 offset = Target.Value.transform.position - Self.Value.transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        Debug.Log($"거리: {distance:F4}, 근거리 기준: {ranges.MeleeRange:F4}");
        BossSkillType[] skills;

        if (distance <= ranges.MeleeRange)
            skills = meleeSkills;
        else if (distance <= ranges.RangedRange)
            skills = rangedSkills;
        else
            return Status.Failure;

        var candidates = new System.Collections.Generic.List<BossSkillType>();

        foreach (BossSkillType skill in skills)
        {
            if (skill == BossSkillType.None || skill == BossSkillType.Wipe || skill == LastSkill.Value)
                continue;

            if (!candidates.Contains(skill))
                candidates.Add(skill);
        }

        if (candidates.Count == 0)
            return Status.Failure;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        SelectedSkill.Value = candidates[index];

        Debug.Log($"선택한 스킬: {SelectedSkill.Value}");

        return Status.Success;
    }
}

