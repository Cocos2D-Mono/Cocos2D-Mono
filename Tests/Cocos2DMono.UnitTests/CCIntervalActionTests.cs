using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the interval actions' nullable conversion (C2D-278). Like CCActionTests,
// actions start with RunAction on a node that isn't running; the ones that need the action
// manager's own update resume the node first.
public class CCIntervalActionTests : IDisposable
{
    [Fact]
    public void Repeat_OfBlink_FinishesWithoutThrowing()
    {
        // CCRepeat stops its inner action when the last repeat ends, and the action manager
        // then stops the CCRepeat, which stops it again. CCBlink.Stop dereferenced the target
        // that the first stop had cleared.
        var node = new CCNode();
        var manager = CCDirector.SharedDirector.ActionManager;
        node.RunAction(new CCRepeat(new CCBlink(0.2f, 2), 2));
        manager.ResumeTarget(node);

        for (int i = 0; i < 20; i++)
        {
            manager.Update(0.05f);
        }

        Assert.Equal(0, manager.NumberOfRunningActionsInTarget(node));
        Assert.True(node.Visible);
    }

    public static TheoryData<string, Func<CCFiniteTimeAction, CCFiniteTimeAction>> Containers => new()
    {
        { "CCSequence", part => new CCSequence(part, new CCDelayTime(1)) },
        { "CCSpawn", part => new CCSpawn(part, new CCDelayTime(1)) },
        { "CCRepeat", part => new CCRepeat(part, 2) },
        { "CCParallel", part => new CCParallel(part) },
        { "CCTargetedAction", part => new CCTargetedAction(new CCNode(), part) },
    };

    [Theory]
    [MemberData(nameof(Containers))]
    public void Reverse_OfAContainerWithAPartThatHasNoReverse_ThrowsNotSupported(
        string container, Func<CCFiniteTimeAction, CCFiniteTimeAction> wrap)
    {
        // CCFiniteTimeAction.Reverse returns null. A container used to build itself from that
        // null and fail later with NullReferenceException (or an assert in Debug builds).
        var action = wrap(new NoReverse());

        var thrown = Record.Exception(() => action.Reverse());

        Assert.True(thrown is NotSupportedException, container + " threw " + thrown?.GetType().Name);
    }

    public static TheoryData<string, Func<CCAction>> CopyableActions => new()
    {
        { "CCJumpBy", () => new CCJumpBy(1, CCPoint.Zero, 10, 1) },
        { "CCJumpTo", () => new CCJumpTo(1, CCPoint.Zero, 10, 1) },
        { "CCMoveBy", () => new CCMoveBy(1, CCPoint.Zero) },
        { "CCRepeat", () => new CCRepeat(new CCDelayTime(1), 2) },
        { "CCRepeatForever", () => new CCRepeatForever(new CCDelayTime(1)) },
        { "CCRotateBy", () => new CCRotateBy(1, 90) },
        { "CCRotateTo", () => new CCRotateTo(1, 90) },
        { "CCSequence", () => new CCSequence(new CCDelayTime(1), new CCDelayTime(1)) },
        { "CCSpawn", () => new CCSpawn(new CCDelayTime(1), new CCDelayTime(1)) },
        { "CCTintBy", () => new CCTintBy(1, 1, 1, 1) },
        { "CCTintTo", () => new CCTintTo(1, 1, 1, 1) },
        { "CCBezierBy", () => new CCBezierBy(1, new CCBezierConfig()) },
        { "CCBezierTo", () => new CCBezierTo(1, new CCBezierConfig()) },
        { "CCParallel", () => new CCParallel(new CCDelayTime(1)) },
        { "CCReverseTime", () => new CCReverseTime(new CCDelayTime(1)) },
        { "CCScaleBy", () => new CCScaleBy(1, 2) },
        { "CCScaleTo", () => new CCScaleTo(1, 2) },
    };

    [Theory]
    [MemberData(nameof(CopyableActions))]
    public void Copy_IntoAZoneOfAnotherType_ThrowsInvalidCast(string action, Func<CCAction> create)
    {
        // Copy(zone) returned null for a zone of the wrong type in some actions and threw
        // NullReferenceException in others. It now casts the zone, like the rest. The zone is
        // an interval action, so the base Copy's own cast doesn't fail first.
        var thrown = Record.Exception(() => create().Copy(new CCDelayTime(1)));

        Assert.True(thrown is InvalidCastException, action + " threw " + (thrown?.GetType().Name ?? "nothing"));
    }

    public void Dispose()
    {
        // RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }

    // Derives from CCFiniteTimeAction directly and keeps the base Reverse, which returns null.
    private sealed class NoReverse : CCFiniteTimeAction
    {
        public NoReverse() : base(1)
        {
        }
    }
}
