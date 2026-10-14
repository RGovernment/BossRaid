using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AnimatorEvent
{
    private readonly Dictionary<(AnimationClip, int), Action<AnimationEvent>> m_hashFrameEvents = new();

    public void RegisterFrame(AnimationClip _refClip, AnimationEvent _refEvent, Action<AnimationEvent> _actCallback)
    {
        var tKey = (_refClip, GetFrame(_refClip, _refEvent));
        m_hashFrameEvents.TryGetValue(tKey, out var actFrame);
        m_hashFrameEvents[tKey] = actFrame + _actCallback;
    }

    public void UnregisterFrame(AnimationClip _refClip, AnimationEvent _refEvent, Action<AnimationEvent> _actCallback)
    {
        var tKey = (_refClip, GetFrame(_refClip, _refEvent));
        Action<AnimationEvent> actFrame = m_hashFrameEvents[tKey] - _actCallback;
        if (actFrame == null)
            m_hashFrameEvents.Remove(tKey);
        else
            m_hashFrameEvents[tKey] = actFrame;
    }

    public void ExecuteFrame(AnimationEvent _refEvent)
    {
        AnimationClip refClip = _refEvent.animatorClipInfo.clip;
        var tKey = (refClip, GetFrame(refClip, _refEvent));
        if (m_hashFrameEvents.TryGetValue(tKey, out var actFrame))
            actFrame(_refEvent);
    }

    private static int GetFrame(AnimationClip _refClip, AnimationEvent _refEvent)
    {
        return Mathf.RoundToInt(_refEvent.time * _refClip.frameRate);
    }
}
