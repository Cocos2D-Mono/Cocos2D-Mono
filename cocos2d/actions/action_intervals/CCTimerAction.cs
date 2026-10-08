using Cocos2D;

namespace cocos2d.actions.action_intervals;

public class CCTimerAction : CCFiniteTimeAction
{
    readonly float _duration;
    ITimerActionListener? _castedActionTarget;

    public CCTimerAction(float duration) : base(duration)
    {
        _duration = duration;
    }

    public override CCFiniteTimeAction Reverse()
    {
        return new CCTimerAction(_duration);
    }

    protected internal override void StartWithTarget(CCNode target)
    {
        base.StartWithTarget(target);
        _castedActionTarget = target as ITimerActionListener;
    }

    public override void Update(float time)
    {
        _castedActionTarget?.TimerActionUpdate(_duration * time, _duration);
    }
}

public interface ITimerActionListener
{
    void TimerActionUpdate(float elapsedTime, float totalTime);
}
