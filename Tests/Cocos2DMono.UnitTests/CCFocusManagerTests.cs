using System;
using System.Reflection;
using Cocos2D;
using Microsoft.Xna.Framework;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the runtime classes (C2D-303), for the item that
// FocusNextItem and FocusPreviousItem move the focus to (C2D-330), and for the d-pad moving the
// focus (C2D-388).
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
    public void Moving_WhenNoItemCanReceiveFocus_LeavesTheFocusWhereItIs(bool next)
    {
        // The focused item has stopped being able to receive focus, and so have the others. The
        // focus went to the first item or the previous one, which couldn't receive it either.
        // It stays put instead, because OnFocusChanged can't report that no item has it.
        var a = new Focusable { CanReceiveFocus = false };
        var b = new Focusable { HasFocus = true, CanReceiveFocus = false };
        var c = new Focusable { CanReceiveFocus = false };
        CCFocusManager manager = NewManager(a, b, c);
        int changes = 0;
        manager.OnFocusChanged += (prev, current) => changes++;

        Move(manager, next);

        Assert.Same(b, manager.ItemWithFocus);
        Assert.True(b.HasFocus);
        Assert.False(a.HasFocus);
        Assert.False(c.HasFocus);
        Assert.Equal(0, changes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Moving_FromARemovedItem_WhenNoItemCanReceiveFocus_LeavesNoFocus(bool next)
    {
        // The removed item loses the focus, as when it was the only item. The focus went to the
        // first or last item, which couldn't receive it.
        var a = new Focusable { CanReceiveFocus = false };
        var b = new Focusable { HasFocus = true };
        CCFocusManager manager = NewManager(a, b);
        manager.Remove(b);

        Move(manager, next);

        Assert.Null(manager.ItemWithFocus);
        Assert.False(a.HasFocus);
        Assert.False(b.HasFocus);
    }

    private static void Move(CCFocusManager manager, bool next)
    {
        if (next)
        {
            manager.FocusNextItem();
        }
        else
        {
            manager.FocusPreviousItem();
        }
    }

    [Fact]
    public void TheDPadEvent_MovesTheFocus()
    {
        // CCApplication reports every d-pad button that a standard pad has, so some are always
        // released. The handler took any released button for the end of a press, so the focus
        // never moved.
        var a = new Focusable { HasFocus = true };
        var b = new Focusable();
        CCFocusManager manager = NewEnabledManager(a, b);
        MethodInfo handler = typeof(CCFocusManager).GetMethod("SharedApplication_GamePadDPadUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!;

        handler.Invoke(manager, new object[]
        {
            CCGamePadButtonStatus.Released, CCGamePadButtonStatus.Released,
            CCGamePadButtonStatus.Pressed, CCGamePadButtonStatus.Released, PlayerIndex.One,
        });

        Assert.Same(b, manager.ItemWithFocus);
    }

    [Theory]
    [InlineData(DPadButtons.Left, 0)]
    [InlineData(DPadButtons.Up, 0)]
    [InlineData(DPadButtons.Right, 2)]
    [InlineData(DPadButtons.Down, 2)]
    public void TappingADPadButton_MovesTheFocusOnce(DPadButtons button, int expected)
    {
        Focusable[] items = Items(3, focused: 1);
        CCFocusManager manager = NewEnabledManager(items);
        int changes = 0;
        manager.OnFocusChanged += (prev, current) => changes++;
        double delay = CCFocusManager.MenuScrollDelay;

        UpdateDPad(manager, 0, button);
        UpdateDPad(manager, delay / 2, DPadButtons.None);
        UpdateDPad(manager, delay * 3, DPadButtons.None);

        Assert.Same(items[expected], manager.ItemWithFocus);
        Assert.Equal(1, changes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HoldingADPadButton_MovesTheFocusAtOnce_ThenOnceEveryMenuScrollDelay(bool onlyRight)
    {
        // A pad with only a right button got past the handler's reset. Then the focus moved only
        // after the button had been held for MenuScrollDelay, and after that on every update.
        Focusable[] items = Items(5, focused: 0);
        CCFocusManager manager = NewEnabledManager(items);
        double delay = CCFocusManager.MenuScrollDelay;

        UpdateDPad(manager, 0, DPadButtons.Right, onlyRight: onlyRight);
        Assert.Same(items[1], manager.ItemWithFocus);

        UpdateDPad(manager, delay / 2, DPadButtons.Right, onlyRight: onlyRight);
        UpdateDPad(manager, delay - 1, DPadButtons.Right, onlyRight: onlyRight);
        Assert.Same(items[1], manager.ItemWithFocus);

        UpdateDPad(manager, delay, DPadButtons.Right, onlyRight: onlyRight);
        Assert.Same(items[2], manager.ItemWithFocus);

        UpdateDPad(manager, delay * 1.5, DPadButtons.Right, onlyRight: onlyRight);
        Assert.Same(items[2], manager.ItemWithFocus);

        UpdateDPad(manager, delay * 2, DPadButtons.Right, onlyRight: onlyRight);
        Assert.Same(items[3], manager.ItemWithFocus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PressingADPadButtonAgain_MovesTheFocusAtOnce(bool onlyRight)
    {
        // A new press doesn't wait for MenuScrollDelay. On a pad with only a right button,
        // releasing it didn't end its hold, so the next press waited.
        Focusable[] items = Items(4, focused: 0);
        CCFocusManager manager = NewEnabledManager(items);
        double delay = CCFocusManager.MenuScrollDelay;

        UpdateDPad(manager, 0, DPadButtons.Right, onlyRight: onlyRight);
        UpdateDPad(manager, delay / 4, DPadButtons.None, onlyRight: onlyRight);
        UpdateDPad(manager, delay / 2, DPadButtons.Right, onlyRight: onlyRight);

        Assert.Same(items[2], manager.ItemWithFocus);
    }

    [Fact]
    public void RollingFromRightToDown_KeepsTheSameHold()
    {
        // Right and down both move the focus to the next item, so going from one to the other
        // isn't a new press.
        Focusable[] items = Items(4, focused: 0);
        CCFocusManager manager = NewEnabledManager(items);
        double delay = CCFocusManager.MenuScrollDelay;

        UpdateDPad(manager, 0, DPadButtons.Right);
        UpdateDPad(manager, delay / 4, DPadButtons.Right | DPadButtons.Down);
        UpdateDPad(manager, delay / 2, DPadButtons.Down);
        Assert.Same(items[1], manager.ItemWithFocus);

        UpdateDPad(manager, delay, DPadButtons.Down);
        Assert.Same(items[2], manager.ItemWithFocus);
    }

    [Fact]
    public void DPadButtonsForBothDirections_DoNotMoveTheFocus()
    {
        // A diagonal such as up and right doesn't say which way to go.
        Focusable[] items = Items(3, focused: 1);
        CCFocusManager manager = NewEnabledManager(items);
        double delay = CCFocusManager.MenuScrollDelay;

        UpdateDPad(manager, 0, DPadButtons.Up | DPadButtons.Right);
        UpdateDPad(manager, delay * 2, DPadButtons.Up | DPadButtons.Right);
        Assert.Same(items[1], manager.ItemWithFocus);

        // Letting go of up leaves a press of right.
        UpdateDPad(manager, delay * 2.5, DPadButtons.Right);
        Assert.Same(items[2], manager.ItemWithFocus);
    }

    [Fact]
    public void EachPlayersDPad_HasAHoldOfItsOwn()
    {
        // CCApplication reports every connected pad on each update. Player two's pad, with nothing
        // pressed, mustn't end player one's hold, and a press on it moves the focus at once.
        Focusable[] items = Items(4, focused: 0);
        CCFocusManager manager = NewEnabledManager(items);
        double delay = CCFocusManager.MenuScrollDelay;

        for (int update = 0; update < 4; update++)
        {
            double time = delay * update / 4;
            UpdateDPad(manager, time, DPadButtons.Right, PlayerIndex.One);
            UpdateDPad(manager, time, DPadButtons.None, PlayerIndex.Two);
        }
        Assert.Same(items[1], manager.ItemWithFocus);

        UpdateDPad(manager, delay * 0.9, DPadButtons.Right, PlayerIndex.Two);
        Assert.Same(items[2], manager.ItemWithFocus);
    }

    [Fact]
    public void WhenNotEnabled_TheDPadDoesNotMoveTheFocus()
    {
        Focusable[] items = Items(3, focused: 0);
        CCFocusManager manager = NewManager(items);

        UpdateDPad(manager, 0, DPadButtons.Right);
        UpdateDPad(manager, CCFocusManager.MenuScrollDelay * 2, DPadButtons.Right);

        Assert.Same(items[0], manager.ItemWithFocus);
    }

    [Fact]
    public void ADPadButtonDown_WhenTheManagerIsEnabledAgain_IsANewPress()
    {
        // The hold doesn't carry over from before the manager was disabled.
        Focusable[] items = Items(4, focused: 0);
        CCFocusManager manager = NewEnabledManager(items);
        double delay = CCFocusManager.MenuScrollDelay;

        UpdateDPad(manager, 0, DPadButtons.Right);
        manager.Enabled = false;
        UpdateDPad(manager, delay / 4, DPadButtons.None);
        UpdateDPad(manager, delay / 2, DPadButtons.Right);
        manager.Enabled = true;
        UpdateDPad(manager, delay * 3 / 4, DPadButtons.Right);

        Assert.Same(items[2], manager.ItemWithFocus);
    }

    [Flags]
    public enum DPadButtons
    {
        None = 0,
        Left = 1,
        Up = 2,
        Right = 4,
        Down = 8,
    }

    // The d-pad tests drive the manager's private d-pad update, which CCApplication's event
    // reaches with the frame's game time, so that each test can set the time itself.
    private static readonly MethodInfo UpdateDPadMethod =
        typeof(CCFocusManager).GetMethod("UpdateDPad", BindingFlags.Instance | BindingFlags.NonPublic)!;

    // Sends one update of a player's d-pad at the given game time. On a standard pad, a button
    // that isn't pressed is Released. A pad with only a right button reports the other three as
    // NotApplicable.
    private static void UpdateDPad(CCFocusManager manager, double milliseconds, DPadButtons pressed,
        PlayerIndex player = PlayerIndex.One, bool onlyRight = false)
    {
        CCGamePadButtonStatus Status(DPadButtons button)
        {
            if ((pressed & button) != 0)
            {
                return CCGamePadButtonStatus.Pressed;
            }
            return onlyRight && button != DPadButtons.Right ? CCGamePadButtonStatus.NotApplicable : CCGamePadButtonStatus.Released;
        }

        UpdateDPadMethod.Invoke(manager, new object[]
        {
            Status(DPadButtons.Left), Status(DPadButtons.Up), Status(DPadButtons.Right), Status(DPadButtons.Down),
            player, TimeSpan.FromMilliseconds(milliseconds),
        });
    }

    private static CCFocusManager NewEnabledManager(params Focusable[] items)
    {
        CCFocusManager manager = NewManager(items);
        manager.Enabled = true;
        return manager;
    }

    private static Focusable[] Items(int count, int focused)
    {
        var items = new Focusable[count];
        for (int i = 0; i < count; i++)
        {
            items[i] = new Focusable { HasFocus = i == focused };
        }
        return items;
    }
}
