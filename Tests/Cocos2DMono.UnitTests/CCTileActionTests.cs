using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the tile actions' seeds (C2D-285). A real tiled grid needs a graphics
// device, so the test actions replace their grid with a stand-in; starting them only shuffles.
public class CCTileActionTests : IDisposable
{
    [Fact]
    public void ShuffleTiles_WithTheSameSeed_ShufflesTheSameWay()
    {
        var a = new SeededShuffle(42);
        var b = new SeededShuffle(42);

        new CCNode().RunAction(a);
        new CCNode().RunAction(b);

        Assert.Equal(a.Order, b.Order);
    }

    [Fact]
    public void ShuffleTiles_WhenStarted_KeepsItsSeed()
    {
        // Starting replaced the seed with a random number.
        var action = new SeededShuffle(42);

        new CCNode().RunAction(action);

        Assert.Equal(42, action.Seed);
    }

    [Fact]
    public void TurnOffTiles_WithTheSameSeed_TurnsTilesOffInTheSameOrder()
    {
        var a = new SeededTurnOff(42);
        var b = new SeededTurnOff(42);

        new CCNode().RunAction(a);
        new CCNode().RunAction(b);

        Assert.Equal(a.Order, b.Order);
    }

    [Fact]
    public void TurnOffTiles_WithANegativeSeed_Starts()
    {
        // The seed was passed to CCRandom.Next as an upper bound, which a negative number isn't.
        var action = new SeededTurnOff(-5);

        var thrown = Record.Exception(() => new CCNode().RunAction(action));

        Assert.Null(thrown);
    }

    [Fact]
    public void TurnOffTiles_WithoutASeed_TurnsTilesOffInANewOrderEachRun()
    {
        // The constructor without a seed is unseeded, as it was in practice before seeds worked.
        // With 100 tiles, two runs matching by chance is vanishingly unlikely.
        var a = new SeededTurnOff();
        var b = new SeededTurnOff();

        new CCNode().RunAction(a);
        new CCNode().RunAction(b);

        Assert.NotEqual(a.Order, b.Order);
    }

    public void Dispose()
    {
        // RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }

    // Stands in for a tiled grid, which needs a graphics device.
    private sealed class StandInGrid : CCGridBase
    {
        public override void Blit()
        {
        }

        public override void Reuse()
        {
        }

        public override void CalculateVertexPoints()
        {
        }
    }

    private sealed class SeededShuffle : CCShuffleTiles
    {
        public SeededShuffle(int seed) : base(new CCGridSize(10, 10), 1, seed)
        {
        }

        public override CCGridBase Grid
        {
            get => new StandInGrid();
            set { }
        }

        public int[] Order => m_pTilesOrder;
        public int Seed => m_nSeed;
    }

    private sealed class SeededTurnOff : CCTurnOffTiles
    {
        public SeededTurnOff() : base(1, new CCGridSize(10, 10))
        {
        }

        public SeededTurnOff(int seed) : base(1, new CCGridSize(10, 10), seed)
        {
        }

        public override CCGridBase Grid
        {
            get => new StandInGrid();
            set { }
        }

        public int[] Order => m_pTilesOrder;
    }
}
