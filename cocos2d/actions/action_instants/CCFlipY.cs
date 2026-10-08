namespace Cocos2D;

public class CCFlipY : CCActionInstant
{
    private bool _flipY;

    public CCFlipY()
    {
    }

    public CCFlipY(bool y)
    {
        InitWithFlipY(y);
    }

    protected virtual bool InitWithFlipY(bool y)
    {
        _flipY = y;
        return true;
    }

    protected CCFlipY(CCFlipY flipY) : base(flipY)
    {
        InitWithFlipY(flipY._flipY);
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        ((CCSprite) (target)).FlipY = _flipY;
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCFlipY(!_flipY);
    }

    public override object Copy(ICCCopyable? pZone)
    {
        if (pZone != null)
        {
            var pRet = (CCFlipY) (pZone);
            base.Copy(pZone);
            pRet.InitWithFlipY(_flipY);
            return pRet;
        }
        return new CCFlipY(this);
    }
}