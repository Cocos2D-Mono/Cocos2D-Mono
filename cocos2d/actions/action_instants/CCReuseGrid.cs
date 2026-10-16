namespace Cocos2D;

public class CCReuseGrid : CCActionInstant
{
    protected int m_nTimes;

    protected virtual bool InitWithTimes(int times)
    {
        m_nTimes = times;
        return true;
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);

        if (target.Grid != null && target.Grid.Active)
        {
            target.Grid.ReuseGrid += m_nTimes;
        }
    }

    public CCReuseGrid()
    {
    }

    public CCReuseGrid(int times)
    {
        InitWithTimes(times);
    }
}