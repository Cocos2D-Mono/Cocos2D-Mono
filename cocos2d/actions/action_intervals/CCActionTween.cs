using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Cocos2D;

public interface ICCActionTweenDelegate
{
    void UpdateTweenAction(float value, string key);
}

public class CCActionTween : CCActionInterval
{
    protected float m_fDelta;
    protected float m_fFrom, m_fTo;
    protected string m_strKey;
    // Optional; without it the target must implement ICCActionTweenDelegate.
    protected Action<float, string>? _tweenAction;

    public CCActionTween(float aDuration, string key, float from, float to)
    {
        InitWithDuration(aDuration, key, from, to);
    }

    public CCActionTween(float aDuration, string key, float from, float to, Action<float,string>? tweenAction)
    {
        InitWithDuration(aDuration, key, from, to);
        _tweenAction = tweenAction;
    }

    [MemberNotNull(nameof(m_strKey))]
    protected bool InitWithDuration(float aDuration, string key, float from, float to)
    {
        m_strKey = key;

        if (base.InitWithDuration(aDuration))
        {
            m_fTo = to;
            m_fFrom = from;
            return true;
        }

        return false;
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        Debug.Assert(_tweenAction != null || target is ICCActionTweenDelegate, "target must implement CCActionTweenDelegate");
        base.StartWithTarget(target);
        m_fDelta = m_fTo - m_fFrom;
    }

    public override void Update(float dt)
    {
        float amt = m_fTo - m_fDelta * (1 - dt);
        if (_tweenAction != null)
        {
            _tweenAction(amt, m_strKey);
        }
        else if(m_pTarget is ICCActionTweenDelegate)
        {
            ((ICCActionTweenDelegate)m_pTarget).UpdateTweenAction(amt, m_strKey);
        }
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCActionTween(m_fDuration, m_strKey, m_fTo, m_fFrom, _tweenAction);
    }
}