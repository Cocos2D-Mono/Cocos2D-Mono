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
    /// When false, the focus will not traverse on the keyboard or dpad events.
    /// </summary>
    public bool Enabled
    {
        get;
        set;
    }

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


    private long _timeOfLastFocus = 0L;
    private bool _scrollingPrevious = false;
    private bool _scrollingNext = false;
    /// <summary>
    /// Scrolling focus delay used to slow down automatic focus changes when the dpad is held.
    /// </summary>
    public static float MenuScrollDelay = 50f;

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
        if (!Enabled)
        {
            return;
        }
        if (leftButton == CCGamePadButtonStatus.Released || upButton == CCGamePadButtonStatus.Released || rightButton == CCGamePadButtonStatus.Released || downButton == CCGamePadButtonStatus.Released)
        {
            _scrollingPrevious = false;
            _timeOfLastFocus = 0L;
        }
        // Left and right d-pad shuffle through the menus
        else if (leftButton == CCGamePadButtonStatus.Pressed || upButton == CCGamePadButtonStatus.Pressed)
        {
            if (_scrollingPrevious)
            {
                TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - _timeOfLastFocus);
                if (ts.TotalMilliseconds > MenuScrollDelay)
                {
                    FocusPreviousItem();
                }
            }
            else
            {
                _scrollingPrevious = true;
                _timeOfLastFocus = DateTime.Now.Ticks;
            }
        }
        else if (rightButton == CCGamePadButtonStatus.Pressed || downButton == CCGamePadButtonStatus.Pressed)
        {
            if (_scrollingNext)
            {
                TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - _timeOfLastFocus);
                if (ts.TotalMilliseconds > MenuScrollDelay)
                {
                    FocusNextItem();
                }
            }
            else
            {
                _scrollingNext = true;
                _timeOfLastFocus = DateTime.Now.Ticks;
            }
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
