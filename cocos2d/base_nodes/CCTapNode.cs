using System;
using Cocos2D;

namespace cocos2d.base_nodes;

public class CCTapNode<T> : CCNode, IDisposable
{
    public delegate void TapHandler(T? data, CCNode node, CCPoint tapLocation);
    public event TapHandler? OnTapped;
    public delegate void TouchBeginHandler(T? data, CCNode node, CCPoint touchLocation);
    public event TouchBeginHandler? OnTouchBegin;
    private bool _active;
    protected bool _disposed { get; private set; }

    public CCTapNode(bool isSwallowTouches = true)
    {
        TouchMode = CCTouchMode.OneByOne;
        IsSwallowTouches = isSwallowTouches;

        Active = true;
        Init();
    }

    public bool IsSwallowTouches { get; set; }

    /// <summary>
    /// Data passed to the tap handlers. Defaults to <c>default(T)</c> until set.
    /// </summary>
    public virtual T? Data { get; set; }

    public bool Active
    {
        get
        {
            return TouchEnabled;
        }
        set
        {
            TouchEnabled = value;
        }
    }

    public override bool TouchBegan(CCTouch touch)
    {
        try
        {
            if (WorldBoundingBox.ContainsPoint(touch.Location) && Visible)
            {
                TouchBegan(touch.Location);
                return IsSwallowTouches;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Touch began failed with exception " + ex.Message);
        }
        return false;
    }

    public override void TouchEnded(CCTouch touch)
    {
        try
        {
            if (WorldBoundingBox.ContainsPoint(touch.Location) && Visible)
            {
                Tapped(touch.Location);
            }
            else
            {
                ReleasedOutside();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Touch ended failed with exception " + ex.Message);
            ReleasedOutside();
        }
    }

    public override void TouchMoved(CCTouch touch)
    {
        try
        {
            if (WorldBoundingBox.ContainsPoint(touch.Location) && Visible)
            {
                DragInside(touch.Location);
            }
            else
            {
                DragOutside(touch.Location);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Touch moved failed with exception " + ex.Message);
            DragOutside(touch.Location);
        }
    }

    /// <summary>
    /// Adjust content size to its children;
    /// </summary>
    protected void RefreshContentSize()
    {
        CCSize newContentSize = new CCSize();
        // Children is null until the first child is added; no children means zero size.
        if (Children != null)
        {
            foreach (var child in Children)
            {
                newContentSize.Height = Math.Max(newContentSize.Height, child.ContentSize.Height);
                newContentSize.Width = Math.Max(newContentSize.Width, child.ContentSize.Width);
            }
        }
        ContentSize = newContentSize;
    }

    protected virtual void Tapped(CCPoint tapLocation)
    {
        OnTapped?.Invoke(Data, this, tapLocation);
    }

    protected virtual void ReleasedOutside()
    {

    }

    protected virtual void TouchBegan(CCPoint touchLocation)
    {
        OnTouchBegin?.Invoke(Data, this, touchLocation);
    }

    protected virtual void DragInside(CCPoint position)
    {

    }

    protected virtual void DragOutside(CCPoint position)
    {

    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            Active = false;
        }
        base.Dispose(disposing);
    }
}
