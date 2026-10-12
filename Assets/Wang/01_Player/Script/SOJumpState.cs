using UnityEngine;
using static FSM;

[CreateAssetMenu(fileName = "SO_JumpState", menuName = "Player/State/Jump")]
public sealed class SOJumpState : SOState
{
    [SerializeField, Min(0f)] private float m_fJumpSpeed = 5f;
    [SerializeField] private AnimationClip m_refRunningClip;

    public override eStateType State => eStateType.Jump;
    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        return new PlayerJumpState(_refOwner, _refClip, GetClipEvents(_refClip),
            m_refRunningClip, GetClipEvents(m_refRunningClip), m_fJumpSpeed);
    }
         
}
