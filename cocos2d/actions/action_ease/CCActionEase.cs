using System.Diagnostics.CodeAnalysis;

namespace Cocos2D;

public class CCActionEase : CCActionInterval
{
    protected CCFiniteTimeAction m_pInner;

    // This can be taken out once all the classes that extend it have had their constructors created.
    protected CCActionEase()
    {
        // Subclasses using this constructor (CCEaseCustom, CCBEaseInstant) call InitWithAction
        // straight after construction, which sets it.
        m_pInner = null!;
    }

    public CCFiniteTimeAction InnerAction
    {
        get { return m_pInner; }
    }

    public CCActionEase(CCFiniteTimeAction pAction)
    {
        InitWithAction(pAction);
    }

    protected CCActionEase(CCActionEase actionEase) : base(actionEase)
    {
        InitWithAction((CCActionInterval) (actionEase.m_pInner.Copy()));
    }

    [MemberNotNull(nameof(m_pInner))]
    protected bool InitWithAction(CCActionInterval pAction)
    {
        m_pInner = pAction;

        if (base.InitWithDuration(pAction.Duration))
        {
            return true;
        }
        return false;
    }

    [MemberNotNull(nameof(m_pInner))]
    protected bool InitWithAction(CCFiniteTimeAction pAction)
    {
        m_pInner = pAction;

        if (base.InitWithDuration(pAction.Duration))
        {
            return true;
        }
        return false;
    }

    public override object Copy(ICCCopyable? pZone)
    {
        if (pZone != null)
        {
            //in case of being called at sub class
            var pCopy = (CCActionEase) pZone;
            base.Copy(pZone);

            pCopy.InitWithAction((CCActionInterval) (m_pInner.Copy()));

            return pCopy;
        }
        return new CCActionEase(this);
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        m_pInner.StartWithTarget(target);
    }

    public override void Stop()
    {
        m_pInner.Stop();
        base.Stop();
    }

    public override void Update(float time)
    {
        m_pInner.Update(time);
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCActionEase((CCActionInterval) m_pInner.ReverseOrThrow());
    }
}