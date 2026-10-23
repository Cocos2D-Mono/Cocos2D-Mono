using System.IO;
using System.Linq;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the tile maps' nullable conversion (C2D-300). A map read from a stream has no file
// name, which made its first tileset image throw ArgumentNullException. Layer data that failed to
// decompress, and elements outside the element they belong to, threw NullReferenceException.
// The gzip layer tests come from replacing the bundled gzip port with System.IO.Compression
// (C2D-316). The port never checked a layer's gzip checksum, so corrupt data became wrong tiles.
// The zlib layer tests come from replacing the bundled zlib port the same way (C2D-322).
public class CCTMXMapInfoTests
{
    [Fact]
    public void Parse_AWellFormedMap_ReadsItsTilesAndObjects()
    {
        var info = TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage + TmxTestMaps.Layer("ground", 1, 0, 0, 2) +
                                     "<objectgroup name=\"things\"><object name=\"spawn\" x=\"8\" y=\"0\"/></objectgroup>");

        Assert.Equal(new uint[] { 1, 0, 0, 2 }, info.Layers.Single().Tiles);
        Assert.Equal("8", info.ObjectGroups.Single().Objects.Single()["x"]);
    }

    [Fact]
    public void Parse_AStreamMapWithATilesetImage_TakesTheImageNameAsGiven()
    {
        var info = TmxTestMaps.Parse(TmxTestMaps.TilesetWithImage);

        Assert.Equal("tiles", info.Tilesets.Single().m_sSourceImage);
    }

    [Fact]
    public void Parse_LayerDataThatCantBeDecompressed_LeavesTheLayerEmpty()
    {
        var info = TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage +
                                     "<layer name=\"bad\" width=\"2\" height=\"2\">" +
                                     "<data encoding=\"base64\" compression=\"gzip\">AAECAwQFBgcICQ==</data></layer>");

        Assert.All(info.Layers.Single().Tiles, gid => Assert.Equal(0u, gid));
    }

    [Fact]
    public void Parse_AGzipLayer_ReadsItsTiles()
    {
        var info = TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage +
                                     TmxTestMaps.GzipLayer("ground", corruptChecksum: false, 1, 0, 0, 2));

        Assert.Equal(new uint[] { 1, 0, 0, 2 }, info.Layers.Single().Tiles);
    }

    [Fact]
    public void Parse_AGzipLayerWithABadChecksum_LeavesTheLayerEmpty()
    {
        var info = TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage +
                                     TmxTestMaps.GzipLayer("ground", corruptChecksum: true, 1, 0, 0, 2));

        Assert.All(info.Layers.Single().Tiles, gid => Assert.Equal(0u, gid));
    }

    [Fact]
    public void Parse_AZlibLayer_ReadsItsTiles()
    {
        var info = TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage +
                                     TmxTestMaps.ZlibLayer("ground", corruptChecksum: false, 1, 0, 0, 2));

        Assert.Equal(new uint[] { 1, 0, 0, 2 }, info.Layers.Single().Tiles);
    }

    [Fact]
    public void Parse_AZlibLayerWithABadChecksum_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() => TmxTestMaps.Parse(TmxTestMaps.TilesetWithoutImage +
                                                                   TmxTestMaps.ZlibLayer("ground", corruptChecksum: true, 1, 0, 0, 2)));
    }

    [Theory]
    [InlineData("<tile id=\"0\"/>", "<tile>")]
    [InlineData("<image source=\"tiles.png\"/>", "<image>")]
    [InlineData("<data encoding=\"base64\">AAAAAAAAAAAAAAAAAAAAAA==</data>", "<data>")]
    [InlineData("<object x=\"0\" y=\"0\"/>", "<object>")]
    public void Parse_AnElementOutsideItsParent_ThrowsInvalidDataException(string element, string named)
    {
        var exception = Assert.Throws<InvalidDataException>(() => TmxTestMaps.Parse(element));

        Assert.Contains(named, exception.Message);
    }
}
