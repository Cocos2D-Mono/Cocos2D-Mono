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

    // The tests below come from the plist classes' nullable conversion (C2D-303).

    [Fact]
    public void SpriteFrameCache_AddAFormat3FrameWithoutAliases_AddsTheFrame()
    {
        // The aliases array was iterated without checking it was there.
        var cache = new CCSpriteFrameCache();

        cache.AddSpriteFramesWithDictionary(Format3Sheet(("a.png", null)), new CCTexture2D());

        Assert.NotNull(cache.SpriteFrameByName("a.png"));
    }

    [Fact]
    public void SpriteSheet_FromAFormat3FrameWithoutAliases_HasTheFrame()
    {
        // The aliases array was read without checking it was there.
        var sheet = new CCSpriteSheet(Format3Sheet(("a.png", null)), new CCTexture2D());

        Assert.Single(sheet.Frames);
    }

    [Fact]
    public void SpriteFrameCache_RemoveFramesWithADictionaryWithoutFrames_KeepsTheFrames()
    {
        // The frames dictionary was read as null and then iterated.
        var cache = new CCSpriteFrameCache();
        cache.AddSpriteFrame(new CCSpriteFrame(new CCTexture2D(), new CCRect(0, 0, 1, 1)), "a.png");

        cache.RemoveSpriteFramesFromDictionary(new PlistDictionary());

        Assert.NotNull(cache.SpriteFrameByName("a.png"));
    }

    [Fact]
    public void SpriteFrameCache_AddAFrameThatIsntADictionary_ThrowsInvalidDataException()
    {
        // The frame was read as a null dictionary, which threw NullReferenceException.
        var frames = new PlistDictionary();
        frames.Add("a.png", new PlistArray());
        var sheet = new PlistDictionary();
        sheet.Add("frames", frames);

        var ex = Assert.Throws<InvalidDataException>(
            () => new CCSpriteFrameCache().AddSpriteFramesWithDictionary(sheet, new CCTexture2D()));
        Assert.Contains("'a.png'", ex.Message);
    }

    [Fact]
    public void SpriteFrameCache_AddAFrameWithoutItsRect_ThrowsInvalidDataException()
    {
        // The missing rect was parsed as null, which threw NotSupportedException without saying
        // what was missing.
        var frame = new PlistDictionary();
        frame.Add("offset", new PlistString("{0,0}"));
        frame.Add("sourceSize", new PlistString("{1,1}"));
        var frames = new PlistDictionary();
        frames.Add("a.png", frame);
        var metadata = new PlistDictionary();
        metadata.Add("format", new PlistInteger(2));
        var sheet = new PlistDictionary();
        sheet.Add("metadata", metadata);
        sheet.Add("frames", frames);

        var ex = Assert.Throws<InvalidDataException>(
            () => new CCSpriteFrameCache().AddSpriteFramesWithDictionary(sheet, new CCTexture2D()));
        Assert.Contains("'frame'", ex.Message);
    }

    [Fact]
    public void AnimationCache_FromPropertiesWithoutSpriteSheets_AddsTheAnimation()
    {
        // The sprite sheets array was iterated without checking it was there. The animation finds
        // its frame in the shared frame cache.
        var entry = new PlistDictionary();
        entry.Add("spriteframe", new PlistString("c2d303-frame.png"));
        CCSpriteFrameCache.SharedSpriteFrameCache.AddSpriteFrame(
            new CCSpriteFrame(new CCTexture2D(), new CCRect(0, 0, 1, 1)), "c2d303-frame.png");
        try
        {
            CCAnimationCache.SharedAnimationCache.AddAnimationsWithDictionary(
                AnimationsPlist("c2d303-walk", entry, spriteSheets: null));

            Assert.NotNull(CCAnimationCache.SharedAnimationCache.AnimationByName("c2d303-walk"));
        }
        finally
        {
            CCAnimationCache.SharedAnimationCache.RemoveAnimationByName("c2d303-walk");
            CCSpriteFrameCache.SharedSpriteFrameCache.RemoveSpriteFrameByName("c2d303-frame.png");
        }
    }

    [Fact]
    public void AnimationCache_AFrameWithoutASpriteFrameName_ThrowsInvalidDataException()
    {
        // The missing name was looked up as null, which threw ArgumentNullException.
        var entry = new PlistDictionary();
        entry.Add("delayUnits", new PlistReal(1));

        var ex = Assert.Throws<InvalidDataException>(() => new CCAnimationCache().AddAnimationsWithDictionary(
            AnimationsPlist("c2d303-walk", entry, spriteSheets: new PlistArray())));
        Assert.Contains("'spriteframe'", ex.Message);
    }

    // A format-3 sprite sheet plist with 1x1 frames that have the given names and aliases. A frame
    // with null aliases has no aliases array.
    private static PlistDictionary Format3Sheet(params (string Name, string[]? Aliases)[] sheetFrames)
    {
        var frames = new PlistDictionary();
        foreach (var (name, frameAliases) in sheetFrames)
        {
            var frame = new PlistDictionary();
            frame.Add("spriteSize", new PlistString("{1,1}"));
            frame.Add("spriteOffset", new PlistString("{0,0}"));
            frame.Add("spriteSourceSize", new PlistString("{1,1}"));
            frame.Add("textureRect", new PlistString("{{0,0},{1,1}}"));
            if (frameAliases != null)
            {
                var aliases = new PlistArray();
                foreach (string alias in frameAliases)
                {
                    aliases.Add(new PlistString(alias));
                }
                frame.Add("aliases", aliases);
            }
            frames.Add(name, frame);
        }

        var metadata = new PlistDictionary();
        metadata.Add("format", new PlistInteger(3));

        var sheet = new PlistDictionary();
        sheet.Add("metadata", metadata);
        sheet.Add("frames", frames);
        return sheet;
    }

    // A format-2 animations plist with one animation of one frame. Its properties have no sprite
    // sheets array when spriteSheets is null.
    private static PlistDictionary AnimationsPlist(string name, PlistDictionary frame, PlistArray? spriteSheets)
    {
        var frames = new PlistArray();
        frames.Add(frame);
        var animation = new PlistDictionary();
        animation.Add("frames", frames);
        var animations = new PlistDictionary();
        animations.Add(name, animation);

        var properties = new PlistDictionary();
        properties.Add("format", new PlistInteger(2));
        if (spriteSheets != null)
        {
            properties.Add("spritesheets", spriteSheets);
        }

        var plist = new PlistDictionary();
        plist.Add("animations", animations);
        plist.Add("properties", properties);
        return plist;
    }
}
