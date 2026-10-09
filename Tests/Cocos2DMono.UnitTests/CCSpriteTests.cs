using System;
using System.Collections.Generic;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of CCSprite, CCSpriteBatchNode and the texture
// protocol (C2D-289). An initialized batch node needs a graphics device for its vertex buffer,
// so these use a batch node made with the parameterless constructor and a bare atlas.
public class CCSpriteTests : IDisposable
{
    [Fact]
    public void SortAllChildren_OnAChildlessSpriteMarkedForReorder_DoesNotThrow()
    {
        // The reorder flag can be set before the first child is added. Sorting then read the
        // missing child list.
        var sprite = new CCSprite();
        sprite.SetReorderChildDirtyRecursively();

        var thrown = Record.Exception(() => sprite.SortAllChildren());

        Assert.Null(thrown);
    }

    [Fact]
    public void IsSpriteFrameDisplayed_WithoutTextures_ComparesTheRest()
    {
        // A sprite made without a texture and a frame made without one both have no texture
        // to read a name from.
        var sprite = new CCSprite();

        Assert.True(sprite.IsSpriteFrameDisplayed(new CCSpriteFrame()));
    }

    [Fact]
    public void RemoveAllChildren_OnABatchedSpriteWithoutChildren_DoesNotThrow()
    {
        // A batched sprite took its child list's elements without checking it had one.
        var batch = new CCSpriteBatchNode { TextureAtlas = new CCTextureAtlas() };
        var sprite = new CCSprite { BatchNode = batch };

        var thrown = Record.Exception(() => sprite.RemoveAllChildren());

        Assert.Null(thrown);
    }

    [Fact]
    public void BatchNodeCompare_OrdersNullFirst()
    {
        // CCNode.Compare accepts nulls, as IComparer<T> allows; the batch node's override cast
        // them and threw.
        var batch = new CCSpriteBatchNode();
        var sprite = new CCSprite();

        Assert.Equal(0, batch.Compare(null, null));
        Assert.Equal(-1, batch.Compare(null, sprite));
        Assert.Equal(1, batch.Compare(sprite, null));
    }

    [Fact]
    public void Animate_StoppedAfterRestoreOriginalFrameIsTurnedOn_DoesNotThrow()
    {
        // The original frame is captured when the action starts, only if the animation
        // restores it. Turning that on later made Stop set a null display frame.
        var frame = new CCSpriteFrame(new CCTexture2D(), new CCRect(0, 0, 1, 1));
        var animation = new CCAnimation(new List<CCSpriteFrame> { frame }, 0.1f);
        var animate = new CCAnimate(animation);
        var sprite = new CCSprite();
        sprite.RunAction(animate);
        animation.RestoreOriginalFrame = true;

        var thrown = Record.Exception(() => animate.Stop());

        Assert.Null(thrown);
    }

    public void Dispose()
    {
        // RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }
}
