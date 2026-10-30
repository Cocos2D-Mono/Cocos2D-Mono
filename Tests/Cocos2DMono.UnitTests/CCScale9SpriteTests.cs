using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the controls' nullable conversion (C2D-305).
public class CCScale9SpriteTests
{
    [Fact]
    public void CapInsets_OnAnEmptySprite_DoesNothing()
    {
        // Setting them sliced the missing image, which threw NullReferenceException.
        var sprite = new CCScale9Sprite();

        sprite.CapInsets = new CCRect(1, 1, 1, 1);

        Assert.Equal(CCRect.Zero, sprite.CapInsets);
    }

    [Fact]
    public void Constructor_WithAFrameNameThatIsNotInTheCache_ThrowsArgumentException()
    {
        // The missing frame was passed on as null, which threw NullReferenceException.
        var exception = Assert.Throws<ArgumentException>(() => new CCScale9Sprite("no-such-frame.png"));

        Assert.Equal("spriteFrameName", exception.ParamName);
    }

    [Fact]
    public void FramesAndSpritesWithoutATexture_ThrowArgumentException()
    {
        // The missing texture was passed to a sprite batch node, which threw
        // NullReferenceException.
        Assert.Equal("spriteFrame",
                     Assert.Throws<ArgumentException>(() => new CCScale9Sprite(new CCSpriteFrame())).ParamName);
        Assert.Equal("spriteFrame",
                     Assert.Throws<ArgumentException>(() => new CCScale9Sprite().SetSpriteFrame(new CCSpriteFrame())).ParamName);
        Assert.Equal("sprite",
                     Assert.Throws<ArgumentException>(() => new CCScale9Sprite(new CCSprite(), CCRect.Zero, CCRect.Zero)).ParamName);
    }
}
