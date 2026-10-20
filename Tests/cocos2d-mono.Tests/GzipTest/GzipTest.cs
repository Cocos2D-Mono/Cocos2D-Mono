using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Cocos2D;

namespace tests;

/// <summary>
/// Checks the engine's gzip paths on the device: a TMX map with a gzip layer, particle systems
/// whose textures are embedded as gzip data, and ZipUtils.InflateMemory with valid data and with
/// a bad checksum. Each check shows PASS or FAIL, and is logged with a "GzipTest:" prefix.
/// </summary>
public class GzipTest : CCLayer
{
    // From decompressing orthogonal-test1.tmx's only layer with Python's zlib: 62 tiles whose
    // global IDs add up to 1677.
    private const int ExpectedTiles = 62;
    private const long ExpectedGidSum = 1677;

    private float _nextResultY;
    private int _checks;
    private int _failures;

    public override bool Init()
    {
        base.Init();
        CCSize s = CCDirector.SharedDirector.WinSize;

        var title = new CCLabelTTF("Gzip Test - System.IO.Compression", "arial", 24);
        title.Position = new CCPoint(s.Width / 2, s.Height - 40);
        AddChild(title, 100);

        // A map whose only layer is gzip-compressed, scaled to fit the left half.
        var map = new CCTMXTiledMap("TileMaps/orthogonal-test1.tmx");
        map.Scale = Math.Min(s.Width * 0.45f / map.ContentSize.Width, (s.Height - 100) / map.ContentSize.Height);
        map.Position = new CCPoint(20, 20);
        AddChild(map, 0);

        // Particle systems whose textures exist only as gzip data inside their plists.
        var galaxy = new CCParticleSystemQuad("Particles/Galaxy.plist");
        galaxy.Position = new CCPoint(s.Width * 0.62f, s.Height * 0.72f);
        AddChild(galaxy, 1);
        var upsidedown = new CCParticleSystemQuad("Particles/Upsidedown.plist");
        upsidedown.Position = new CCPoint(s.Width * 0.85f, s.Height * 0.72f);
        AddChild(upsidedown, 1);

        _nextResultY = s.Height * 0.45f;

        CCTMXLayer layer = map.LayerNamed("Layer 0");
        uint[] tiles = layer != null ? layer.Tiles : null;
        int tileCount = tiles != null ? tiles.Count(gid => gid != 0) : 0;
        long gidSum = tiles != null ? tiles.Sum(gid => (long) gid) : 0;
        Check("TMX gzip layer: " + tileCount + " tiles, GID sum " + gidSum,
              tileCount == ExpectedTiles && gidSum == ExpectedGidSum);

        Check("Particle textures from gzip: " + SizeOf(galaxy) + " and " + SizeOf(upsidedown),
              SizeOf(galaxy) == "32x32" && SizeOf(upsidedown) == "64x64");

        byte[] text = Encoding.ASCII.GetBytes("hello world");
        byte[] valid = Compress(text, false);
        var output = new byte[64];
        int inflated = ZipUtils.InflateMemory(valid, (uint) valid.Length, output);
        Check("ZipUtils.InflateMemory, valid data", inflated != 0 && output.Take(text.Length).SequenceEqual(text));

        byte[] corrupt = Compress(text, true);
        Check("ZipUtils.InflateMemory, bad checksum rejected",
              ZipUtils.InflateMemory(corrupt, (uint) corrupt.Length, new byte[64]) == 0);

        AddResult(_failures == 0 ? "All " + _checks + " checks passed" : _failures + " of " + _checks + " checks failed",
                  _failures == 0);
        return true;
    }

    private void Check(string what, bool passed)
    {
        _checks++;
        if (!passed)
        {
            _failures++;
        }
        AddResult((passed ? "PASS  " : "FAIL  ") + what, passed);
    }

    private void AddResult(string text, bool passed)
    {
        CCLog.Log("GzipTest: " + text);

        var label = new CCLabelTTF(text, "arial", 20);
        label.AnchorPoint = new CCPoint(0, 0.5f);
        label.Position = new CCPoint(CCDirector.SharedDirector.WinSize.Width * 0.5f + 20, _nextResultY);
        label.Color = passed ? new CCColor3B(80, 220, 80) : new CCColor3B(240, 70, 70);
        AddChild(label, 100);
        _nextResultY -= 34;
    }

    private static string SizeOf(CCParticleSystem system)
    {
        CCTexture2D texture = system.Texture;
        return texture != null ? texture.PixelsWide + "x" + texture.PixelsHigh : "no texture";
    }

    // Gzip data made by the BCL's compressor. With corruptChecksum, the CRC-32 in the trailer no
    // longer matches, so the data still decompresses but fails its integrity check.
    private static byte[] Compress(byte[] data, bool corruptChecksum)
    {
        var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
        {
            gzip.Write(data, 0, data.Length);
        }
        byte[] bytes = output.ToArray();
        if (corruptChecksum)
        {
            // The trailer is the CRC-32 of the data, then its length, 4 bytes each.
            bytes[bytes.Length - 8] ^= 0xFF;
        }
        return bytes;
    }
}
