using System;
using UnityEngine;
using static FSM;

[CreateAssetMenu(fileName = "SO_MoveState", menuName = "Player/State/Walk or Run")]
public sealed class SOMoveState : SOState
{
    [SerializeField] private eStateType m_eState = eStateType.Walk;
    [SerializeField, Min(0f)] private float m_fMoveSpeed = 2f;
    [SerializeField, Min(1f)] private float m_fDirectionTurnSpeed = 360f;

    public override eStateType State => m_eState;

    public override PlayerState CreateState(FSM _refOwner, AnimationClip _refClip)
    {
        if (m_eState != eStateType.Walk && m_eState != eStateType.Run)
            throw new InvalidOperationException("SOMoveState에는 Walk 또는 Run만 지정할 수 있습니다.");

        return new PlayerMoveState(_refOwner, m_eState, m_fMoveSpeed, m_fDirectionTurnSpeed);
    }
}
