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
