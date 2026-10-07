#nullable disable
using System;

namespace Cocos2D;

public partial class CCEaseCustom : CCActionEase
{
    private Func<float, float> _easeFunc;

    public Func<float, float> EaseFunc
    {
        get { return _easeFunc; }
        set { _easeFunc = value; }
    }

    public CCEaseCustom(CCActionInterval pAction, Func<float, float> easeFunc)
    {
        InitWithAction(pAction, easeFunc);
    }

    public CCEaseCustom(CCFiniteTimeAction pAction, Func<float, float> easeFunc)
    {
        InitWithAction(pAction, easeFunc);
    }

    protected CCEaseCustom(CCEaseCustom easeCustom)
        : base(easeCustom)
    {
        InitWithAction((CCActionInterval) easeCustom.InnerAction.Copy(), easeCustom.EaseFunc);
    }

    public void InitWithAction(CCActionInterval action, Func<float, float> easeFunc)
    {
        base.InitWithAction(action);
        _easeFunc = easeFunc;
    }

    public void InitWithAction(CCFiniteTimeAction action, Func<float, float> easeFunc)
    {
        base.InitWithAction(action);
        _easeFunc = easeFunc;
    }

    public override void Update(float time)
    {
        m_pInner.Update(_easeFunc(time));
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCReverseTime(new CCEaseCustom(this));
    }

    public override object Copy(ICCCopyable pZone)
    {
        if (pZone != null)
        {
            //in case of being called at sub class
            var pCopy = pZone as CCEaseCustom;
            base.Copy(pCopy);
            pCopy.InitWithAction((CCActionInterval) m_pInner.Copy(), _easeFunc);

            return pCopy;
        }
        return new CCEaseCustom(this);
    }
}