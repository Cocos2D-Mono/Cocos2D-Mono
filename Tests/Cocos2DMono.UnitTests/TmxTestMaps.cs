using System;
using System.IO;
using System.Text;
using Cocos2D;

namespace Cocos2DMono.UnitTests;

// Small TMX maps for tests, read from a stream so no content files or graphics device are needed.
internal static class TmxTestMaps
{
    public const string TilesetWithoutImage =
        "<tileset firstgid=\"1\" name=\"tiles\" tilewidth=\"8\" tileheight=\"8\"/>";

    public const string TilesetWithImage =
        "<tileset firstgid=\"1\" name=\"tiles\" tilewidth=\"8\" tileheight=\"8\">" +
        "<image source=\"tiles.png\" width=\"16\" height=\"16\"/></tileset>";

    // A 2x2 layer, its tiles' global IDs given as base64 of 32-bit little-endian values.
    public static string Layer(string name, params uint[] gids)
    {
        return "<layer name=\"" + name + "\" width=\"2\" height=\"2\"><data encoding=\"base64\">" +
               Convert.ToBase64String(TileBytes(gids)) + "</data></layer>";
    }

    // The same layer with its data gzip-compressed, optionally with a bad checksum.
    public static string GzipLayer(string name, bool corruptChecksum, params uint[] gids)
    {
        return "<layer name=\"" + name + "\" width=\"2\" height=\"2\"><data encoding=\"base64\" compression=\"gzip\">" +
               Convert.ToBase64String(GzipTestData.Compress(TileBytes(gids), corruptChecksum)) + "</data></layer>";
    }

    private static byte[] TileBytes(uint[] gids)
    {
        var bytes = new byte[gids.Length * 4];
        for (int i = 0; i < gids.Length; i++)
        {
            BitConverter.GetBytes(gids[i]).CopyTo(bytes, i * 4);
        }
        return bytes;
    }

    // A 2x2 orthogonal map with 8x8 tiles holding the given elements.
    public static CCTMXMapInfo Parse(string body)
    {
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                     "<map version=\"1.0\" orientation=\"orthogonal\" width=\"2\" height=\"2\" tilewidth=\"8\" tileheight=\"8\">" +
                     body + "</map>";
        return new CCTMXMapInfo(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(xml))));
    }
}
