#nullable disable
using System.Diagnostics;

namespace Cocos2D;

public class CCRepeatForever : CCActionInterval
{
    protected CCActionInterval m_pInnerAction;

    public CCActionInterval InnerAction
    {
        get { return m_pInnerAction; }
        set { m_pInnerAction = value; }
    }

    /// <summary>
    /// Creates a CCRepeatForever action that repeats a single action indefinitely.
    /// </summary>
    /// <param name="action">The action to repeat forever.</param>
    public CCRepeatForever(CCActionInterval action)
    {
        InitWithAction(action);
    }

    /// <summary>
    /// Creates a CCRepeatForever action that repeats a sequence of actions indefinitely.
    /// The actions are automatically combined into a CCSequence and then repeated forever.
    /// </summary>
    /// <param name="actions">An array of actions to be executed sequentially and repeated forever.</param>
    public CCRepeatForever(CCActionInterval[] actions)
    {
        InitWithActions(actions);
    }

    protected CCRepeatForever(CCRepeatForever repeatForever) : base(repeatForever)
    {
        var param = repeatForever.m_pInnerAction.Copy() as CCActionInterval;
        InitWithAction(param);
    }

    protected bool InitWithAction(CCActionInterval action)
    {
        Debug.Assert(action != null);
        m_pInnerAction = action;
        // Duration = action.Duration;
        return true;
    }

    protected bool InitWithActions(CCActionInterval[] actions)
    {
        Debug.Assert(actions != null && actions.Length > 0);

        m_pInnerAction = new CCSequence(actions);
        // Duration = m_pInnerAction.Duration;
        return true;
    }

    public override object Copy(ICCCopyable zone)
    {
        if (zone != null)
        {
            var ret = zone as CCRepeatForever;
            if (ret == null)
            {
                return null;
            }
            base.Copy(zone);

            var param = m_pInnerAction.Copy() as CCActionInterval;
            if (param == null)
            {
                return null;
            }
            ret.InitWithAction(param);

            return ret;
        }
        else
        {
            return new CCRepeatForever(this);
        }
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        m_pInnerAction.StartWithTarget(target);
    }

    public override void Step(float dt)
    {
        m_pInnerAction.Step(dt);

        if (m_pInnerAction.IsDone)
        {
            float diff = m_pInnerAction.Elapsed - m_pInnerAction.Duration;
            m_pInnerAction.StartWithTarget(m_pTarget);
            m_pInnerAction.Step(0f);
            m_pInnerAction.Step(diff);
        }
    }

    public override bool IsDone
    {
        get { return false; }
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCRepeatForever(m_pInnerAction.Reverse() as CCActionInterval);
    }
}