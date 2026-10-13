using UnityEngine;
using static FSM;

public sealed class PlayerIdleState : PlayerState
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundHash = Animator.StringToHash("Ground");

    public PlayerIdleState(FSM _refOwner) : base(_refOwner, eStateType.Idle) { }

    public override void Enter()
    {
        m_refOwner.Animator.SetFloat(MoveXHash, 0f);
        m_refOwner.Animator.SetFloat(MoveYHash, 0f);
        m_refOwner.Animator.SetFloat(SpeedHash, 0f);
        m_refOwner.Animator.SetBool(GroundHash, m_refOwner.IsGrounded);
        FixedUpdateState();
    }

    public override void UpdateState()
    {
        m_refOwner.Rigidbody.rotation = m_refOwner.ViewRotation;
        m_refOwner.Animator.SetBool(GroundHash, m_refOwner.IsGrounded);
        m_refOwner.ChangeState(PlayerMoveState.SelectState());
    }

    public override void FixedUpdateState()
    {
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        m_refOwner.Rigidbody.linearVelocity = vVelocity;
    }

    public override void Exit()
    {
        m_refOwner.Animator.SetBool(GroundHash, false);
    }
}
