using Cocos2D;
using cocos2d.actions.action_intervals;
using Xunit;

namespace Cocos2DMono.UnitTests;

// These four actions override StartWithTarget without calling the base method, so they never
// had a target (C2D-277). Like CCActionTests, each test starts the action with RunAction on a
// node that isn't running, then drives it with Update.
public class CCCustomActionTests : System.IDisposable
{
    [Fact]
    public void MoveFrom_MovesItsTarget()
    {
        var node = new CCNode();
        var move = new CCMoveFrom(new CCPoint(10, 20), 1);
        node.RunAction(move);

        move.Update(0f);
        Assert.Equal(new CCPoint(10, 20), node.Position);

        move.Update(1f);
        Assert.Equal(CCPoint.Zero, node.Position);
    }

    [Fact]
    public void ColorBlendAnimation_BlendsItsTargetsColor()
    {
        var node = new CCNode();
        var blend = new CCColorBlendAnimation(1, new CCColor3B(10, 20, 30));
        node.RunAction(blend);

        blend.Update(1f);

        Assert.Equal(new CCColor3B(10, 20, 30), node.Color);
    }

    [Fact]
    public void RotateAnimation_RotatesItsTarget()
    {
        // This used to throw NullReferenceException on RunAction.
        var node = new CCNode();
        var rotate = new CCRotateAnimation(1, 90);
        node.RunAction(rotate);

        rotate.Update(0.5f);

        Assert.Equal(45f, node.Rotation);
    }

    [Fact]
    public void RotateAnimation_StartsFromCurrentRotationWhenTheTargetProvidesIt()
    {
        var node = new RotationNode { CurrentRotation = 30 };
        var rotate = new CCRotateAnimation(1, 90);
        node.RunAction(rotate);

        rotate.Update(0f);

        Assert.Equal(30f, node.Rotation);
    }

    [Fact]
    public void TimerAction_HasItsTarget()
    {
        var node = new TimerNode();
        var timer = new CCTimerAction(2);
        node.RunAction(timer);

        Assert.Same(node, timer.Target);
        timer.Update(0.5f);
        Assert.Equal(1f, node.Elapsed);
    }

    public void Dispose()
    {
        // RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }

    private sealed class RotationNode : CCNode, ICCRotationAnimationGetter
    {
        public float CurrentRotation { get; set; }
    }

    private sealed class TimerNode : CCNode, ITimerActionListener
    {
        public float Elapsed;

        public void TimerActionUpdate(float elapsedTime, float totalTime)
        {
            Elapsed = elapsedTime;
        }
    }
}
