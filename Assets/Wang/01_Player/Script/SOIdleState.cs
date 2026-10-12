using UnityEngine;
using static FSM;

[CreateAssetMenu(fileName = "SO_IdleState", menuName = "Player/State/Idle")]
public sealed class SOIdleState : SOState
{
    public override eStateType State => eStateType.Idle;
    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip) => new PlayerIdleState(_refOwner);
}
