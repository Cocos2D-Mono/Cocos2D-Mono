using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cocos2D;

public delegate void CCFocusChangeDelegate(ICCFocusable? prev, ICCFocusable current);

public class CCFocusManager
{
    public event CCFocusChangeDelegate? OnFocusChanged;

    /// <summary>
    /// The list of nodes that can receive focus
    /// </summary>
    private LinkedList<ICCFocusable> _FocusList = new LinkedList<ICCFocusable>();
    /// <summary>
    /// The current node with focus
    /// </summary>
    private LinkedListNode<ICCFocusable>? _Current = null;

    /// <summary>
    /// Removes the given focusable node
    /// </summary>
    /// <param name="f"></param>
    public void Remove(ICCFocusable f)
    {
        _FocusList.Remove(f);
    }

    /// <summary>
    /// Adds the given node to the list of focus nodes. If the node has the focus, then it is
    /// given the current focused item status. If there is already a focused item and the
    /// given node has focus, the focus is disabled.
    /// </summary>
    /// <param name="f"></param>
    public void Add(ICCFocusable f)
    {
        LinkedListNode<ICCFocusable> i = _FocusList.AddLast(f);
        if (f.HasFocus)
        {
            if (_Current == null)
            {
                _Current = i;
            }
            else
            {
                f.HasFocus = false;
            }
        }
    }

    /// <summary>
    /// When true, the d-pad on each game pad moves the focus: left and up to the previous item,
    /// and right and down to the next. A press moves it once, and holding the button moves it
    /// again every <see cref="MenuScrollDelay"/> milliseconds. False by default.
    /// </summary>
    public bool Enabled
    {
        get
        {
            return _enabled;
        }
        set
        {
            if (!value)
            {
                // A button that's down when the manager is enabled again counts as a new press,
                // even if its pad sends no update in between. Setting true doesn't clear the
                // holds, because a game may set it on every frame.
                Array.Clear(_dPadHolds);
            }
            _enabled = value;
        }
    }

    private bool _enabled;

    /// <summary>
    /// Moves the focus to the next item that can receive focus, going round to the first item
    /// after the last. With no item focused, the first item that can receive focus gets it. When
    /// no other item can receive focus, the focus stays where it is, unless the focused item was
    /// removed: then it loses the focus, and no item has it.
    /// </summary>
    public void FocusNextItem()
    {
        MoveFocus(forward: true);
    }

    /// <summary>
    /// Moves the focus to the previous item that can receive focus, going round to the last item
    /// before the first. With no item focused, the last item that can receive focus gets it. When
    /// no other item can receive focus, the focus stays where it is, unless the focused item was
    /// removed: then it loses the focus, and no item has it.
    /// </summary>
    public void FocusPreviousItem()
    {
        MoveFocus(forward: false);
    }

    private void MoveFocus(bool forward)
    {
        // A focused item that was removed is no longer in the list, so the search starts from the
        // end of the list instead, as it does when no item has the focus.
        LinkedListNode<ICCFocusable>? focused = _Current != null && _Current.List == _FocusList ? _Current : null;
        LinkedListNode<ICCFocusable>? item = FindItemToFocus(focused, forward);
        if (item == null && focused != null)
        {
            // No item can receive focus, the focused one included. It keeps the focus anyway,
            // because OnFocusChanged has no way to say that no item has it.
            return;
        }
        ICCFocusable? lostItem = _Current?.Value;
        _Current = item;
        if (lostItem != null)
        {
            lostItem.HasFocus = false;
        }
        if (item != null)
        {
            item.Value.HasFocus = true;
            OnFocusChanged?.Invoke(lostItem, item.Value);
        }
    }

