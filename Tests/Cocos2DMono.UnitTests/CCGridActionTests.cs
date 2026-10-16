using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the grid actions' nullable conversion (C2D-283). A real grid needs a
// graphics device, so these stop short of running a grid effect.
public class CCGridActionTests : IDisposable
{
    [Fact]
    public void GridAction_Grid_ThrowsNotSupported()
    {
        // The base grid action has no grid of its own. Its Grid getter returned null.
        var action = new CCGridAction(1, new CCGridSize(2, 2));

        Assert.Throws<NotSupportedException>(() => action.Grid);
    }

    [Fact]
    public void GridAction_WhenRun_ThrowsNotSupported()
    {
        // Starting it gave the node that null grid and threw NullReferenceException.
        var node = new CCNode();

        var thrown = Record.Exception(() => node.RunAction(new CCGridAction(1, new CCGridSize(2, 2))));

        Assert.True(thrown is NotSupportedException, "threw " + (thrown?.GetType().Name ?? "nothing"));
    }

    public static TheoryData<string, Func<CCAction, CCAction>> AmplitudeActions => new()
    {
        { "CCAccelAmplitude", inner => new CCAccelAmplitude(inner, 1) },
        { "CCAccelDeccelAmplitude", inner => new CCAccelDeccelAmplitude(inner, 1) },
        { "CCDeccelAmplitude", inner => new CCDeccelAmplitude(inner, 1) },
    };

    [Theory]
    [MemberData(nameof(AmplitudeActions))]
    public void AmplitudeAction_AroundAnInstantAction_ThrowsInvalidCast(string action, Func<CCAction, CCAction> wrap)
    {
        // The inner action must be an interval action. An "as" cast stored null instead, and
        // the amplitude action threw NullReferenceException when it started.
        var thrown = Record.Exception(() => wrap(new CCHide()));

        Assert.True(thrown is InvalidCastException, action + " threw " + (thrown?.GetType().Name ?? "nothing"));
    }

    [Fact]
    public void ShuffleTiles_BeforeItStarts_HasEmptyTileArrays()
    {
        var action = new ShuffleTilesProbe();

        Assert.Empty(action.Tiles);
        Assert.Empty(action.TilesOrder);
    }

    [Fact]
    public void TurnOffTiles_BeforeItStarts_HasAnEmptyTileOrder()
    {
        Assert.Empty(new TurnOffTilesProbe().TilesOrder);
        Assert.Empty(new TurnOffTilesProbe(seed: 5).TilesOrder);
    }

    [Theory]
    [MemberData(nameof(AmplitudeActions))]
    public void AmplitudeAction_Update_UpdatesItsInnerAction(string action, Func<CCAction, CCAction> wrap)
    {
        // CCAccelDeccelAmplitude set its inner action's amplitude but never updated it, so the
        // wrapped effect didn't animate (C2D-284). The other two update it.
        var inner = new UpdateRecorder();
        var amplitude = (CCActionInterval) wrap(inner);
        new CCNode().RunAction(amplitude);

        amplitude.Update(0.25f);

        Assert.True(inner.Updates == 1, action + " updated its inner action " + inner.Updates + " times");
        Assert.Equal(0.25f, inner.LastTime);
    }

    public void Dispose()
    {
        // RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }

    // Records the updates an amplitude action passes on to its inner action.
    private sealed class UpdateRecorder : CCActionInterval
    {
        public UpdateRecorder() : base(1)
        {
        }

        public int Updates;
        public float LastTime = -1;

        public override float AmplitudeRate { get; set; }

        public override void Update(float time)
        {
            Updates++;
            LastTime = time;
        }
    }

    // Expose the protected tile arrays, which were null until the action started.
    private sealed class ShuffleTilesProbe : CCShuffleTiles
    {
        public ShuffleTilesProbe() : base(new CCGridSize(2, 2), 1, 5)
        {
        }

        public CCTile[] Tiles => m_pTiles;
        public int[] TilesOrder => m_pTilesOrder;
    }

    private sealed class TurnOffTilesProbe : CCTurnOffTiles
    {
        public TurnOffTilesProbe() : base(1, new CCGridSize(2, 2))
        {
        }

        public TurnOffTilesProbe(int seed) : base(1, new CCGridSize(2, 2), seed)
        {
        }

        public int[] TilesOrder => m_pTilesOrder;
    }
}
