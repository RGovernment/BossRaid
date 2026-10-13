using UnityEngine;
using static FSM;

public sealed class PlayerHitState : PlayerState
{
    private readonly float m_fKnockbackPower;
    private readonly float m_fKnockbackDeceleration;
    private readonly float m_fHitDuration;
    private Vector3 m_vKnockbackDirection;
    private float m_fEndTime;
    private bool m_bPendingImpulse;

    public PlayerHitState(FSM _refOwner, float _fKnockbackPower, float _fKnockbackDeceleration,
        float _fHitDuration) : base(_refOwner, eStateType.Hit)
    {
        m_fKnockbackPower = _fKnockbackPower;
        m_fKnockbackDeceleration = _fKnockbackDeceleration;
        m_fHitDuration = _fHitDuration;
    }

    public void PrepareHit(tAttackInfo _tInfo)
    {
        m_vKnockbackDirection = -_tInfo.AttackDirection;
        m_vKnockbackDirection.y = 0f;
        m_vKnockbackDirection.Normalize();
    }

    public override void Enter()
    {
        m_fEndTime = Time.time + m_fHitDuration;
        m_bPendingImpulse = true;
    }

    public override void UpdateState()
    {
        if (m_bPendingImpulse == false && Time.time >= m_fEndTime)
            m_refOwner.ChangeState(PlayerMoveState.SelectState());
    }

    public override void FixedUpdateState()
    {
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        if (m_bPendingImpulse)
        {
            m_bPendingImpulse = false;
            vVelocity.x = 0f;
            vVelocity.z = 0f;
            m_refOwner.Rigidbody.linearVelocity = vVelocity;
            m_refOwner.Rigidbody.AddForce(m_vKnockbackDirection * m_fKnockbackPower, ForceMode.Impulse);
            return;
        }

        float fVerticalSpeed = vVelocity.y;
        vVelocity.y = 0f;
        vVelocity = Vector3.MoveTowards(vVelocity, Vector3.zero, m_fKnockbackDeceleration * Time.fixedDeltaTime);
        vVelocity.y = fVerticalSpeed;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
    }
}
