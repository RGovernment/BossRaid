using UnityEngine;
using UnityEngine.Serialization;
using static FSM;

[CreateAssetMenu(fileName = "SO_HitState", menuName = "Player/State/Hit")]
public sealed class SOHitState : SOState
{
    [SerializeField, Min(0f)] private float m_fKnockbackPower = 8f;
    [SerializeField, Min(0f)] private float m_fKnockbackDeceleration = 16f;
    public override eStateType State => eStateType.Hit;

    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        return new PlayerHitState(_refOwner, _refClip, GetClipEvents(_refClip),
            m_fKnockbackPower, m_fKnockbackDeceleration);
    }
}
