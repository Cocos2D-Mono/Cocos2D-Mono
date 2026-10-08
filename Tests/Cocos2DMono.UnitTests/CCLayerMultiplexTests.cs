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

    [Fact]
    public void FirstNextPrevious_AfterTheLayerListConstructor_CycleThroughTheLayers()
    {
        // The constructors that take a layer list never recorded the layer order, so these
        // methods returned null (C2D-273).
        var a = new CCLayer();
        var b = new CCLayer();
        var c = new CCLayer();
        var multiplex = new CCLayerMultiplex(a, b, c);

        Assert.Same(a, multiplex.SwitchToFirstLayer());
        Assert.Same(b, multiplex.SwitchToNextLayer());
        Assert.Same(c, multiplex.SwitchToNextLayer());
        Assert.Same(a, multiplex.SwitchToNextLayer());
        Assert.Same(c, multiplex.SwitchToPreviousLayer());
    }

    [Fact]
    public void AddLayer_AfterATaggedLayer_UsesTheNextIndex()
    {
        // AddLayer numbered layers by a count that included the tag aliases, so the layer
        // after a tagged one got index 2 (C2D-273).
        var a = new CCLayer { Tag = 7 };
        var b = new CCLayer();
        var multiplex = new CCLayerMultiplex();
        multiplex.AddLayer(a);
        multiplex.AddLayer(b);

        Assert.Same(b, multiplex.SwitchTo(1));
    }

    [Fact]
    public void SwitchToAndReleaseMe_ActiveLayer_DetachesItAndShowsNothing()
    {
        // Releasing the active layer itself left it attached and still marked active
        // (C2D-273).
        var a = new CCLayer();
        var b = new CCLayer();
        var multiplex = new CCLayerMultiplex(a, b);
        multiplex.SwitchTo(0);

        Assert.Null(multiplex.SwitchToAndReleaseMe(0));
        Assert.Null(a.Parent);
        Assert.Null(multiplex.ActiveLayer);

        Assert.Same(b, multiplex.SwitchTo(1));
        Assert.Null(a.Parent);
    }

    [Fact]
    public void SwitchToAndReleaseMe_TaggedLayer_ReleasesItsTagToo()
    {
        // The release cleared only the active key, so SwitchTo(tag) still reached the layer
        // through its tag alias (C2D-273).
        var a = new CCLayer { Tag = 7 };
        var multiplex = new CCLayerMultiplex(a, new CCLayer());
        multiplex.SwitchTo(0);
        multiplex.SwitchToAndReleaseMe(1);

        Assert.Null(multiplex.SwitchTo(7));
    }

    [Fact]
    public void NextAndPrevious_SkipReleasedLayers()
    {
        // Both used to land on a released layer and show nothing (C2D-273).
        var a = new CCLayer();
        var b = new CCLayer();
        var c = new CCLayer();
        var multiplex = new CCLayerMultiplex(a, b, c);
        multiplex.SwitchTo(1);
        multiplex.SwitchToAndReleaseMe(2); // shows c, releases b

        Assert.Same(a, multiplex.SwitchToPreviousLayer());
        Assert.Same(c, multiplex.SwitchToNextLayer());
    }

    [Fact]
    public void OnEnter_AfterTheFirstLayerWasReleased_ShowsTheFirstRemainingLayer()
    {
        // OnEnter switched to index 0 even when that layer had been released, and then
        // showed nothing (C2D-273).
        var b = new CCLayer();
        var multiplex = new CCLayerMultiplex(new CCLayer(), b);
        multiplex.SwitchTo(0);
        multiplex.SwitchToAndReleaseMe(0);

        multiplex.OnEnter();
        try
        {
            Assert.Same(b, multiplex.ActiveLayer);
        }
        finally
        {
            multiplex.OnExit();
        }
    }
}
