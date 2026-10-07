#nullable disable
namespace Cocos2D;

public class CCPlace : CCActionInstant
{
    private CCPoint _position;

    protected CCPlace()
    {
    }

    protected CCPlace(CCPlace place) : base(place)
    {
        InitWithPosition(place._position);
    }

    public CCPlace(CCPoint pos)
    {
        InitWithPosition(pos);
    }

    protected virtual bool InitWithPosition(CCPoint pos)
    {
        _position = pos;
        return true;
    }

    public override object Copy(ICCCopyable pZone)
    {
        if (pZone != null)
        {
            var pRet = (CCPlace) (pZone);
            base.Copy(pZone);
            pRet.InitWithPosition(_position);
            return pRet;
        }
        return new CCPlace(this);
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        m_pTarget.Position = _position;
    }
}