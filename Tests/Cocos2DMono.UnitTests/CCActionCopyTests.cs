using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the instant actions' copy constructors.
//
// Each of these actions exposes Copy(ICCCopyable). When passed null it routes to the
// protected copy constructor - `return new CCFlipX(this)` - which is the only way to
// reach that constructor from outside. The copied action's state is private, so these
// tests observe it the way a consumer would: run the copy and assert on the target.
public class CCActionCopyTests : System.IDisposable
{
    [Fact]
    public void CCPlace_Copy_PreservesPosition()
    {
        var expected = new CCPoint(10f, 20f);
        var place = new CCPlace(expected);

        var copy = (CCPlace)place.Copy(null);

        var node = new CCNode();
        node.RunAction(copy);
        Assert.Equal(expected, node.Position);
    }

    [Fact]
    public void CCFlipX_Copy_PreservesFlip()
    {
        var flip = new CCFlipX(true);

        var copy = (CCFlipX)flip.Copy(null);

        var sprite = new CCSprite();
        sprite.RunAction(copy);
        Assert.True(sprite.FlipX);
    }

    [Fact]
    public void CCFlipY_Copy_PreservesFlip()
    {
        var flip = new CCFlipY(true);

        var copy = (CCFlipY)flip.Copy(null);

        var sprite = new CCSprite();
        sprite.RunAction(copy);
        Assert.True(sprite.FlipY);
    }

    [Fact]
    public void CCScaleTo_CopyIntoZone_FillsTheZoneAndKeepsTheOriginal()
    {
        // Copy(zone) copied the zone's end scale into the original instead of the other way
        // round (C2D-279). CCScaleBy's Copy(zone) relies on it.
        var original = new CCScaleTo(1, 2f);
        var zone = new CCScaleTo(1, 5f);

        original.Copy(zone);

        Assert.Equal(2f, ScaleAtEnd(zone));
        Assert.Equal(2f, ScaleAtEnd(original));
    }

    [Fact]
    public void CCReverseTime_CopyIntoZone_FillsTheZoneAndKeepsTheOriginal()
    {
        // Copy(zone) copied the zone's inner action into the original (C2D-279).
        var original = new CCReverseTime(new CCMoveBy(1, new CCPoint(10, 0)));
        var zone = new CCReverseTime(new CCMoveBy(1, new CCPoint(0, 10)));

        original.Copy(zone);

        Assert.Equal(new CCPoint(10, 0), PositionAtStart(zone));
        Assert.Equal(new CCPoint(10, 0), PositionAtStart(original));
    }

    [Fact]
    public void CCReverseTime_Copy_DoesNotShareTheInnerAction()
    {
        // The copy constructor shared the inner action, so running the original and its copy
        // on two nodes moved only the node started last (C2D-279).
        var original = new CCReverseTime(new CCMoveBy(1, new CCPoint(10, 0)));
        var copy = (CCReverseTime)original.Copy();
        var first = new CCNode();
        var second = new CCNode();
        first.RunAction(original);
        second.RunAction(copy);

        original.Update(0f);

        Assert.Equal(new CCPoint(10, 0), first.Position);
    }

    [Fact]
    public void CCOrbitCamera_CopyIntoZone_FillsTheZoneAndKeepsTheOriginal()
    {
        // Copy(zone) set the original's orbit from the zone instead of the other way round
        // (C2D-281), as CCScaleTo's did.
        // Every orbit parameter differs, so a copy that drops any of them fails too.
        var original = new CCOrbitCamera(1, 2, 1, 45, 10, 20, 5);
        var zone = new CCOrbitCamera(1, 10, -3, 70, -7, 60, -11);
        float expected = EyeXAtEnd(new CCOrbitCamera(1, 2, 1, 45, 10, 20, 5));

        original.Copy(zone);

        Assert.Equal(expected, EyeXAtEnd(zone));
        Assert.Equal(expected, EyeXAtEnd(original));
    }

    // Runs the action on a fresh node and returns the node's scale at the action's end.
    private static float ScaleAtEnd(CCFiniteTimeAction action)
    {
        var node = new CCNode();
        node.RunAction(action);
        action.Update(1f);
        return node.ScaleX;
    }

    // Runs the action on a fresh node and returns the node's position at the action's start.
    // A CCReverseTime starts where its inner action ends.
    private static CCPoint PositionAtStart(CCFiniteTimeAction action)
    {
        var node = new CCNode();
        node.RunAction(action);
        action.Update(0f);
        return node.Position;
    }

    // Runs the camera action on a fresh node and returns the camera's eye X at the action's end,
    // which depends on every orbit parameter.
    private static float EyeXAtEnd(CCOrbitCamera action)
    {
        var node = new CCNode();
        node.RunAction(action);
        action.Update(1f);
        node.Camera.GetEyeXyz(out float x, out _, out _);
        return x;
    }

    [Fact]
    public void CCCallFuncN_InitWithTarget_ReturnsTrueOnSuccess()
    {
        // Matches CCCallFuncO.InitWithTarget, which returns true.
        var callFuncN = new CCCallFuncN(_ => { });

        Assert.True(callFuncN.InitWithTarget(_ => { }));
    }

    [Fact]
    public void CCCallFuncN_Copy_PreservesSelector()
    {
        var invoked = false;
        var callFuncN = new CCCallFuncN(_ => invoked = true);

        var copy = (CCCallFuncN)callFuncN.Copy(null);

        // CCCallFunc fires its selector in Update (not StartWithTarget), so drive it:
        // RunAction sets the target, then Update triggers Execute.
        var node = new CCNode();
        node.RunAction(copy);
        copy.Update(1f);
        Assert.True(invoked);
    }

    public void Dispose()
    {
        // CCNode.RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }
}
