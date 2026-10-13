using UnityEngine;
using static FSM;

public sealed class PlayerHitState : PlayerState
{
    private readonly float m_fKnockbackPower;
    private readonly float m_fKnockbackDeceleration;
    private readonly float m_fGravityMultiplier;
    private Vector3 m_vKnockbackDirection;

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DownHash = Animator.StringToHash("Down");
    private static readonly int JumpFinishedHash = Animator.StringToHash("JumpFinished");

    private eStateType m_eStateType;
    private readonly AnimationClip m_refDownClip;
    private readonly AnimationEvent[] m_arrDownClipEvents;
    private bool m_bFalling;
    private bool m_bWaitingForGround;
    private bool m_bFinished;

    public PlayerHitState(FSM _refOwner, AnimationClip _refClip, AnimationEvent[] _arrClipEvents,
        AnimationClip _refDownClip, AnimationEvent[] _arrDownClipEvents,
        float _fKnockbackPower, float _fKnockbackDeceleration, float _fGravityMultiplier)
        : base(_refOwner, eStateType.Hit)
    {
        m_refClip = _refClip;
        m_arrClipEvents = _arrClipEvents;
        m_fKnockbackPower = _fKnockbackPower;
        m_fKnockbackDeceleration = _fKnockbackDeceleration;
        m_fGravityMultiplier = _fGravityMultiplier;
        m_refDownClip = _refDownClip;
        m_arrDownClipEvents = _arrDownClipEvents;
    }


    public override void Init()
    {
        base.Init();
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[0], FinishKnockBack);
        m_refAnimEvent.RegisterFrame(m_refDownClip, m_arrDownClipEvents[0], PauseDown);
        m_refAnimEvent.RegisterFrame(m_refDownClip, m_arrDownClipEvents[1], FinishDown);
    }

    public override void Dispose()
    {
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[0], FinishKnockBack);
        m_refAnimEvent.UnregisterFrame(m_refDownClip, m_arrDownClipEvents[0], PauseDown);
        m_refAnimEvent.UnregisterFrame(m_refDownClip, m_arrDownClipEvents[1], FinishDown);
        base.Dispose();
    }

    public override void Enter()
    {
        m_bFalling = m_eStateType == eStateType.Jump || m_refOwner.IsGrounded == false;
        m_bWaitingForGround = false;
        m_bFinished = false;

        m_refOwner.Animator.speed = 1f;
        m_refOwner.Animator.ResetTrigger(JumpFinishedHash);
        m_refOwner.Animator.SetTrigger(m_bFalling ? DownHash : HitHash);

        Knockback();
    }

    public override void UpdateState()
    {
        if (m_refOwner.IsGrounded == false)
            return;

        if (m_bWaitingForGround)
        {
            m_bWaitingForGround = false;
            m_refOwner.Animator.speed = 1f;
        }

        if (m_bFinished)
            m_refOwner.ChangeState(m_bFalling ? eStateType.Idle : PlayerMoveState.SelectState());
    }

    public override void Exit()
    {
        m_bWaitingForGround = false;
        m_refOwner.Animator.speed = 1f;
        m_refOwner.Animator.ResetTrigger(HitHash);
        m_refOwner.Animator.ResetTrigger(DownHash);
        base.Exit();
    }

    public override void FixedUpdateState()
    {
      
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        float fVerticalSpeed = vVelocity.y;

        vVelocity.y = 0f;
        vVelocity = Vector3.MoveTowards(vVelocity, Vector3.zero, m_fKnockbackDeceleration * Time.fixedDeltaTime);
        vVelocity.y = fVerticalSpeed;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;

        if (m_bFalling && m_refOwner.IsGrounded == false)
            m_refOwner.Rigidbody.AddForce(Vector3.up * (Physics.gravity.y * (m_fGravityMultiplier - 1f)),
                ForceMode.Acceleration);
    }

    public void SetHitInfo(tAttackInfo _tInfo, eStateType _eType)
    {
        m_eStateType = _eType;
        m_vKnockbackDirection = m_refOwner.Rigidbody.position - _tInfo.Pos;
        m_vKnockbackDirection.y = 0f;
        m_vKnockbackDirection.Normalize();
    }

    private void FinishKnockBack(AnimationEvent _refEvent)
    {
        if (m_bFalling == false)
            m_bFinished = true;
    }

    private void PauseDown(AnimationEvent _refEvent)
    {
        if (m_bFalling == false || m_refOwner.IsGrounded)
            return;

        m_bWaitingForGround = true;
        m_refOwner.Animator.speed = 0f;
    }

    private void FinishDown(AnimationEvent _refEvent)
    {
        if (m_bFalling)
            m_bFinished = true;
    }

    private void Knockback()
    {
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
        m_refOwner.Rigidbody.AddForce(m_vKnockbackDirection * m_fKnockbackPower, ForceMode.Impulse);
    }
}
