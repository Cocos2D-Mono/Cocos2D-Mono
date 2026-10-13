#if IOS || __IOS__
using System;
using System.ComponentModel;
using System.Threading;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIKit;

namespace Cocos2D;

/// <summary>
/// iOS-specific time source for the game view.
/// </summary>
class GameViewTimeSource
{
    TimeSpan _timeout;
    NSTimer _timer;
    CCGameView _view;

    public GameViewTimeSource(CCGameView view, double updatesPerSecond)
    {
        _view = view;

        // Can't use TimeSpan.FromSeconds() as that only has 1ms
        // resolution, and we need better (e.g. 60fps doesn't fit nicely
        // in 1ms resolution, but does in ticks).
        _timeout = new TimeSpan((long)(((1.0 * TimeSpan.TicksPerSecond) / updatesPerSecond) + 0.5));
    }

    public void Suspend()
    {
        if (_timer != null)
        {
            _timer.Invalidate();
            _timer = null;
        }
    }

    public void Resume()
    {
        if (_timeout != new TimeSpan(-1))
        {
            _timer = NSTimer.CreateRepeatingTimer(_timeout, _view.RunIteration);
            NSRunLoop.Main.AddTimer(_timer, NSRunLoopMode.Common);
        }
    }

    public void Invalidate()
    {
        if (_timer != null)
        {
            _timer.Invalidate();
            _timer = null;
        }
    }
}

/// <summary>
/// iOS-specific partial implementation of CCGameView.
/// Uses UIView with CADisplayLink for rendering with MonoGame graphics integration.
/// </summary>
[Register("CCGameView"), DesignTimeVisible(true)]
public partial class CCGameView : UIView
{
    bool _initialized;
    GameViewTimeSource _timeSource;

    NSObject _backgroundObserver;
    NSObject _foregroundObserver;

    #region Constructors

    /// <summary>
    /// Creates a new CCGameView from a coder (for Interface Builder).
    /// </summary>
    [Export("initWithCoder:")]
    public CCGameView(NSCoder coder)
        : base(coder)
    {
        BeginInitialise();
    }

    /// <summary>
    /// Creates a new CCGameView with the specified frame.
    /// </summary>
    public CCGameView(CGRect frame)
        : base(frame)
    {
        BeginInitialise();
    }

    void BeginInitialise()
    {
        ContentScaleFactor = UIScreen.MainScreen.Scale;
#if !TVOS
        // No touch screen on tvOS; UIView has no MultipleTouchEnabled there.
        MultipleTouchEnabled = true;
#endif
        UserInteractionEnabled = true;
    }

    #endregion Constructors

    #region Initialisation

    partial void PlatformInitialise()
    {
        _backgroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.DidEnterBackgroundNotification, (n) => Paused = true);
        _foregroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillEnterForegroundNotification, (n) => Paused = false);
    }

    partial void PlatformInitialiseGraphicsDevice(ref PresentationParameters presParams)
    {
        // iOS-specific graphics device initialization
    }

    partial void PlatformStartGame()
    {
        if (_timeSource != null)
            _timeSource.Invalidate();

        _timeSource = new GameViewTimeSource(this, 60.0f);

        if (!_initialized)
        {
            Initialise();
            _initialized = true;
            _platformInitialised = true;
            LoadGame();
        }

        _timeSource.Resume();
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        var newSize = new CCSize(
            (float)(Bounds.Size.Width * ContentScaleFactor),
            (float)(Bounds.Size.Height * ContentScaleFactor));

        if (newSize.Width == 0 || newSize.Height == 0)
            return;

        ViewSize = newSize;
        _viewportDirty = true;
    }

    partial void InitialiseInputHandling()
    {
        InitialiseMobileInputHandling();
    }

    #endregion Initialisation

    #region Cleaning up

    partial void PlatformDispose(bool disposing)
    {
        if (disposing)
        {
            if (_timeSource != null)
            {
                _timeSource.Invalidate();
                _timeSource = null;
            }
        }

        if (_backgroundObserver != null)
            NSNotificationCenter.DefaultCenter.RemoveObserver(_backgroundObserver);

        if (_foregroundObserver != null)
            NSNotificationCenter.DefaultCenter.RemoveObserver(_foregroundObserver);
    }

    partial void PlatformCanDisposeGraphicsDevice(ref bool canDispose)
    {
        canDispose = true;
    }

    #endregion Cleaning up

    #region Run loop

    partial void PlatformUpdatePaused()
    {
        if (_timeSource != null)
        {
            if (Paused)
                _timeSource.Suspend();
            else
                _timeSource.Resume();
        }

        MobilePlatformUpdatePaused();
    }

    /// <summary>
    /// Internal method called by the timer to run one iteration of the game loop.
    /// </summary>
    internal void RunIteration(NSTimer timer)
    {
        if (Paused)
            return;

        Tick();
        Draw();
        PlatformPresentInternal();
    }

    partial void ProcessInput()
    {
        ProcessMobileInput();
    }

    #endregion Run loop

    #region Rendering

    partial void PlatformPresent()
    {
        PlatformPresentInternal();
    }

    void PlatformPresentInternal()
    {
        if (Paused)
            return;

        try
        {
            if (_graphicsDevice != null)
                _graphicsDevice.Present();
        }
        catch (Exception ex)
        {
            CCLog.Log("CCGameView: Error in present. Error: {0}", ex.Message);
        }
    }

    #endregion Rendering

    #region Touch handling

    partial void PlatformUpdateTouchEnabled()
    {
        UserInteractionEnabled = TouchEnabled;
    }

    /// <summary>
    /// Called when touches begin.
    /// </summary>
    public override void TouchesBegan(NSSet touches, UIEvent evt)
    {
        base.TouchesBegan(touches, evt);
        FillTouchCollection(touches);
    }

    /// <summary>
    /// Called when touches end.
    /// </summary>
    public override void TouchesEnded(NSSet touches, UIEvent evt)
    {
        base.TouchesEnded(touches, evt);
        FillTouchCollection(touches);
    }

    /// <summary>
    /// Called when touches move.
    /// </summary>
    public override void TouchesMoved(NSSet touches, UIEvent evt)
    {
        base.TouchesMoved(touches, evt);
        FillTouchCollection(touches);
    }

    /// <summary>
    /// Called when touches are cancelled.
    /// </summary>
    public override void TouchesCancelled(NSSet touches, UIEvent evt)
    {
        base.TouchesCancelled(touches, evt);
        FillTouchCollection(touches);
    }

    void FillTouchCollection(NSSet touches)
    {
        if (touches.Count == 0)
            return;

        foreach (UITouch touch in touches)
        {
            var location = touch.LocationInView(touch.View);
            var position = new CCPoint((float)(location.X * ContentScaleFactor), (float)(location.Y * ContentScaleFactor));

            var id = touch.Handle.GetHashCode();

            switch (touch.Phase)
            {
                case UITouchPhase.Moved:
                    UpdateIncomingMoveTouch(id, ref position);
                    break;
                case UITouchPhase.Began:
                    AddIncomingNewTouch(id, ref position);
                    break;
                case UITouchPhase.Ended:
                case UITouchPhase.Cancelled:
                    UpdateIncomingReleaseTouch(id);
                    break;
                default:
                    break;
            }
        }
    }

    #endregion Touch handling
}
#endif
