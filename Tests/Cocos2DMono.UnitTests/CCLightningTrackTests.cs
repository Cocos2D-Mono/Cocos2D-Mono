using System.Collections.Generic;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCLightningTrackTests
{
    [Fact]
    public void GetPoint_OverrideSkippingBaseCreateBolt_FallsBackInsteadOfThrowing()
    {
        // The base CreateBolt fills the segment lengths GetPoint walks. An override that
        // skipped it used to leave them null, so GetPoint threw (C2D-268).
        var track = new StraightTrack(new CCPoint(0f, 0f), new CCPoint(10f, 0f));

        Assert.Null(Record.Exception(() => track.GetPoint(0.5f)));
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.25f, 2.5f)]
    [InlineData(0.5f, 5f)]
    [InlineData(1f, 10f)]
    public void GetPoint_Fallback_InterpolatesOnTheSegmentContainingThePosition(float position, float expectedX)
    {
        // The fallback picked the first segment whose start lay past the position, skipping
        // the one that contains it, so interior points on a straight track came back as the
        // end point (C2D-269).
        var track = new StraightTrack(new CCPoint(0f, 0f), new CCPoint(10f, 0f));

        CCPoint p = track.GetPoint(position);

        Assert.Equal(expectedX, p.X, 3);
        Assert.Equal(0f, p.Y, 3);
    }

    private sealed class StraightTrack : CCLightningTrack
    {
        public StraightTrack(CCPoint start, CCPoint end) : base(start, end)
        {
        }

        protected override List<CCPoint> CreateBolt(CCPoint source, CCPoint dest)
        {
            return new List<CCPoint> { source, dest };
        }
    }
}
