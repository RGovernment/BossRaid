using UnityEngine;
using UnityEngine.InputSystem;
using static FSM;
using static InputManager;

public sealed class PlayerJumpState : PlayerState
{
    private enum eJumpPhase
    {
        None,

        Start,
        Air,
        Finish,
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
        m_Instance.JumpAction.performed += RequestJump;
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
            m_Instance.JumpAction.performed -= RequestJump;

        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[0], Jump);
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[1], StayJump);
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[2], FinishJump);
        m_refAnimEvent.UnregisterFrame(m_refRunningClip, m_arrRunningClipEvents[0], Jump);
        m_refAnimEvent.UnregisterFrame(m_refRunningClip, m_arrRunningClipEvents[1], StayJump);
        m_refAnimEvent.UnregisterFrame(m_refRunningClip, m_arrRunningClipEvents[2], FinishJump);
        base.Dispose();
    }

    private void RequestJump(InputAction.CallbackContext _tContext)
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

        if (m_refOwner.CurrentState.Type == eStateType.Hit)
            return false;

        return m_refOwner.IsGrounded;
    }

    public override void Enter()
    {
        m_ePhase = eJumpPhase.None;

        eStateType eMoveState = m_Instance.IsRunHeld ? eStateType.Run : eStateType.Walk;
        m_refMovementState = m_refOwner.FindState<PlayerMoveState>(eMoveState);
        Vector2 vInput = m_Instance.MoveAction.ReadValue<Vector2>();
        m_refMovementState.StartMove(vInput.normalized);

        bool bRunningJump = vInput.sqrMagnitude > 0.001f;
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
            if (m_ePhase == eJumpPhase.Finish)
            {
                m_refOwner.ChangeState(PlayerMoveState.SelectState());
                return;
            }
        }

        if (m_ePhase == eJumpPhase.Air)
            m_refMovementState.UpdateMovement();

        m_refOwner.Animator.SetBool(GroundHash, m_ePhase != eJumpPhase.Start && bGrounded);
        m_refOwner.Animator.SetFloat(JumpVerticalHash, m_refOwner.Rigidbody.linearVelocity.y);
    }

    public override void FixedUpdateState()
    {
        if (m_ePhase == eJumpPhase.Air)
        {
            m_refMovementState.ApplyMovement();
            return;
        }

        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
    }

    public override void Exit()
    {
        ResumeJumpAnimation();
        m_refMovementState.ResetAnimation();
        m_refOwner.Animator.ResetTrigger(IdleJumpHash);
        m_refOwner.Animator.ResetTrigger(RunningJumpHash);
        m_refOwner.Animator.SetTrigger(JumpFinishedHash);
        m_refOwner.Animator.SetBool(GroundHash, true);
        m_refOwner.Animator.SetFloat(JumpVerticalHash, 0f);
    }

    private void Jump(AnimationEvent _refEvent)
    {
        m_ePhase = eJumpPhase.Start;

        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        vVelocity.y = m_fJumpSpeed;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
        m_refOwner.ClearGroundContact();
    }

    private void StayJump(AnimationEvent _refEvent)
    {
        m_ePhase = eJumpPhase.Air;
        if (m_refOwner.IsGrounded)
            return;

        m_fAnimationSpeed = m_refOwner.Animator.speed;
        m_refOwner.Animator.speed = 0f;
    }

    private void FinishJump(AnimationEvent _refEvent)
    {
      
        ResumeJumpAnimation();
        m_ePhase = eJumpPhase.Finish;
    }

    private void ResumeJumpAnimation()
    {
        if (m_ePhase != eJumpPhase.Air || m_refOwner.Animator.speed != 0f)
            return;

        m_refOwner.Animator.speed = m_fAnimationSpeed;
    }
}
