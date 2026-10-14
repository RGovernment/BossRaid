using UnityEngine;
using UnityEngine.Serialization;
using static FSM;

[CreateAssetMenu(fileName = "SO_DeadState", menuName = "Player/State/Dead")]
public sealed class SODeadState : SOState
{
    public override eStateType State => eStateType.Dead;

    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        return new PlayerDeadState(_refOwner, _refClip, GetClipEvents(_refClip));
    }
}
