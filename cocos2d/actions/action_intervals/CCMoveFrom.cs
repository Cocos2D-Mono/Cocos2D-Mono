#nullable disable
using System;
using Cocos2D;

namespace cocos2d.actions.action_intervals;

public class CCMoveFrom : CCFiniteTimeAction
{
    readonly CCPoint _targetFrom;
    CCPoint _targetTo;

    public CCMoveFrom(CCPoint ccFrom, float duration) : base(duration)
    {
        _targetFrom = ccFrom;
    }

    public override CCFiniteTimeAction Reverse()
    {
        throw new NotImplementedException();
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        _targetTo = target.Position;
    }

    public override void Update(float time)
    {
        if (Target != null)
        {
            // calculate current position
            var newPointX = _targetFrom.X + ((_targetTo.X - _targetFrom.X) * time);
            var newPointY = _targetFrom.Y + ((_targetTo.Y - _targetFrom.Y) * time);

            Target.Position = new CCPoint(newPointX, newPointY);
        }
    }
}
