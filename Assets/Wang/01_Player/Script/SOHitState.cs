using UnityEngine;
using UnityEngine.Serialization;
using static FSM;

[CreateAssetMenu(fileName = "SO_HitState", menuName = "Player/State/Hit")]
public sealed class SOHitState : SOState
{
    [SerializeField, Min(0f)] private float m_fKnockbackPower = 8f;
    [SerializeField, Min(0f)] private float m_fKnockbackDeceleration = 16f;
    [SerializeField] private AnimationClip m_refDownClip;
    [SerializeField, Min(1f)] private float m_fGravityMultiplier = 2f;
    public override eStateType State => eStateType.Hit;

    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        return new PlayerHitState(_refOwner, _refClip, GetClipEvents(_refClip),
            m_refDownClip, GetClipEvents(m_refDownClip),
            m_fKnockbackPower, m_fKnockbackDeceleration, m_fGravityMultiplier);
    }
}
