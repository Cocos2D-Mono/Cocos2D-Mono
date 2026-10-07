#nullable disable
using System;
using System.Collections.Generic;

namespace Cocos2D;

/// <summary>
/// Mobile-specific partial implementation of CCGameView.
/// Contains shared touch handling code for iOS and Android.
/// </summary>
public partial class CCGameView
{
    static readonly TimeSpan TouchTimeLimit = TimeSpan.FromMilliseconds(1000);

    bool _touchEnabled;

    Dictionary<int, CCTouch> _touchMap;
    List<CCTouch> _incomingNewTouches;
    List<CCTouch> _incomingMoveTouches;
    List<CCTouch> _incomingReleaseTouches;

    object _touchLock = new object();

    #region Properties

    /// <summary>
    /// Gets or sets whether touch input is enabled.
    /// </summary>
    public bool TouchEnabled
    {
        get { return _touchEnabled; }
        set
        {
            _touchEnabled = value;
            PlatformUpdateTouchEnabled();
        }
    }

    /// <summary>
    /// Gets the accelerometer for this view.
    /// </summary>
    public CCAccelerometer Accelerometer { get { return CCDirector.SharedDirector.Accelerometer; } }

    #endregion Properties

    #region Initialisation

    void InitialiseMobileInputHandling()
    {
        _touchMap = new Dictionary<int, CCTouch>();
        _incomingNewTouches = new List<CCTouch>();
        _incomingMoveTouches = new List<CCTouch>();
        _incomingReleaseTouches = new List<CCTouch>();

        TouchEnabled = true;
    }

    #endregion Initialisation

    #region Pause handling

    void MobilePlatformUpdatePaused()
    {
        // Handle accelerometer state when pausing/resuming
        // The CCAccelerometer in cocos2d-mono handles this differently
    }

    #endregion Pause handling

    #region Touch handling

    /// <summary>
    /// Adds a new touch to the incoming touch queue.
    /// </summary>
    protected void AddIncomingNewTouch(int touchId, ref CCPoint position)
    {
        lock (_touchLock)
        {
            if (!_touchMap.ContainsKey(touchId))
            {
                var touch = new CCTouch(touchId, position.X, position.Y);
                _touchMap.Add(touchId, touch);
                _incomingNewTouches.Add(touch);
            }
        }
    }

    /// <summary>
    /// Updates an existing touch with move information.
    /// </summary>
    protected void UpdateIncomingMoveTouch(int touchId, ref CCPoint position)
    {
        lock (_touchLock)
        {
            CCTouch existingTouch;
            if (_touchMap.TryGetValue(touchId, out existingTouch))
            {
                var delta = existingTouch.LocationInView - position;
                if (delta.LengthSquared > 1.0f)
                {
                    _incomingMoveTouches.Add(existingTouch);
                    existingTouch.SetTouchInfo(touchId, position.X, position.Y);
                }
            }
        }
    }

    /// <summary>
    /// Updates an existing touch to indicate it has been released.
    /// </summary>
    protected void UpdateIncomingReleaseTouch(int touchId)
    {
        lock (_touchLock)
        {
            CCTouch existingTouch;
            if (_touchMap.TryGetValue(touchId, out existingTouch))
            {
                _incomingReleaseTouches.Add(existingTouch);
                _touchMap.Remove(touchId);
            }
        }
    }

    void ProcessMobileInput()
    {
        lock (_touchLock)
        {
            var touchDispatcher = Director.TouchDispatcher;
            if (touchDispatcher != null && touchDispatcher.IsDispatchEvents)
            {
                RemoveOldTouches();

                if (_incomingNewTouches.Count > 0)
                {
                    touchDispatcher.TouchesBegan(_incomingNewTouches);
                }

                if (_incomingMoveTouches.Count > 0)
                {
                    touchDispatcher.TouchesMoved(_incomingMoveTouches);
                }

                if (_incomingReleaseTouches.Count > 0)
                {
                    touchDispatcher.TouchesEnded(_incomingReleaseTouches);
                }

                _incomingNewTouches.Clear();
                _incomingMoveTouches.Clear();
                _incomingReleaseTouches.Clear();
            }
        }

        // Update accelerometer if available and enabled
        if (Accelerometer != null)
        {
            Accelerometer.Update();
        }
    }

    // Prevent memory leaks by removing stale touches
    // In particular, in the case of the game entering the background
    // a release touch event may not have been triggered within the view
    void RemoveOldTouches()
    {
        // cocos2d-mono's CCTouch doesn't track timestamps, so we can't implement
        // time-based stale touch detection like CocosSharp does.
        // Stale touches are handled by the pause/resume mechanism instead -
        // when the app is paused, touches should be cleared or handled
        // by the platform-specific implementations.
    }

    /// <summary>
    /// Clears all tracked touches. Call this when the view is paused or loses focus.
    /// </summary>
    protected void ClearTouches()
    {
        lock (_touchLock)
        {
            _touchMap.Clear();
            _incomingNewTouches.Clear();
            _incomingMoveTouches.Clear();
            _incomingReleaseTouches.Clear();
        }
    }

    /// <summary>
    /// Platform-specific touch enabled state update.
    /// </summary>
    partial void PlatformUpdateTouchEnabled();

    #endregion Touch handling
}
