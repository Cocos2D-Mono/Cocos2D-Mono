using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the tile maps' nullable conversion (C2D-300). A layer with no tiles has no tileset,
// and building a map with one threw NullReferenceException; such a layer is now skipped, as in
// cocos2d-x 3. LayerNamed threw NullReferenceException on a map with no layers.
public class CCTMXTiledMapTests
{
    [Fact]
    public void Constructor_AMapWithAnEmptyLayer_SkipsTheLayer()
    {
        var map = new CCTMXTiledMap(TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage + TmxTestMaps.Layer("empty", 0, 0, 0, 0)));

        Assert.Equal(0, map.ChildrenCount);
        Assert.Null(map.LayerNamed("empty"));
    }

    [Fact]
    public void LayerNamed_OnAMapWithNoLayers_ReturnsNull()
    {
        var map = new CCTMXTiledMap(TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage));

        Assert.Null(map.LayerNamed("ground"));
    }
}
