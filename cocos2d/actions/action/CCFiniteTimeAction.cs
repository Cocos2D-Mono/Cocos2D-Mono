using System;

namespace Cocos2D;

public class CCFiniteTimeAction : CCAction
{
    protected float m_fDuration;

    protected CCFiniteTimeAction()
    {
    }

    protected CCFiniteTimeAction(CCFiniteTimeAction finiteTimeAction) : base(finiteTimeAction)
    {
        m_fDuration = finiteTimeAction.m_fDuration;
    }

    protected CCFiniteTimeAction(float duration)
    {
        m_fDuration = duration;
    }

    /// <summary>
    /// Get/set the duration of this action
    /// </summary>
    public float Duration
    {
        get { return m_fDuration; }
        set { m_fDuration = value; }
    }

    /// <summary>
    /// Does nothing by default. 
    /// </summary>
    /// <returns></returns>
    public virtual CCFiniteTimeAction? Reverse()
    {
        return null;
    }
}