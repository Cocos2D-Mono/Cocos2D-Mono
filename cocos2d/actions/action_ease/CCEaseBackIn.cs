namespace Cocos2D;

public class CCEaseBackIn : CCActionEase
{
    public CCEaseBackIn(CCActionInterval pAction) : base(pAction)
    {
    }

    public CCEaseBackIn(CCFiniteTimeAction pAction) : base(pAction)
    {
    }

    protected CCEaseBackIn(CCEaseBackIn easeBackIn) : base(easeBackIn)
    {
    }

    public override void Update(float time)
    {
        m_pInner.Update(CCEaseMath.BackIn(time));
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCEaseBackOut((CCActionInterval) m_pInner.ReverseOrThrow());
    }

    public override object Copy(ICCCopyable? pZone)
    {
        if (pZone != null)
        {
            //in case of being called at sub class
            var pCopy = (CCEaseBackIn) pZone;
            pCopy.InitWithAction((CCActionInterval) (m_pInner.Copy()));

            return pCopy;
        }
        return new CCEaseBackIn(this);
    }
}