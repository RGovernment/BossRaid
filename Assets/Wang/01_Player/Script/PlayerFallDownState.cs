using UnityEngine;
using static FSM;

public sealed class PlayerFallDownState : PlayerState
{
    private static readonly int DownHash = Animator.StringToHash("Down");
    private static readonly int JumpFinishedHash = Animator.StringToHash("JumpFinished");

    private bool m_bWaitingForGround;

    public PlayerFallDownState(FSM _refOwner, AnimationClip _refClip, AnimationEvent[] _arrClipEvents)
        : base(_refOwner, eStateType.FallDown)
    {
        m_refClip = _refClip;
        m_arrClipEvents = _arrClipEvents;
    }

    public override void Init()
    {
        base.Init();
        if (m_arrClipEvents.Length < 2)
        {
            Debug.LogError("FallDown 클립에 정지와 종료 이벤트를 순서대로 등록해야 합니다.", m_refOwner);
            return;
        }

        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[0], PauseDown);
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[1], FinishDown);
    }

    public override void Dispose()
    {
        if (m_arrClipEvents.Length >= 2)
        {
            m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[0], PauseDown);
            m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[1], FinishDown);
        }

        base.Dispose();
    }

    public override void Enter()
    {
        m_bWaitingForGround = false;
        m_refOwner.Animator.speed = 1f;
        m_refOwner.Animator.ResetTrigger(JumpFinishedHash);
        m_refOwner.Animator.SetTrigger(DownHash);
    }

    public override void UpdateState()
    {
        if (m_bWaitingForGround == false || m_refOwner.IsGrounded == false)
            return;

        m_bWaitingForGround = false;
        m_refOwner.Animator.speed = 1f;
    }

    public override void Exit()
    {
        m_bWaitingForGround = false;
        m_refOwner.Animator.speed = 1f;
        m_refOwner.Animator.ResetTrigger(DownHash);
    }

    private void PauseDown(AnimationEvent _refEvent)
    {
        if (m_refOwner.IsGrounded)
            return;

        m_bWaitingForGround = true;
        m_refOwner.Animator.speed = 0f;
    }

    private void FinishDown(AnimationEvent _refEvent)
    {
        if (m_refOwner.IsGrounded == false)
            return;

        m_refOwner.ChangeState(eStateType.Idle);
    }
}
