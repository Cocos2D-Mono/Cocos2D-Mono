using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the tile maps' nullable conversion (C2D-300). A layer draws with its tileset's
// image; a tileset with none used to fail inside the texture cache instead of saying so.
public class CCTMXLayerTests
{
    [Fact]
    public void Constructor_ATilesetWithNoImage_ThrowsArgumentException()
    {
        var mapInfo = TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage);
        var layerInfo = new CCTMXLayerInfo { Name = "ground", LayerSize = new CCSize(2, 2), Tiles = new uint[] { 1, 0, 0, 0 } };

        var exception = Assert.Throws<ArgumentException>(() => new CCTMXLayer(mapInfo.Tilesets[0], layerInfo, mapInfo));

        Assert.Equal("tilesetInfo", exception.ParamName);
        Assert.Contains("tiles", exception.Message);
    }
}
