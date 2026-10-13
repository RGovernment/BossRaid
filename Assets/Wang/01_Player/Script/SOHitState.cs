using UnityEngine;
using UnityEngine.Serialization;
using static FSM;

[CreateAssetMenu(fileName = "SO_HitState", menuName = "Player/State/Hit")]
public sealed class SOHitState : SOState
{
    [FormerlySerializedAs("m_fMoveSpeed")]
    [SerializeField, Min(0f)] private float m_fKnockbackPower = 8f;
    [FormerlySerializedAs("m_fDirectionTurnSpeed")]
    [SerializeField, Min(0f)] private float m_fKnockbackDeceleration = 16f;
    [SerializeField, Min(0f)] private float m_fHitDuration = 0.4f;

    public override eStateType State => eStateType.Hit;

    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        return new PlayerHitState(_refOwner, m_fKnockbackPower, m_fKnockbackDeceleration, m_fHitDuration);
    }
}
