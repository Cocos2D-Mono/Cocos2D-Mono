using System;
using System.Collections.Generic;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for copying the cardinal spline and Catmull-Rom actions (C2D-282). Only
// CCCardinalSplineTo overrode Copy, so copying any of its subclasses returned the base class.
public class CCSplineActionTests : IDisposable
{
    private static List<CCPoint> Points() => new() { new CCPoint(0, 0), new CCPoint(10, 0), new CCPoint(20, 0) };

    public static TheoryData<string, Func<CCAction>> Splines => new()
    {
        { "CCCardinalSplineTo", () => new CCCardinalSplineTo(1, Points(), 0.5f) },
        { "CCCardinalSplineBy", () => new CCCardinalSplineBy(1, Points(), 0.5f) },
        { "CCCatmullRomTo", () => new CCCatmullRomTo(1, Points()) },
        { "CCCatmullRomBy", () => new CCCatmullRomBy(1, Points()) },
    };

    [Theory]
    [MemberData(nameof(Splines))]
    public void Copy_ReturnsTheSameType(string spline, Func<CCAction> create)
    {
        var action = create();

        var copy = action.Copy();

        Assert.True(copy.GetType() == action.GetType(), spline + " copied as " + copy.GetType().Name);
    }

    [Fact]
    public void CardinalSplineBy_Copy_MovesRelativeToTheStart()
    {
        // The copy was a CCCardinalSplineTo, which moved the node to the spline's absolute
        // points.
        var node = new CCNode { Position = new CCPoint(100, 100) };
        var copy = (CCActionInterval) new CCCardinalSplineBy(1, Points(), 0.5f).Copy();
        node.RunAction(copy);

        copy.Update(1);

        Assert.Equal(new CCPoint(120, 100), node.Position);
    }

    public void Dispose()
    {
        // RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }
}
