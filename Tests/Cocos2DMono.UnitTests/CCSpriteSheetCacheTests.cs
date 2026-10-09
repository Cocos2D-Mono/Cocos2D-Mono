using System;
using System.IO;
using System.Text;
using Cocos2D;
using Cocos2D.sprite_nodes;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for CCSpriteSheetCache's stream and dictionary overloads (C2D-288). They
// built the sheet by loading a plist named after the cache key, ignoring the data they were
// given, and the test host has no content to load.
public class CCSpriteSheetCacheTests : IDisposable
{
    [Fact]
    public void AddSpriteSheet_FromADictionary_BuildsTheSheetFromIt()
    {
        var frame = new PlistDictionary();
        frame.Add("frame", new PlistString("{{0,0},{1,1}}"));
        frame.Add("offset", new PlistString("{0,0}"));
        frame.Add("sourceSize", new PlistString("{1,1}"));
        var frames = new PlistDictionary();
        frames.Add("a.png", frame);
        var metadata = new PlistDictionary();
        metadata.Add("format", new PlistInteger(2));
        var plist = new PlistDictionary();
        plist.Add("metadata", metadata);
        plist.Add("frames", frames);

        var sheet = CCSpriteSheetCache.Instance.AddSpriteSheet(plist, new CCTexture2D(), "dictionary-sheet");

        Assert.NotNull(sheet["a.png"]);
    }

    [Fact]
    public void AddSpriteSheet_FromAStream_BuildsTheSheetFromIt()
    {
        // Laid out like a real plist file: the parser expects whitespace between nested elements.
        const string plist = """
            <?xml version="1.0" encoding="UTF-8"?>
            <plist version="1.0">
            <dict>
                <key>frames</key>
                <dict>
                    <key>a.png</key>
                    <dict>
                        <key>frame</key>
                        <string>{{0,0},{1,1}}</string>
                        <key>offset</key>
                        <string>{0,0}</string>
                        <key>sourceSize</key>
                        <string>{1,1}</string>
                    </dict>
                </dict>
                <key>metadata</key>
                <dict>
                    <key>format</key>
                    <integer>2</integer>
                </dict>
            </dict>
            </plist>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(plist));

        var sheet = CCSpriteSheetCache.Instance.AddSpriteSheet(stream, new CCTexture2D(), "stream-sheet");

        Assert.NotNull(sheet["a.png"]);
    }

    public void Dispose()
    {
        // The cache is shared; drop it so each test starts empty.
        CCSpriteSheetCache.DestroyInstance();
    }
}
