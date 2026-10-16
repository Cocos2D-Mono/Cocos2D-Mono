using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the ease, camera, spline and progress actions
// (C2D-280). Like CCActionTests, actions start with RunAction on a node that isn't running and
// are driven with Update.
public class CCEaseActionTests : IDisposable
{
    private const int P = 4;

    [Fact]
    public void EaseIn_MovesTheNodeAlongItsCurve()
    {
        // The ease starts its inner action on the same target and feeds it the eased time.
        var node = new CCNode { Position = new CCPoint(0f, 0f) };
        var ease = new CCEaseIn(new CCMoveTo(1f, new CCPoint(100f, 0f)), 2f);
        node.RunAction(ease);

        ease.Update(0.5f);
        Assert.Equal(25f, node.Position.X, P);
        ease.Update(1f);
        Assert.Equal(100f, node.Position.X, P);
    }

    // The eases whose reverse wraps the reverse of their inner action.
    public static TheoryData<string, Func<CCFiniteTimeAction, CCFiniteTimeAction>> Eases => new()
    {
        { "CCActionEase", inner => new CCActionEase(inner) },
        { "CCEaseBackIn", inner => new CCEaseBackIn(inner) },
        { "CCEaseBackInOut", inner => new CCEaseBackInOut(inner) },
        { "CCEaseBackOut", inner => new CCEaseBackOut(inner) },
        { "CCEaseBounceIn", inner => new CCEaseBounceIn(inner) },
        { "CCEaseBounceInOut", inner => new CCEaseBounceInOut(inner) },
        { "CCEaseBounceOut", inner => new CCEaseBounceOut(inner) },
        { "CCEaseElasticIn", inner => new CCEaseElasticIn(inner) },
        { "CCEaseElasticInOut", inner => new CCEaseElasticInOut(inner) },
        { "CCEaseElasticOut", inner => new CCEaseElasticOut(inner) },
        { "CCEaseExponentialIn", inner => new CCEaseExponentialIn(inner) },
        { "CCEaseExponentialInOut", inner => new CCEaseExponentialInOut(inner) },
        { "CCEaseExponentialOut", inner => new CCEaseExponentialOut(inner) },
        { "CCEaseIn", inner => new CCEaseIn(inner, 2) },
        { "CCEaseInOut", inner => new CCEaseInOut(inner, 2) },
        { "CCEaseOut", inner => new CCEaseOut(inner, 2) },
        { "CCEaseRateAction", inner => new CCEaseRateAction(inner, 2) },
        { "CCEaseSineIn", inner => new CCEaseSineIn(inner) },
        { "CCEaseSineInOut", inner => new CCEaseSineInOut(inner) },
        { "CCEaseSineOut", inner => new CCEaseSineOut(inner) },
    };

    [Theory]
    [MemberData(nameof(Eases))]
    public void Reverse_OfAnEaseWhoseInnerActionHasNoReverse_ThrowsNotSupported(
        string ease, Func<CCFiniteTimeAction, CCFiniteTimeAction> wrap)
    {
        // CCFiniteTimeAction.Reverse returns null. An ease passed that null to the constructor
        // of its reverse, which threw NullReferenceException.
        var action = wrap(new NoReverse());

        var thrown = Record.Exception(() => action.Reverse());

        Assert.True(thrown is NotSupportedException, ease + " threw " + (thrown?.GetType().Name ?? "nothing"));
    }

    [Theory]
    [MemberData(nameof(Eases))]
    public void Reverse_OfAnEaseWhoseInnerActionHasAReverse_WrapsIt(
        string ease, Func<CCFiniteTimeAction, CCFiniteTimeAction> wrap)
    {
        var reverse = wrap(new CCDelayTime(2)).Reverse();

        Assert.NotNull(reverse);
        Assert.Equal(2f, reverse.Duration, P);
    }

    [Fact]
    public void EaseElastic_Reverse_ThrowsNotSupported()
    {
        // The base elastic ease has no curve of its own to reverse; its subclasses do. Its
        // Reverse returned null.
        var ease = new CCEaseElastic(new CCDelayTime(1));

        Assert.Throws<NotSupportedException>(() => ease.Reverse());
    }

    // The actions whose Copy(zone) used "as" on the zone.
    public static TheoryData<string, Func<CCAction>> CopyableActions => new()
    {
        { "CCActionEase", () => new CCActionEase(new CCDelayTime(1)) },
        { "CCEaseBackIn", () => new CCEaseBackIn(new CCDelayTime(1)) },
        { "CCEaseBackInOut", () => new CCEaseBackInOut(new CCDelayTime(1)) },
        { "CCEaseBackOut", () => new CCEaseBackOut(new CCDelayTime(1)) },
        { "CCEaseBounceIn", () => new CCEaseBounceIn(new CCDelayTime(1)) },
        { "CCEaseBounceInOut", () => new CCEaseBounceInOut(new CCDelayTime(1)) },
        { "CCEaseBounceOut", () => new CCEaseBounceOut(new CCDelayTime(1)) },
        { "CCEaseCustom", () => new CCEaseCustom(new CCDelayTime(1), t => t) },
        { "CCEaseElastic", () => new CCEaseElastic(new CCDelayTime(1)) },
        { "CCEaseElasticIn", () => new CCEaseElasticIn(new CCDelayTime(1)) },
        { "CCEaseElasticInOut", () => new CCEaseElasticInOut(new CCDelayTime(1)) },
        { "CCEaseElasticOut", () => new CCEaseElasticOut(new CCDelayTime(1)) },
        { "CCEaseExponentialIn", () => new CCEaseExponentialIn(new CCDelayTime(1)) },
        { "CCEaseExponentialInOut", () => new CCEaseExponentialInOut(new CCDelayTime(1)) },
        { "CCEaseExponentialOut", () => new CCEaseExponentialOut(new CCDelayTime(1)) },
        { "CCEaseIn", () => new CCEaseIn(new CCDelayTime(1), 2) },
        { "CCEaseInOut", () => new CCEaseInOut(new CCDelayTime(1), 2) },
        { "CCEaseSineOut", () => new CCEaseSineOut(new CCDelayTime(1)) },
        { "CCOrbitCamera", () => new CCOrbitCamera(1, 1, 0, 0, 90, 0, 0) },
    };

    [Theory]
    [MemberData(nameof(CopyableActions))]
    public void Copy_IntoAZoneOfAnotherType_ThrowsInvalidCast(string action, Func<CCAction> create)
    {
        // Copy(zone) threw NullReferenceException for a zone of the wrong type. It now casts the
        // zone, like the other actions. The zone is an interval action, so the base Copy's own
        // cast doesn't fail first.
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
