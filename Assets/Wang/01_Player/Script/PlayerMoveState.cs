using UnityEngine;
using static FSM;
using static InputManager;

public sealed class PlayerMoveState : PlayerState
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundHash = Animator.StringToHash("Ground");

    private readonly float m_fMoveSpeed;
    private readonly float m_fDirectionTurnSpeed;
    private Vector2 m_vMoveInput;
    private Vector2 m_vMoveDir;

    public override Vector2 MoveDirection => m_vMoveDir;

    public PlayerMoveState(FSM _refOwner, eStateType _eType, float _fMoveSpeed, float _fDirectionTurnSpeed)
        : base(_refOwner, _eType)
    {
        m_fMoveSpeed = _fMoveSpeed;
        m_fDirectionTurnSpeed = _fDirectionTurnSpeed;
    }

    public override bool CanEnter()
    {
        return SelectState() == Type;
    }

    public override void Enter()
    {
        StartMove(m_refOwner.PreviousState?.MoveDirection ?? Vector2.zero);
        UpdateMovement();
        m_refOwner.Animator.SetBool(GroundHash, m_refOwner.IsGrounded);
    }

    public override void UpdateState()
    {
        eStateType eNextState = SelectState();
        if (eNextState != Type)
        {
            m_refOwner.ChangeState(eNextState);
            return;
        }

        UpdateMovement();
        m_refOwner.Animator.SetBool(GroundHash, m_refOwner.IsGrounded);
    }

    public override void FixedUpdateState()
    {
        ApplyMovement();
    }

    public override void Exit()
    {
        // 방향은 다음 상태가 Enter에서 이어받습니다.
        ResetAnimation();
        m_refOwner.Animator.SetBool(GroundHash, false);
    }

    public void StartMove(Vector2 _vMoveDirection)
    {
        m_vMoveDir = _vMoveDirection;
        m_vMoveInput = Vector2.ClampMagnitude(m_Instance.MoveAction.ReadValue<Vector2>(), 1f);
    }

    // Jump에서도 같은 이동 계산을 사용하되 상태 전환은 호출한 State가 결정합니다.
    public void UpdateMovement()
    {
        m_vMoveInput = Vector2.ClampMagnitude(m_Instance.InputInfo.MoveDir, 1f);
        if (m_vMoveInput.sqrMagnitude <= 0.001f)
            m_vMoveDir = Vector2.zero;
        else
        {
            float fCurrentAngle = Mathf.Atan2(m_vMoveDir.x, m_vMoveDir.y) * Mathf.Rad2Deg;
            float fTargetAngle = Mathf.Atan2(m_vMoveInput.x, m_vMoveInput.y) * Mathf.Rad2Deg;
            float fFinalAngle = fTargetAngle;
            //45도 이내만 보간해서 움직이게
            if (Mathf.Abs(Mathf.DeltaAngle(fCurrentAngle, fTargetAngle)) <= 45f)
            {
                fFinalAngle = Mathf.MoveTowardsAngle(fCurrentAngle, fTargetAngle,
                    m_fDirectionTurnSpeed * Time.deltaTime);
            }

            float fDirectionAngle = fFinalAngle * Mathf.Deg2Rad;
            m_vMoveDir = new Vector2(Mathf.Sin(fDirectionAngle), Mathf.Cos(fDirectionAngle));
        }

        m_refOwner.Rigidbody.rotation = m_refOwner.ViewRotation;
        Vector2 vAnimationValue = m_vMoveDir * m_vMoveInput.magnitude;
        m_refOwner.Animator.SetFloat(MoveXHash, vAnimationValue.x);
        m_refOwner.Animator.SetFloat(MoveYHash, vAnimationValue.y);
        m_refOwner.Animator.SetFloat(SpeedHash, Type == eStateType.Run ? 2f : 0f);
    }

    public void ApplyMovement()
    {
        Vector3 vVelocity = m_refOwner.ViewRotation * new Vector3(m_vMoveDir.x, 0f, m_vMoveDir.y)
            * (m_fMoveSpeed * m_vMoveInput.magnitude);

        vVelocity.y = m_refOwner.Rigidbody.linearVelocity.y;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
    }

    public void ResetAnimation()
    {
        m_refOwner.Animator.SetFloat(MoveXHash, 0f);
        m_refOwner.Animator.SetFloat(MoveYHash, 0f);
        m_refOwner.Animator.SetFloat(SpeedHash, 0f);
    }

    public static eStateType SelectState()
    {
        if (!m_Instance.HasGameplayInput)
            return eStateType.Idle;

        if (m_Instance.InputInfo.MoveDir.sqrMagnitude <= 0.001f)
            return eStateType.Idle;

        return m_Instance.IsRunHeld ? eStateType.Run : eStateType.Walk;
    }
}