    /// <summary>
    /// Returns the nearest item after the focused one, or before it, that can receive focus. The
    /// search goes round to the other end of the list, and the focused item itself comes last, so
    /// it keeps the focus when no other item can take it. Returns null when no item can receive
    /// focus.
    /// </summary>
    private LinkedListNode<ICCFocusable>? FindItemToFocus(LinkedListNode<ICCFocusable>? focused, bool forward)
    {
        LinkedListNode<ICCFocusable>? node = focused;
        for (int i = 0; i < _FocusList.Count; i++)
        {
            node = forward ? (node?.Next ?? _FocusList.First) : (node?.Previous ?? _FocusList.Last);
            if (node != null && node.Value.CanReceiveFocus)
            {
                return node;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the item with the current focus. This test will create a copy 
    /// of the master item list.
    /// </summary>
    public ICCFocusable? ItemWithFocus
    {
        get
        {
            return (_Current != null ? _Current.Value : null);
        }
    }


    /// <summary>
    /// While a d-pad button is held, the time in milliseconds from one move of the focus to the
    /// next. The press itself moves the focus at once. 250 by default.
    /// </summary>
    public static float MenuScrollDelay = 250f;

    // The d-pad hold of each player's game pad, indexed by PlayerIndex.
    private readonly DPadHold[] _dPadHolds = new DPadHold[4];

    private struct DPadHold
    {
        // The direction the d-pad was held in at the pad's last update: -1 for the previous
        // item, 1 for the next, and 0 for neither.
        public int Direction;
        // The game time when this hold last moved the focus.
        public TimeSpan TimeOfLastMove;
    }

    #region Singleton

    private CCFocusManager()
    {
        // GamePad focus management is only available when using CCApplication
        // CCGameView handles input differently
        if (CCApplication.SharedApplication != null)
        {
            CCApplication.SharedApplication.GamePadDPadUpdate += new CCGamePadDPadDelegate(SharedApplication_GamePadDPadUpdate);
        }
    }

    private void SharedApplication_GamePadDPadUpdate(CCGamePadButtonStatus leftButton, CCGamePadButtonStatus upButton, CCGamePadButtonStatus rightButton, CCGamePadButtonStatus downButton, Microsoft.Xna.Framework.PlayerIndex player)
    {
        // CCApplication raises this from its Update, after it keeps that frame's game time. The
        // game's time is steadier than the wall clock, which can be set back.
        TimeSpan time = CCApplication.SharedApplication?.GameTime?.TotalGameTime ?? TimeSpan.Zero;
        UpdateDPad(leftButton, upButton, rightButton, downButton, player, time);
    }

    /// <summary>
    /// Moves the focus for one update of a player's d-pad, at the given game time. CCApplication
    /// reports every button that the pad has on each update, as pressed or released.
    /// </summary>
    private void UpdateDPad(CCGamePadButtonStatus leftButton, CCGamePadButtonStatus upButton, CCGamePadButtonStatus rightButton, CCGamePadButtonStatus downButton, Microsoft.Xna.Framework.PlayerIndex player, TimeSpan time)
    {
        if (!Enabled)
        {
            return;
        }
        ref DPadHold hold = ref _dPadHolds[(int)player];
        bool previous = leftButton == CCGamePadButtonStatus.Pressed || upButton == CCGamePadButtonStatus.Pressed;
        bool next = rightButton == CCGamePadButtonStatus.Pressed || downButton == CCGamePadButtonStatus.Pressed;
        // Buttons for both directions at once, such as up and right on a diagonal, don't say
        // which way to go, so the focus doesn't move.
        int direction = previous == next ? 0 : (next ? 1 : -1);
        if (direction == 0)
        {
            hold.Direction = 0;
            return;
        }
        // A new press moves the focus at once. A button held since the last update moves it again
        // once MenuScrollDelay has passed since its last move.
        if (direction == hold.Direction && (time - hold.TimeOfLastMove).TotalMilliseconds < MenuScrollDelay)
        {
            return;
        }
        hold.Direction = direction;
        hold.TimeOfLastMove = time;
        if (direction > 0)
        {
            FocusNextItem();
        }
        else
        {
            FocusPreviousItem();
        }
    }

    private static CCFocusManager _Instance = new CCFocusManager();

    public static CCFocusManager Instance
    {
        get
        {
            return (_Instance);
        }
    }
    #endregion
}
