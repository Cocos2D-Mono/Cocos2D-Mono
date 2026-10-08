using System;
using Cocos2D;

namespace cocos2d.actions.action_intervals;

public class CCRotateAnimation : CCFiniteTimeAction
{
    readonly float _targetRotation;
    float _startAngle;

    public CCRotateAnimation(float duration, float rotation) : base(duration)
    {
        _targetRotation = rotation;
    }

    public override CCFiniteTimeAction Reverse()
    {
        throw new NotImplementedException();
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        _startAngle = target is ICCRotationAnimationGetter getter ? getter.CurrentRotation : target.Rotation;
    }

    public override void Update(float time)
    {
        if (Target != null)
        {
            // calculate current percentage
            Target.Rotation = _startAngle + (_targetRotation - _startAngle) * time;
        }
    }
}
