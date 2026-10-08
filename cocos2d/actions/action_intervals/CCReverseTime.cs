using System.Diagnostics;

namespace Cocos2D;

public class CCReverseTime : CCActionInterval
{
    protected CCFiniteTimeAction m_pOther;

    public CCReverseTime(CCFiniteTimeAction action) : base(action.Duration)
    {
        m_pOther = action;
    }

    protected CCReverseTime(CCReverseTime copy)
        : base(copy)
    {
        m_pOther = (CCFiniteTimeAction) copy.m_pOther.Copy();
    }

    public override object Copy(ICCCopyable? zone)
    {
        if (zone != null)
        {
            var ret = (CCReverseTime) zone;
            base.Copy(zone);
            ret.m_pOther = (CCFiniteTimeAction) m_pOther.Copy();
            return ret;
        }
        else
        {
            return new CCReverseTime(this);
        }
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        m_pOther.StartWithTarget(target);
    }

    public override void Stop()
    {
        m_pOther.Stop();
        base.Stop();
    }

    public override void Update(float time)
    {
        if (m_pOther != null)
        {
            m_pOther.Update(1 - time);
        }
    }

    public override CCFiniteTimeAction Reverse()
    {
        return (CCFiniteTimeAction) m_pOther.Copy();
    }
}