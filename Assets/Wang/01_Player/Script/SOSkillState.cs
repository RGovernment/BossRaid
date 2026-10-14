using System;
using UnityEngine;
using static FSM;

[CreateAssetMenu(fileName = "SO_SkillState", menuName = "Player/State/Skill")]
public sealed class SOSkillState : SOState
{

    public override eStateType State => eStateType.Skill;

    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        return new PlayerSkillState(_refOwner, _refOwner.SkillSpawner);
    }
}
