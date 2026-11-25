using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the runtime classes (C2D-303), and for the item
// that FocusNextItem and FocusPreviousItem move the focus to (C2D-330).
public class CCFocusManagerTests
{
    private sealed class Focusable : ICCFocusable
    {
        public bool CanReceiveFocus { get; init; } = true;
        public bool HasFocus { get; set; }
    }

    // A manager of its own for a test. Instance is shared, so items left in it would change what
    // the other tests see.
    private static CCFocusManager NewManager(params Focusable[] items)
    {
        var manager = (CCFocusManager)Activator.CreateInstance(typeof(CCFocusManager), nonPublic: true)!;
        foreach (Focusable item in items)
        {
            manager.Add(item);
        }
        return manager;
    }

    [Fact]
    public void FocusNextItem_AfterTheOnlyFocusedItemIsRemoved_LeavesNoFocus()
    {
        // The removed item stayed current, and moving on from it took the first item of an
        // empty list, which threw NullReferenceException.
        CCFocusManager manager = CCFocusManager.Instance;
        var item = new Focusable { HasFocus = true };
        manager.Add(item);
        manager.Remove(item);

        manager.FocusNextItem();

        Assert.False(item.HasFocus);
        Assert.Null(manager.ItemWithFocus);
    }

    [Fact]
    public void FocusPreviousItem_AfterTheOnlyFocusedItemIsRemoved_LeavesNoFocus()
    {
        // The same as FocusNextItem, with the last item of the empty list.
        CCFocusManager manager = CCFocusManager.Instance;
        var item = new Focusable { HasFocus = true };
        manager.Add(item);
        manager.Remove(item);

        manager.FocusPreviousItem();

        Assert.False(item.HasFocus);
        Assert.Null(manager.ItemWithFocus);
    }

    [Fact]
    public void FocusNextItem_MovesToTheNextItem()
    {
        // The search for the next item didn't stop at the first one it found, so the focus went
        // to the last item in the list.
        var a = new Focusable { HasFocus = true };
        var b = new Focusable();
        var c = new Focusable();
        CCFocusManager manager = NewManager(a, b, c);
        ICCFocusable? lost = null;
        ICCFocusable? gained = null;
        int changes = 0;
        manager.OnFocusChanged += (prev, current) =>
        {
            lost = prev;
            gained = current;
            changes++;
        };

        manager.FocusNextItem();

        Assert.Same(b, manager.ItemWithFocus);
        Assert.False(a.HasFocus);
        Assert.True(b.HasFocus);
        Assert.False(c.HasFocus);
        Assert.Equal(1, changes);
        Assert.Same(a, lost);
        Assert.Same(b, gained);
    }

    [Fact]
    public void FocusNextItem_SkipsItemsThatCannotReceiveFocus()
    {
        var a = new Focusable { HasFocus = true };
        var b = new Focusable { CanReceiveFocus = false };
        var c = new Focusable();
        var d = new Focusable();
        CCFocusManager manager = NewManager(a, b, c, d);

        manager.FocusNextItem();

        Assert.Same(c, manager.ItemWithFocus);
        Assert.False(b.HasFocus);
        Assert.False(d.HasFocus);
    }

    [Fact]
    public void FocusPreviousItem_SkipsAnItemThatCannotReceiveFocus()
    {
        // The search for an item that could receive focus found one, but the focus went to the
        // previous item anyway.
        var a = new Focusable();
        var b = new Focusable { CanReceiveFocus = false };
        var c = new Focusable { HasFocus = true };
        CCFocusManager manager = NewManager(a, b, c);

        manager.FocusPreviousItem();

        Assert.Same(a, manager.ItemWithFocus);
        Assert.True(a.HasFocus);
        Assert.False(b.HasFocus);
        Assert.False(c.HasFocus);
    }

    [Fact]
    public void FocusPreviousItem_MovesToTheNearestItemThatCanReceiveFocus()
    {
        // Its search didn't stop at the first item it found either, so it ended on the first
        // item in the list.
        var a = new Focusable();
        var b = new Focusable();
        var c = new Focusable { CanReceiveFocus = false };
        var d = new Focusable { HasFocus = true };
        CCFocusManager manager = NewManager(a, b, c, d);

        manager.FocusPreviousItem();

        Assert.Same(b, manager.ItemWithFocus);
        Assert.False(a.HasFocus);
        Assert.False(c.HasFocus);
    }

    [Fact]
    public void FocusNextItem_FromTheLastItem_GoesRoundToTheFirstItemThatCanReceiveFocus()
    {
        // Going round took the first item, whether it could receive focus or not.
        var a = new Focusable { CanReceiveFocus = false };
        var b = new Focusable();
        var c = new Focusable { HasFocus = true };
        CCFocusManager manager = NewManager(a, b, c);

        manager.FocusNextItem();

        Assert.Same(b, manager.ItemWithFocus);
        Assert.False(a.HasFocus);
    }

    [Fact]
    public void FocusPreviousItem_FromTheFirstItem_GoesRoundToTheLastItemThatCanReceiveFocus()
    {
        var a = new Focusable { HasFocus = true };
        var b = new Focusable();
        var c = new Focusable { CanReceiveFocus = false };
        CCFocusManager manager = NewManager(a, b, c);

        manager.FocusPreviousItem();

        Assert.Same(b, manager.ItemWithFocus);
        Assert.False(c.HasFocus);
    }

    [Fact]
    public void FocusNextItem_WithNoItemFocused_FocusesTheFirstItemThatCanReceiveFocus()
    {
        // With no item focused, it took the first item, whether it could receive focus or not.
        var a = new Focusable { CanReceiveFocus = false };
        var b = new Focusable();
        var c = new Focusable();
        CCFocusManager manager = NewManager(a, b, c);

        manager.FocusNextItem();

        Assert.Same(b, manager.ItemWithFocus);
        Assert.False(a.HasFocus);
    }

    [Fact]
    public void FocusPreviousItem_WithNoItemFocused_FocusesTheLastItemThatCanReceiveFocus()
    {
        var a = new Focusable();
        var b = new Focusable();
        var c = new Focusable { CanReceiveFocus = false };
        CCFocusManager manager = NewManager(a, b, c);

        manager.FocusPreviousItem();

        Assert.Same(b, manager.ItemWithFocus);
        Assert.False(c.HasFocus);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Moving_WhenNoItemCanReceiveFocus_LeavesNoFocus(bool next)
    {
        // The focused item has stopped being able to receive focus, and so has the other one. The
        // focus went to whichever item came next, or back to the same one.
        var a = new Focusable { HasFocus = true, CanReceiveFocus = false };
        var b = new Focusable { CanReceiveFocus = false };
        CCFocusManager manager = NewManager(a, b);
        int changes = 0;
        manager.OnFocusChanged += (prev, current) => changes++;

        if (next)
        {
            manager.FocusNextItem();
        }
        else
        {
            manager.FocusPreviousItem();
        }

        Assert.Null(manager.ItemWithFocus);
        Assert.False(a.HasFocus);
        Assert.False(b.HasFocus);
        Assert.Equal(0, changes);
    }
}
