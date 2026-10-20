using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the misc nodes' nullable conversion (C2D-299). A progress timer's sprite is never
// null, so a null sprite is rejected. The constructor used to accept one and make a timer that
// drew nothing, and replacing a sprite with null threw NullReferenceException after clearing it.
public class CCProgressTimerTests
{
    [Fact]
    public void Constructor_ANullSprite_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CCProgressTimer((CCSprite)null!));
    }

    [Fact]
    public void Sprite_SetToNull_ThrowsAndKeepsTheSprite()
    {
        var sprite = new CCSprite();
        var timer = new CCProgressTimer(sprite);

        Assert.Throws<ArgumentNullException>(() => timer.Sprite = null!);
        Assert.Same(sprite, timer.Sprite);
    }

    [Fact]
    public void Sprite_SetToAnotherSprite_TakesItsSize()
    {
        var timer = new CCProgressTimer(new CCSprite());
        var other = new CCSprite { ContentSize = new CCSize(30, 20) };

        timer.Sprite = other;

        Assert.Same(other, timer.Sprite);
        Assert.Equal(new CCSize(30, 20), timer.ContentSize);
    }
}
