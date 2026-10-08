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

    /// <summary>
    /// The reverse a container needs for each of its parts. Every engine action has one; a custom
    /// action that doesn't override Reverse can't be reversed inside a container.
    /// </summary>
    internal CCFiniteTimeAction ReverseOrThrow()
    {
        return Reverse() ?? throw new NotSupportedException(GetType().Name + " has no reverse.");
    }
}