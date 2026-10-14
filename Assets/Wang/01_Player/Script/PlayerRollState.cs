using UnityEngine;
using UnityEngine.InputSystem;
using static FSM;
using static InputManager;

public sealed class PlayerRollState : PlayerState
{
    private static readonly int RollHash = Animator.StringToHash("Roll");
    private static readonly int GroundHash = Animator.StringToHash("Ground");

    private readonly float m_fRollPower;
    private readonly float m_fRollDeceleration;
    private bool m_bStarted;
    private bool m_bFinished;

    public Vector3 Direction { get; private set; }
    public override Vector2 MoveDirection
    {
        get
        {
            Vector3 vDirection = Quaternion.Inverse(m_refOwner.ViewRotation) * Direction;
            return new Vector2(vDirection.x, vDirection.z);
        }
    }

    public PlayerRollState(FSM _refOwner, AnimationClip _refClip, AnimationEvent[] _arrClipEvents,
        float _fRollPower, float _fRollDeceleration) : base(_refOwner, eStateType.Roll)
    {
        m_refClip = _refClip;
        m_arrClipEvents = _arrClipEvents;
        m_fRollPower = _fRollPower;
        m_fRollDeceleration = _fRollDeceleration;
    }

    public override void Init()
    {
        base.Init();
        m_Instance.RollAction.performed += RequestRoll;
        // 클립 이벤트 시간순: 구르기 시작, 종료.
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[0], StartRoll);
        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[1], FinishRoll);
    }

    public override void Dispose()
    {
        if (m_Instance != null)
            m_Instance.RollAction.performed -= RequestRoll;
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[0], StartRoll);
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[1], FinishRoll);
        base.Dispose();
    }

    private void RequestRoll(InputAction.CallbackContext _tContext)
    {
        if (m_refOwner.CurrentState != null)
            m_refOwner.ChangeState(eStateType.Roll);
    }

    public override bool CanEnter()
    {
        if (!m_refOwner.HasGameplayInput || m_refOwner.CurrentState == null)
            return false;

        if (m_refOwner.CurrentState.Type == eStateType.Jump ||
            m_refOwner.CurrentState.Type == eStateType.Hit ||
            m_refOwner.CurrentState.Type == eStateType.Skill)
            return false;

        return true;
    }

    public override void Enter()
    {
        Vector2 vInput = m_Instance.MoveAction.ReadValue<Vector2>();
        Vector3 vDirection = vInput.sqrMagnitude > 0.001f
            ? new Vector3(vInput.x, 0f, vInput.y).normalized : Vector3.forward;

        Direction = m_refOwner.ViewRotation * vDirection;
        m_refOwner.Rigidbody.rotation = Quaternion.LookRotation(Direction);
        m_bStarted = false;
        m_bFinished = false;
        m_refOwner.Animator.SetBool(GroundHash, m_refOwner.IsGrounded);
        m_refOwner.Animator.SetTrigger(RollHash);
    }

    public override void UpdateState()
    {
        m_refOwner.Animator.SetBool(GroundHash, m_refOwner.IsGrounded);
    }

    public override void Exit()
    {
        m_refOwner.Animator.ResetTrigger(RollHash);
        m_refOwner.Animator.SetBool(GroundHash, false);
        m_bStarted = false;
        m_bFinished = false;
    }

    public override void FixedUpdateState()
    {
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        float fVerticalSpeed = vVelocity.y;
        vVelocity.y = 0f;
        vVelocity = Vector3.MoveTowards(vVelocity, Vector3.zero, m_fRollDeceleration * Time.fixedDeltaTime);
        vVelocity.y = fVerticalSpeed;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
    }

    private void StartRoll(AnimationEvent _refEvent)
    {
        if (m_bStarted)
            return;

        m_bStarted = true;
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
        m_refOwner.Rigidbody.AddForce(Direction * m_fRollPower, ForceMode.Impulse);
    }

    private void FinishRoll(AnimationEvent _refEvent)
    {
        if (!m_bStarted || m_bFinished)
            return;

        m_bFinished = true;
        m_refOwner.ChangeState(PlayerMoveState.SelectState());
    }
}
