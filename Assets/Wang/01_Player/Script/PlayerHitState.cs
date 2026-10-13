using UnityEngine;
using static FSM;

public sealed class PlayerHitState : PlayerState
{
    private readonly float m_fKnockbackPower;
    private readonly float m_fKnockbackDeceleration;
    private Vector3 m_vKnockbackDirection;
    private bool m_bPendingImpulse;

    private static readonly int HitHash = Animator.StringToHash("Hit");

    public PlayerHitState(FSM _refOwner, AnimationClip _refClip, AnimationEvent[] _arrClipEvents,
        float _fKnockbackPower, float _fKnockbackDeceleration)
        : base(_refOwner, eStateType.Hit)
    {
        m_refClip = _refClip;
        m_arrClipEvents = _arrClipEvents;
        m_fKnockbackPower = _fKnockbackPower;
        m_fKnockbackDeceleration = _fKnockbackDeceleration;
    }

    public override void Init()
    {
        base.Init();
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[0], FinishKnockBack);
    }

    public override void Dispose()
    {
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[0], FinishKnockBack);
        base.Dispose();
    }

    public override void Enter()
    {
        //공중에서 맞으면?
        m_refOwner.Animator.SetTrigger(HitHash);
        m_bPendingImpulse = true;
    }
    public override void Exit()
    {
        m_bPendingImpulse = false;
        base.Exit();
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

    public void SetHitInfo(tAttackInfo _tInfo)
    {
        m_vKnockbackDirection = -_tInfo.AttackDirection;
        m_vKnockbackDirection.y = 0f;
        m_vKnockbackDirection.Normalize();
    }

    private void FinishKnockBack(AnimationEvent _refEvent)
    {
        m_refOwner.ChangeState(PlayerMoveState.SelectState());
    }
}
