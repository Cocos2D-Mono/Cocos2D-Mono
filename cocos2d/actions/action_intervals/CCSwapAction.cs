using cocos2d.misc_nodes;
using Cocos2D;

namespace cocos2d.actions.action_intervals;

public class CCSwapAction : CCParallel
{
    #region Constructors
    public CCSwapAction(CCSwappableNode firstCard, CCSwappableNode secondCard, float duration = 0.5f) : base(new CCFiniteTimeAction[0])
    {
        var relativePoint = (firstCard.CurrentTargetPosition - secondCard.CurrentTargetPosition);
        var distance = CCPoint.Distance(firstCard.CurrentTargetPosition, secondCard.CurrentTargetPosition);
        var angleBetween = relativePoint.Angle;

        var temp = new CCPoint2D(relativePoint.X, relativePoint.Y);
        temp.Length = (firstCard.ContentSize.Height > 0) ? firstCard.ContentSize.Height * 0.8f : distance * 0.25f;
        temp.Angle = angleBetween + CCMathHelper.Pi_2;

        var bezierConfig = new CCBezierConfig();
        bezierConfig.ControlPoint1 = firstCard.CurrentTargetPosition + new CCPoint(temp.X, temp.Y);
        bezierConfig.ControlPoint2 = secondCard.CurrentTargetPosition + new CCPoint(temp.X, temp.Y);
        bezierConfig.EndPosition = secondCard.CurrentTargetPosition;


        var moveFirst = new CCBezierTo(duration, bezierConfig);

        temp.Angle = angleBetween - CCMathHelper.Pi_2;
        bezierConfig = new CCBezierConfig();
        bezierConfig.ControlPoint1 = secondCard.CurrentTargetPosition + new CCPoint(temp.X, temp.Y);
        bezierConfig.ControlPoint2 = firstCard.CurrentTargetPosition + new CCPoint(temp.X, temp.Y);
        bezierConfig.EndPosition = firstCard.CurrentTargetPosition;

        firstCard.CurrentTargetPosition = secondCard.CurrentTargetPosition;
        secondCard.CurrentTargetPosition = bezierConfig.EndPosition;

        var moveSecond = new CCBezierTo(duration, bezierConfig);


        var actions = new CCFiniteTimeAction[] { new CCTargetedAction(firstCard, moveFirst), new CCTargetedAction(secondCard, moveSecond) };


        // Can't call base(duration) because max action duration needs to be determined here
        float maxDuration = 0.0f;
        foreach (CCFiniteTimeAction action in actions)
        {
            if (action.Duration > maxDuration)
            {
                maxDuration = action.Duration;
            }
        }
        Duration = maxDuration;

        Actions = actions;

        for (int i = 0; i < Actions.Length; i++)
        {
            var actionDuration = Actions[i].Duration;
            if (actionDuration < Duration)
            {
                Actions[i] = new CCSequence(Actions[i], new CCDelayTime(Duration - actionDuration));
            }
        }
    }
    #endregion Constructors

    protected internal override void StartWithTarget(CCNode target)
    {
        for (int i = 0; i < Actions.Length; i++)
        {
            Actions[i] = new CCTargetedAction(target, (CCTargetedAction)Actions[i]);
        }
        base.StartWithTarget(target);
    }

}
