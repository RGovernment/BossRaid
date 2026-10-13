using UnityEngine;
using static FSM;

public abstract class PlayerState
{
    protected readonly FSM m_refOwner;
    protected readonly eStateType m_eType;
    protected AnimatorEvent m_refAnimEvent;

    protected AnimationClip m_refClip;
    protected AnimationEvent[] m_arrClipEvents;

    public eStateType Type => m_eType;
    public virtual Vector2 MoveDirection => Vector2.zero;

    protected PlayerState(FSM _refOwner, eStateType _eType)
    {
        m_eType = _eType;
        m_refOwner = _refOwner;
    }

    public virtual void Init()
    {
        m_refAnimEvent = m_refOwner.FindAnimEvent(m_eType);
    }

    public virtual void Dispose()
    {
        m_refAnimEvent = null;
    }

    public virtual bool CanEnter() => true;
    public virtual void Enter() { }
    public virtual void UpdateState() { }
    public virtual void FixedUpdateState() { }
    public virtual void Exit() { }
}
