using System.IO;
using System.Text;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the sprite frames, animations and their
// caches (C2D-286). Textures are created without loading an image, which needs no graphics
// device.
public class CCSpriteFrameCacheTests
{
    [Fact]
    public void SpriteFrameCache_AddFromADictionaryWithoutFrames_AddsNothing()
    {
        // The frames dictionary was read as null and then iterated.
        var cache = new CCSpriteFrameCache();
        cache.Init();

        cache.AddSpriteFramesWithDictionary(new PlistDictionary(), new CCTexture2D());

        Assert.Null(cache.SpriteFrameByName("missing"));
    }

    [Fact]
    public void SpriteSheet_FromADictionaryWithoutFrames_IsEmpty()
    {
        var sheet = new CCSpriteSheet(new PlistDictionary(), new CCTexture2D());

        Assert.Empty(sheet.Frames);
    }

    [Fact]
    public void SpriteSheet_FromASpriteKitPlistWithoutImages_IsEmpty()
    {
        // The SpriteKit loader iterated the images array without checking it was there.
        const string plist =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<plist version=\"1.0\"><dict>" +
            "<key>format</key><string>APPL</string>" +
            "<key>version</key><integer>1</integer>" +
            "</dict></plist>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(plist));

        var sheet = new CCSpriteSheet(stream, new CCTexture2D());

        Assert.Empty(sheet.Frames);
    }

    [Fact]
    public void SpriteFrameCache_RemoveFramesFromTexture_SkipsFramesWithoutATexture()
    {
        // A frame made with the parameterless constructor has no texture, and reading its
        // texture's name threw.
        var cache = new CCSpriteFrameCache();
        cache.Init();
        var texture = new CCTexture2D();
        cache.AddSpriteFrame(new CCSpriteFrame(), "blank");
        cache.AddSpriteFrame(new CCSpriteFrame(texture, new CCRect(0, 0, 1, 1)), "textured");

        cache.RemoveSpriteFramesFromTexture(texture);

        Assert.NotNull(cache.SpriteFrameByName("blank"));
        Assert.Null(cache.SpriteFrameByName("textured"));
    }

    [Fact]
    public void SpriteFrameCache_BeforeInit_FindsNothing()
    {
        // The frame dictionaries were created by Init, so a cache made with new threw.
        Assert.Null(new CCSpriteFrameCache().SpriteFrameByName("missing"));
    }

    [Fact]
    public void AnimationCache_BeforeInit_FindsNothing()
    {
        Assert.Null(new CCAnimationCache().AnimationByName("missing"));
    }
}
