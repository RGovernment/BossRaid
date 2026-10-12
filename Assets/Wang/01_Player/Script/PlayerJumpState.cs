using UnityEngine;
using static FSM;
using static InputManager;

public sealed class PlayerJumpState : PlayerState
{
    private enum eJumpPhase
    {
        Start,
        Airborne,
        WaitingForGround,
        Finished,
    }

    private static readonly int IdleJumpHash = Animator.StringToHash("Jumping");
    private static readonly int RunningJumpHash = Animator.StringToHash("RunningJump");
    private static readonly int JumpFinishedHash = Animator.StringToHash("JumpFinished");
    private static readonly int GroundHash = Animator.StringToHash("Ground");
    private static readonly int JumpVerticalHash = Animator.StringToHash("JumpVertical");

    private readonly float m_fJumpSpeed;
    private readonly AnimationClip m_refRunningClip;
    private readonly AnimationEvent[] m_arrRunningClipEvents;
    private PlayerMoveState m_refMovementState;
    private eJumpPhase m_ePhase;
    private float m_fAnimationSpeed;

    public override Vector2 MoveDirection => m_refMovementState.MoveDirection;

    public PlayerJumpState(FSM _refOwner, AnimationClip _refClip, AnimationEvent[] _arrClipEvents,
        AnimationClip _refRunningClip, AnimationEvent[] _arrRunningClipEvents, float _fJumpSpeed)
        : base(_refOwner, eStateType.Jump)
    {
        m_refClip = _refClip;
        m_arrClipEvents = _arrClipEvents;
        m_refRunningClip = _refRunningClip;
        m_arrRunningClipEvents = _arrRunningClipEvents;
        m_fJumpSpeed = _fJumpSpeed;
    }

    public override void Init()
    {
        base.Init();
        m_Instance.OnJumpButtonPressed += RequestJump;
        // 각 클립 이벤트 시간순: 이륙, 공중 유지, 착지 동작 완료.
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[0], Jump);
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[1], StayJump);
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[2], FinishJump);
        m_refAnimEvent.RegisterFrame(m_refRunningClip, m_arrRunningClipEvents[0], Jump);
        m_refAnimEvent.RegisterFrame(m_refRunningClip, m_arrRunningClipEvents[1], StayJump);
        m_refAnimEvent.RegisterFrame(m_refRunningClip, m_arrRunningClipEvents[2], FinishJump);
    }

    public override void Dispose()
    {
        if (m_Instance != null)
            m_Instance.OnJumpButtonPressed -= RequestJump;

        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[0], Jump);
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[1], StayJump);
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[2], FinishJump);
        m_refAnimEvent.UnregisterFrame(m_refRunningClip, m_arrRunningClipEvents[0], Jump);
        m_refAnimEvent.UnregisterFrame(m_refRunningClip, m_arrRunningClipEvents[1], StayJump);
        m_refAnimEvent.UnregisterFrame(m_refRunningClip, m_arrRunningClipEvents[2], FinishJump);
        base.Dispose();
    }

    private void RequestJump()
    {
        if (m_refOwner.CurrentState != null)
            m_refOwner.ChangeState(eStateType.Jump);
    }

    public override bool CanEnter()
    {
        if (!m_Instance.HasGameplayInput)
            return false;

        if (m_refOwner.CurrentState.Type == eStateType.Roll)
            return false;

        return m_refOwner.IsGrounded;
    }

    public override void Enter()
    {
        m_ePhase = eJumpPhase.Start;

        eStateType eMoveState = m_Instance.IsRunHeld ? eStateType.Run : eStateType.Walk;
        m_refMovementState = m_refOwner.FindState<PlayerMoveState>(eMoveState);
        m_refMovementState.StartMove(m_refOwner.PreviousState?.MoveDirection ?? Vector2.zero);

        bool bRunningJump = m_Instance.InputInfo.MoveDir.sqrMagnitude > 0.001f;
        m_refOwner.Animator.ResetTrigger(JumpFinishedHash);
        m_refOwner.Animator.SetBool(GroundHash, false);
        m_refOwner.Animator.SetFloat(JumpVerticalHash, m_refOwner.Rigidbody.linearVelocity.y);
        m_refOwner.Animator.SetTrigger(bRunningJump ? RunningJumpHash : IdleJumpHash);
    }

    public override void UpdateState()
    {
        bool bGrounded = m_refOwner.IsGrounded;
        if (bGrounded)
        {
            ResumeJumpAnimation();
            if (m_ePhase == eJumpPhase.Finished)
            {
                m_refOwner.ChangeState(PlayerMoveState.SelectState());
                return;
            }
        }

        //여기서 좀 강하게 연결됨 
        eStateType eMoveState = m_Instance.IsRunHeld ? eStateType.Run : eStateType.Walk;
        PlayerMoveState refMovement = m_refOwner.FindState<PlayerMoveState>(eMoveState);
        //에어본일때만 움직이게
        if (refMovement != m_refMovementState && m_ePhase != eJumpPhase.Airborne)
        {
            refMovement.StartMove(m_refMovementState.MoveDirection);
            m_refMovementState = refMovement;
        }

        m_refMovementState.UpdateMovement();

        m_refOwner.Animator.SetBool(GroundHash, m_ePhase != eJumpPhase.Start && bGrounded);
        m_refOwner.Animator.SetFloat(JumpVerticalHash, m_refOwner.Rigidbody.linearVelocity.y);
    }

    public override void FixedUpdateState()
    {
        m_refMovementState.ApplyMovement();
    }

    public override void Exit()
    {
        ResumeJumpAnimation();
        m_refMovementState.ResetAnimation();
        m_refOwner.Animator.ResetTrigger(IdleJumpHash);
        m_refOwner.Animator.ResetTrigger(RunningJumpHash);
        m_refOwner.Animator.SetTrigger(JumpFinishedHash);
        m_refOwner.Animator.SetBool(GroundHash, false);
        m_refOwner.Animator.SetFloat(JumpVerticalHash, 0f);
    }

    private void Jump(AnimationEvent _refEvent)
    {
        if (m_ePhase != eJumpPhase.Start)
            return;

        if (!m_refOwner.IsGrounded)
            return;

        m_ePhase = eJumpPhase.Airborne;
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        vVelocity.y = m_fJumpSpeed;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
        m_refOwner.ClearGroundContact();
    }

    private void StayJump(AnimationEvent _refEvent)
    {
        if (m_ePhase != eJumpPhase.Airborne)
            return;

        if (m_refOwner.IsGrounded)
            return;

        m_fAnimationSpeed = m_refOwner.Animator.speed;
        m_refOwner.Animator.speed = 0f;
        m_ePhase = eJumpPhase.WaitingForGround;
    }

    private void FinishJump(AnimationEvent _refEvent)
    {
        if (m_ePhase == eJumpPhase.Start)
            return;

        ResumeJumpAnimation();
        m_ePhase = eJumpPhase.Finished;
    }

    private void ResumeJumpAnimation()
    {
        if (m_ePhase != eJumpPhase.WaitingForGround)
            return;

        m_refOwner.Animator.speed = m_fAnimationSpeed;
        m_ePhase = eJumpPhase.Airborne;
    }
}
