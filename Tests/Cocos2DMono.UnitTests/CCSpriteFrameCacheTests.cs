using System;
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

    [Fact]
    public void SpriteFrameCache_RemoveByName_RemovesAFrameWithoutAnAlias()
    {
        // Removing read the alias dictionary with its indexer, which threw for a name that
        // isn't an alias (C2D-287).
        var cache = new CCSpriteFrameCache();
        cache.AddSpriteFrame(new CCSpriteFrame(new CCTexture2D(), new CCRect(0, 0, 1, 1)), "plain.png");

        cache.RemoveSpriteFrameByName("plain.png");

        Assert.Null(cache.SpriteFrameByName("plain.png"));
    }

    [Fact]
    public void SpriteFrameCache_RemoveByAlias_RemovesTheFrameAndTheAlias()
    {
        // Removing by alias removed the frame, then an alias keyed by the frame's name instead
        // of the alias, so the alias found a frame added later under that name (C2D-287).
        var cache = new CCSpriteFrameCache();
        cache.AddSpriteFramesWithDictionary(Format3Sheet(("a.png", new[] { "alias-a" })), new CCTexture2D());
        Assert.NotNull(cache.SpriteFrameByName("alias-a"));

        cache.RemoveSpriteFrameByName("alias-a");
        cache.AddSpriteFrame(new CCSpriteFrame(new CCTexture2D(), new CCRect(0, 0, 1, 1)), "a.png");

        Assert.Null(cache.SpriteFrameByName("alias-a"));
    }

    [Fact]
    public void SpriteFrameCache_RemoveByName_AlsoRemovesTheFramesAliases()
    {
        // Removing a frame by its own name left its aliases, which then found a frame added
        // later under that name (C2D-287).
        var cache = new CCSpriteFrameCache();
        cache.AddSpriteFramesWithDictionary(Format3Sheet(("a.png", new[] { "alias-a" })), new CCTexture2D());

        cache.RemoveSpriteFrameByName("a.png");
        cache.AddSpriteFrame(new CCSpriteFrame(new CCTexture2D(), new CCRect(0, 0, 1, 1)), "a.png");

        Assert.Null(cache.SpriteFrameByName("alias-a"));
    }

    [Fact]
    public void SpriteFrameCache_RemoveByName_PrefersAFrameOverAnAliasWithTheSameName()
    {
        // "b.png" names a frame and is also an alias of "a.png". SpriteFrameByName finds the
        // frame "b.png", so removing "b.png" has to remove that frame, not "a.png" (C2D-287).
        var cache = new CCSpriteFrameCache();
        cache.AddSpriteFramesWithDictionary(
            Format3Sheet(("a.png", new[] { "b.png" }), ("b.png", Array.Empty<string>())), new CCTexture2D());

        cache.RemoveSpriteFrameByName("b.png");

        Assert.NotNull(cache.SpriteFrameByName("a.png"));
    }

    // A format-3 sprite sheet plist with 1x1 frames that have the given names and aliases.
    private static PlistDictionary Format3Sheet(params (string Name, string[] Aliases)[] sheetFrames)
    {
        var frames = new PlistDictionary();
        foreach (var (name, frameAliases) in sheetFrames)
        {
            var aliases = new PlistArray();
            foreach (string alias in frameAliases)
            {
                aliases.Add(new PlistString(alias));
            }

            var frame = new PlistDictionary();
            frame.Add("spriteSize", new PlistString("{1,1}"));
            frame.Add("spriteOffset", new PlistString("{0,0}"));
            frame.Add("spriteSourceSize", new PlistString("{1,1}"));
            frame.Add("textureRect", new PlistString("{{0,0},{1,1}}"));
            frame.Add("aliases", aliases);
            frames.Add(name, frame);
        }

        var metadata = new PlistDictionary();
        metadata.Add("format", new PlistInteger(3));

        var sheet = new PlistDictionary();
        sheet.Add("metadata", metadata);
        sheet.Add("frames", frames);
        return sheet;
    }
}
