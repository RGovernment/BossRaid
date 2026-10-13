using UnityEngine;
using static FSM;

[CreateAssetMenu(fileName = "SO_RollState", menuName = "Player/State/Roll")]
public sealed class SORollState : SOState
{
    [SerializeField, Min(0f)] private float m_fRollPower = 8f;
    [SerializeField, Min(0f)] private float m_fRollDeceleration = 16f;

    public override eStateType State => eStateType.Roll;
    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
        => new PlayerRollState(_refOwner, _refClip, GetClipEvents(_refClip), m_fRollPower, m_fRollDeceleration);
}
