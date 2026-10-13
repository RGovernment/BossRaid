using System;
using UnityEngine;
using static FSM;

public abstract class SOState : ScriptableObject
{
    [SerializeField] protected AnimationClip m_refClip = null;
    public AnimationClip Clip => m_refClip;

    public abstract eStateType State { get; }

    public abstract PlayerState CreateState(FSM _refOwner, AnimationClip _refClip);

    protected static AnimationEvent[] GetClipEvents(AnimationClip _refClip)
    {
        AnimationEvent[] arrEvents = _refClip.events;
        Array.Sort(arrEvents, (_refLeft, _refRight) => _refLeft.time.CompareTo(_refRight.time));
        return arrEvents;
    }
}
