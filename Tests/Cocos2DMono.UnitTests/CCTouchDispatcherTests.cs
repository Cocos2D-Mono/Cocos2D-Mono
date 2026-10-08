using System;
using System.Collections.Generic;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCTouchDispatcherTests
{
    [Fact]
    public void AddAndRemoveDelegate_BeforeInit_Works()
    {
        // The handler lists used to be created only by Init(), so a dispatcher used
        // before it threw a null reference error (C2D-270).
        var dispatcher = new CCTouchDispatcher();
        var target = new Target();

        dispatcher.AddTargetedDelegate(target, 0, true);
        Assert.NotNull(dispatcher.FindHandler(target));

        dispatcher.RemoveDelegate(target);
        Assert.Null(dispatcher.FindHandler(target));
    }

    [Fact]
    public void SetPriority_ForDelegateAddedDuringDispatch_AppliesWhenItIsAdded()
    {
        // A delegate added during a dispatch waits in a queue until the dispatch ends.
        // SetPriority only looked in the registered lists, so it threw (C2D-271).
        var dispatcher = NewDispatcher();
        var popup = new Target();
        var opener = new Target(() =>
        {
            dispatcher.AddTargetedDelegate(popup, 0, true);
            dispatcher.SetPriority(-10, popup);
        });
        dispatcher.AddTargetedDelegate(opener, 0, true);

        Touch(dispatcher);

        Assert.Equal(-10, dispatcher.FindHandler(popup)?.Priority);
    }

    [Fact]
    public void SetPriority_DuringDispatch_AppliesFromTheNextTouch()
    {
        // SetPriority sorted the handlers while the dispatch loop was iterating them,
        // which threw InvalidOperationException (C2D-271).
        var dispatcher = NewDispatcher();
        var order = new List<string>();
        var second = new Target(() => order.Add("second"));
        var first = new Target(() =>
        {
            order.Add("first");
            dispatcher.SetPriority(-10, second);
        });
        dispatcher.AddTargetedDelegate(first, 0, true);
        dispatcher.AddTargetedDelegate(second, 1, true);

        Touch(dispatcher);
        Assert.Equal(new[] { "first", "second" }, order);

        order.Clear();
        Touch(dispatcher);
        Assert.Equal(new[] { "second", "first" }, order);
    }

    [Fact]
    public void SetPriority_UnregisteredDelegate_ThrowsArgumentException()
    {
        var dispatcher = NewDispatcher();

        Assert.Throws<ArgumentException>(() => dispatcher.SetPriority(-10, new Target()));
    }

    [Fact]
    public void UpdateGraphPriority_ForDelegateAddedDuringDispatch_AppliesWhenItIsAdded()
    {
        // UpdateGraphPriority skipped a delegate still queued from the same dispatch, so
        // it was added with its old priority (C2D-271).
        var dispatcher = NewDispatcher();
        var popup = new Target();
        var opener = new Target(() =>
        {
            dispatcher.AddTargetedDelegate(popup, 0, true);
            popup.TouchPriority = 5;
            dispatcher.UpdateGraphPriority(popup);
        });
        dispatcher.AddTargetedDelegate(opener, 0, true);

        Touch(dispatcher);

        Assert.Equal(5, dispatcher.FindHandler(popup)?.Priority);
    }

    private static CCTouchDispatcher NewDispatcher()
    {
        var dispatcher = new CCTouchDispatcher();
        dispatcher.Init();
        return dispatcher;
    }

    private static void Touch(CCTouchDispatcher dispatcher)
    {
        dispatcher.TouchesBegan(new List<CCTouch> { new CCTouch(1, 0, 0) });
    }

    private sealed class Target : ICCTargetedTouchDelegate
    {
        private readonly Action? _onBegan;

        public Target(Action? onBegan = null)
        {
            _onBegan = onBegan;
        }

        public int TouchPriority { get; set; }
        public bool VisibleForTouches { get; set; } = true;

        // Never claims the touch, so every target sees it.
        public bool TouchBegan(CCTouch pTouch)
        {
            _onBegan?.Invoke();
            return false;
        }

        public void TouchMoved(CCTouch pTouch) { }
        public void TouchEnded(CCTouch pTouch) { }
        public void TouchCancelled(CCTouch pTouch) { }
    }
}
