using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCLayerMultiplexTests
{
    [Fact]
    public void SwitchTo_ReleasedLayer_ReturnsNull()
    {
        // SwitchToAndReleaseMe leaves a null entry for the layer it releases. Switching
        // back to that layer passed the null to AddChild (C2D-272).
        var multiplex = new CCLayerMultiplex(new CCLayer(), new CCLayer());
        multiplex.SwitchTo(0);
        multiplex.SwitchToAndReleaseMe(1); // releases layer 0

        Assert.Null(multiplex.SwitchTo(0));
        Assert.Null(multiplex.ActiveLayer);
    }

    [Fact]
    public void SwitchTo_AfterTheActiveLayerWasReleased_SwitchesWithoutThrowing()
    {
        // Releasing the active layer itself leaves its entry null. Switching away with an
        // out action then ran the action on that null (C2D-272).
        var second = new CCLayer();
        var multiplex = new CCLayerMultiplex(null, new CCFadeOut(1), new CCLayer(), second);
        multiplex.SwitchTo(0);
        multiplex.SwitchToAndReleaseMe(0);

        Assert.Same(second, multiplex.SwitchTo(1));
    }
}
