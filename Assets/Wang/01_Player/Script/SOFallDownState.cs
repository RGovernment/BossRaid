using UnityEngine;
using static FSM;

[CreateAssetMenu(fileName = "SO_FallDownState", menuName = "Player/State/FallDown")]
public sealed class SOFallDownState : SOState
{
    public override eStateType State => eStateType.FallDown;

    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        return new PlayerFallDownState(_refOwner, _refClip, GetClipEvents(_refClip));
    }
}
