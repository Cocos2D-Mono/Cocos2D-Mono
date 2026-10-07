#nullable disable
using System;
using Cocos2D;

namespace cocos2d.actions.action_intervals;

public class CCColorBlendAnimation : CCFiniteTimeAction
{
    public readonly CCColor3B EndColor;

    CCColor3B _startColor;
    CCColor3B _interpolateColor;

    int sumR;
    int sumG;
    int sumB;

    public CCColorBlendAnimation(float duration, CCColor3B endColor) : base(duration)
    {
        this.EndColor = endColor;
    }

    public override CCFiniteTimeAction Reverse()
    {
        throw new System.NotImplementedException();
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        _startColor = target.Color;

        var endColor = EndColor;

        sumR = endColor.R - _startColor.R;
        sumG = endColor.G - _startColor.G;
        sumB = endColor.B - _startColor.B;

        _interpolateColor = new CCColor3B(_startColor.R, _startColor.G, _startColor.B);
    }

    public override void Update(float time)
    {
        if (Target != null)
        {
            _interpolateColor.R = (byte)(_startColor.R + (int)(sumR * time));
            _interpolateColor.G = (byte)(_startColor.G + (int)(sumG * time));
            _interpolateColor.B = (byte)(_startColor.B + (int)(sumB * time));
            Target.Color = _interpolateColor;
        }
    }
}
