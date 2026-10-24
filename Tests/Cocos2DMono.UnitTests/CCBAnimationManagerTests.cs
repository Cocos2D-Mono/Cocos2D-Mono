using System;
using System.Collections.Generic;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the CCB reader's nullable conversion (C2D-305).
public class CCBAnimationManagerTests : IDisposable
{
    [Fact]
    public void SequenceCompleted_TellsTheDelegateWhichSequenceFinished()
    {
        // The manager cleared its running sequence and then read the sequence's name from it for
        // the delegate, which threw NullReferenceException.
        var root = new CCNode();
        var manager = new CCBAnimationManager { RootNode = root };
        manager.Sequences.Add(new CCBSequence { Name = "intro", SequenceId = 0, ChainedSequenceId = -1, Duration = 0.5f });
        string? completed = null;
        manager.Delegate = name => completed = name;

        manager.RunAnimationsForSequenceNamed("intro");
        // The root isn't in a running scene, so its actions start paused. An action's first
        // step only starts its clock, so the second one finishes the sequence.
        CCDirector.SharedDirector.ActionManager.ResumeTarget(root);
        CCDirector.SharedDirector.ActionManager.Update(0.1f);
        CCDirector.SharedDirector.ActionManager.Update(1f);

        Assert.Equal("intro", completed);
        Assert.Equal("intro", manager.GetLastCompletedSequenceName());
    }

    [Fact]
    public void RunAnimations_ForASequenceIdThatDoesNotExist_ThrowsArgumentException()
    {
        // The manager read the missing sequence's duration, which threw NullReferenceException.
        var manager = new CCBAnimationManager { RootNode = new CCNode() };

        var exception = Assert.Throws<ArgumentException>(() => manager.RunAnimationsForSequenceIdTweenDuration(7, 0));

        Assert.Equal("nSeqId", exception.ParamName);
    }

    [Fact]
    public void RunAnimations_ForASequenceIdThatDoesNotExist_LeavesTheRunningSequenceAlone()
    {
        // The manager stopped the running sequence's actions before it found that the new one
        // doesn't exist.
        var root = new CCNode();
        var manager = new CCBAnimationManager { RootNode = root };
        manager.Sequences.Add(new CCBSequence { Name = "intro", SequenceId = 0, ChainedSequenceId = -1, Duration = 0.5f });
        string? completed = null;
        manager.Delegate = name => completed = name;
        manager.RunAnimationsForSequenceNamed("intro");

        Assert.Throws<ArgumentException>(() => manager.RunAnimationsForSequenceIdTweenDuration(7, 0));
        CCDirector.SharedDirector.ActionManager.ResumeTarget(root);
        CCDirector.SharedDirector.ActionManager.Update(0.1f);
        CCDirector.SharedDirector.ActionManager.Update(1f);

        Assert.Equal("intro", completed);
    }

    [Fact]
    public void RunAnimationsForSequenceNamed_ForANameThatDoesNotExist_ThrowsArgumentException()
    {
        // The missing name became sequence ID -1, which failed an assert in debug builds before
        // the lookup could throw.
        var manager = new CCBAnimationManager { RootNode = new CCNode() };

        Assert.Throws<ArgumentException>(() => manager.RunAnimationsForSequenceNamed("missing"));
    }

    [Fact]
    public void RunAnimations_WithAMissingDisplayFrameAndNoKeyframes_LeavesTheSpriteAlone()
    {
        // A display frame that couldn't be found leaves a null base value, which failed an assert
        // in debug builds when a sequence without keyframes for it reset the sprite.
        var sprite = new CCSprite();
        var manager = new CCBAnimationManager { RootNode = sprite };
        manager.SetBaseValue(null, sprite, "displayFrame");
        var displayFrame = new CCBSequenceProperty { Name = "displayFrame", Type = CCBPropertyType.SpriteFrame };
        manager.AddNode(sprite, new Dictionary<int, Dictionary<string, CCBSequenceProperty>>
        {
            [0] = new Dictionary<string, CCBSequenceProperty> { ["displayFrame"] = displayFrame },
        });
        manager.Sequences.Add(new CCBSequence { Name = "intro", SequenceId = 0, ChainedSequenceId = -1 });

        manager.RunAnimationsForSequenceNamed("intro");

        Assert.Null(sprite.Texture);
    }

    [Fact]
    public void SetSpriteFrame_WithAFrameThatCouldNotBeFound_DoesNothing()
    {
        // A keyframe whose sprite frame isn't in the cache has no frame, and setting that as the
        // sprite's display frame threw NullReferenceException.
        var sprite = new CCSprite();
        var action = new CCBSetSpriteFrame(null);
        sprite.RunAction(action);

        action.Update(1);

        Assert.Null(sprite.Texture);
    }

    public void Dispose()
    {
        // RunAction registers actions with the shared ActionManager; clean it up to keep tests isolated.
        CCDirector.SharedDirector.ActionManager.RemoveAllActions();
    }
}
