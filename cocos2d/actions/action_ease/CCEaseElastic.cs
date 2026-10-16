using System;

namespace Cocos2D;

public class CCEaseElastic : CCActionEase
{
    protected float m_fPeriod;

    public CCEaseElastic(CCActionInterval pAction) : base(pAction)
    {
        InitWithAction(pAction);
    }

    public CCEaseElastic(CCFiniteTimeAction pAction) : base(pAction)
    {
        InitWithAction(pAction);
    }

    public CCEaseElastic(CCActionInterval pAction, float fPeriod) : base(pAction)
    {
        m_fPeriod = fPeriod;
    }

    public CCEaseElastic(CCFiniteTimeAction pAction, float fPeriod) : base(pAction)
    {
        m_fPeriod = fPeriod;
    }

    protected CCEaseElastic(CCEaseElastic easeElastic) : base(easeElastic)
    {
        InitWithAction((CCActionInterval) (easeElastic.m_pInner.Copy()), easeElastic.m_fPeriod);
    }

    public float Period
    {
        get { return m_fPeriod; }
        set { m_fPeriod = value; }
    }

    protected bool InitWithAction(CCActionInterval pAction, float fPeriod)
    {
        if (base.InitWithAction(pAction))
        {
            m_fPeriod = fPeriod;
            return true;
        }
        return false;
    }

    protected new bool InitWithAction(CCActionInterval pAction)
    {
        return InitWithAction(pAction, 0.3f);
    }

    public override CCFiniteTimeAction Reverse()
    {
        // Only the elastic curves of CCEaseElasticIn, CCEaseElasticOut and CCEaseElasticInOut have a reverse.
        throw new NotSupportedException(GetType().Name + " has no reverse.");
    }

    public override object Copy(ICCCopyable? pZone)
    {
        if (pZone != null)
        {
            //in case of being called at sub class
            var pCopy = (CCEaseElastic) pZone;
            pCopy.InitWithAction((CCActionInterval) (m_pInner.Copy()), m_fPeriod);

            return pCopy;
        }
        return new CCEaseElastic(this);
    }
}