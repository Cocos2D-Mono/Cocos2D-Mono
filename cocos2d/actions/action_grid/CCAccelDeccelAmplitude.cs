using System;
using System.Diagnostics.CodeAnalysis;

namespace Cocos2D;

public class CCAccelDeccelAmplitude : CCActionInterval
{
    protected float m_fRate;
    protected CCActionInterval m_pOther;

    public float Rate
    {
        get { return m_fRate; }
        set { m_fRate = value; }
    }

    [MemberNotNull(nameof(m_pOther))]
    protected virtual bool InitWithAction(CCAction pAction, float duration)
    {
        m_pOther = (CCActionInterval) pAction;

        if (base.InitWithDuration(duration))
        {
            m_fRate = 1.0f;

            return true;
        }

        return false;
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        m_pOther.StartWithTarget(target);
    }

    public override void Update(float time)
    {
        float f = time * 2;

        if (f > 1)
        {
            f -= 1;
            f = 1 - f;
        }

        ((m_pOther)).AmplitudeRate = (float) Math.Pow(f, m_fRate);
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCAccelDeccelAmplitude(m_pOther.Reverse(), m_fDuration);
    }

    public CCAccelDeccelAmplitude(CCAction pAction, float duration) : base(duration)
    {
        InitWithAction(pAction, duration);
    }
}