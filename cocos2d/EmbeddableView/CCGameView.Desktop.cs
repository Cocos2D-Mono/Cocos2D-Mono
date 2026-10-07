#nullable disable
#if DESKTOPGL || WINDOWS || WINDOWSGL || MACOS || LINUX
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Cocos2D;

/// <summary>
/// Desktop-specific partial implementation of CCGameView.
/// Provides a game view that can be embedded in desktop applications.
/// Note: On desktop platforms, this typically wraps an existing Game instance.
/// </summary>
public partial class CCGameView
{
    Game _game;
    GraphicsDeviceManager _graphicsDeviceManager;
    bool _mouseEnabled = true;
    int _lastMouseId;
    MouseState _lastMouseState;
    MouseState _prevMouseState;

    #region Desktop Constructors

    /// <summary>
    /// Creates a new CCGameView from an existing MonoGame Game instance.
    /// </summary>
    /// <param name="game">The MonoGame Game instance.</param>
    /// <param name="graphics">The graphics device manager.</param>
    public CCGameView(Game game, GraphicsDeviceManager graphics)
    {
        _game = game;
        _graphicsDeviceManager = graphics;
        _graphicsDevice = game.GraphicsDevice;
        ViewSize = new CCSize(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
    }

    #endregion Desktop Constructors

    #region Properties

    /// <summary>
    /// Gets or sets whether mouse input is enabled.
    /// </summary>
    public bool MouseEnabled
    {
        get { return _mouseEnabled; }
        set { _mouseEnabled = value; }
    }

    /// <summary>
    /// Gets the MonoGame Game instance.
    /// </summary>
    public Game Game { get { return _game; } }

    #endregion Properties

    /// <summary>
    /// Updates the view size. Call this when the window is resized.
    /// </summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    public void UpdateViewSize(int width, int height)
    {
        ViewSize = new CCSize(width, height);
        _viewportDirty = true;
    }

    #region Initialisation

    partial void PlatformInitialise()
    {
        // Desktop-specific initialization
        _platformInitialised = true;
    }

    partial void PlatformInitialiseGraphicsDevice(ref PresentationParameters presParams)
    {
        // For desktop, we use the existing graphics device from the Game
        presParams.BackBufferWidth = (int)ViewSize.Width;
        presParams.BackBufferHeight = (int)ViewSize.Height;
    }

    partial void PlatformStartGame()
    {
        // Initialize the view if not already done
        if (!_viewInitialised)
        {
            Initialise();
            LoadGame();
        }
    }

    partial void InitialiseInputHandling()
    {
        // Initialize desktop input handling (keyboard, mouse)
    }

    #endregion Initialisation

    #region Run loop

    partial void PlatformUpdatePaused()
    {
        // Desktop-specific pause handling if needed
    }

    /// <summary>
    /// Call this from your MonoGame Game.Update() method to update the game state.
    /// </summary>
    /// <param name="gameTime">The game time from MonoGame.</param>
    public void UpdateView(GameTime gameTime)
    {
        if (!_gameStarted || Paused)
            return;

        _gameTime = gameTime;

        // Handle view-owned scene transitions
        if (_hasOwnScene && _nextViewScene != null)
        {
            SetNextViewScene();
        }

        // Handle split-screen scene transitions
        if (_splitScreenEnabled && _nextSplitScreenScene != null)
        {
            SetNextSplitScreenScene();
        }

        if (_hasOwnScene)
        {
            // Update scheduler for view-owned scene
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (RunningScene != null)
            {
                Scheduler.update(deltaTime);
            }
        }
        else
        {
            // Use Director.Update which handles scene transitions (SetNextScene)
            // and scheduler updates
            Director.Update(gameTime);
        }

        ProcessInput();

        // Update any secondary views attached to this primary view
        UpdateSecondaryViews(gameTime);
    }

    /// <summary>
    /// Call this from your MonoGame Game.Draw() method to render the game.
    /// </summary>
    /// <param name="gameTime">The game time from MonoGame.</param>
    public void DrawView(GameTime gameTime)
    {
        if (!_gameStarted || Paused)
            return;

        _gameTime = gameTime;

        // Restore this view's state before drawing (for multi-view support)
        if (_drawManagerState != null)
        {
            CCDrawManager.RestoreState(_drawManagerState);
        }

        // Ensure viewport is updated before drawing
        if (_viewportDirty)
        {
            UpdateViewport();
            // Save state after viewport update since it changes CCDrawManager state
            _drawManagerState = CCDrawManager.SaveState();
        }

        if (_splitScreenEnabled && _splitScreenScene != null)
        {
            // Split-screen mode: render two scenes side by side
            DrawSplitScreen();
        }
        else
        {
            // Normal single-scene rendering
            if (CCDrawManager.BeginDraw())
            {
                CCScene runningScene = RunningScene;

                if (runningScene != null)
                {
                    if (_hasOwnScene)
                    {
                        // Draw view-owned scene directly
                        runningScene.Visit();
                    }
                    else
                    {
                        // Use shared director's main loop
                        Director.MainLoop(gameTime);
                    }
                }

                CCDrawManager.EndDraw();

                // Save state after drawing for next frame
                _drawManagerState = CCDrawManager.SaveState();
            }
        }

        // Draw any secondary views attached to this primary view
        DrawSecondaryViews(gameTime);
    }

    partial void ProcessInput()
    {
        ProcessDesktopInput();
    }

    void ProcessDesktopInput()
    {
        if (_mouseEnabled)
        {
            ProcessMouse();
        }
    }

    void ProcessMouse()
    {
        _prevMouseState = _lastMouseState;
        _lastMouseState = Mouse.GetState();

        var touchDispatcher = Director.TouchDispatcher;
        if (touchDispatcher == null || !touchDispatcher.IsDispatchEvents)
            return;

        var newTouches = new System.Collections.Generic.List<CCTouch>();
        var movedTouches = new System.Collections.Generic.List<CCTouch>();
        var endedTouches = new System.Collections.Generic.List<CCTouch>();

        if (_prevMouseState.LeftButton == ButtonState.Released && _lastMouseState.LeftButton == ButtonState.Pressed)
        {
            var pos = CCDrawManager.ScreenToWorld(_lastMouseState.X, _lastMouseState.Y);
            _lastMouseId++;
            var touch = new CCTouch(_lastMouseId, pos.X, pos.Y);
            newTouches.Add(touch);
        }
        else if (_prevMouseState.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Pressed)
        {
            if (_prevMouseState.X != _lastMouseState.X || _prevMouseState.Y != _lastMouseState.Y)
            {
                var pos = CCDrawManager.ScreenToWorld(_lastMouseState.X, _lastMouseState.Y);
                var touch = new CCTouch(_lastMouseId, pos.X, pos.Y);
                movedTouches.Add(touch);
            }
        }
        else if (_prevMouseState.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released)
        {
            var pos = CCDrawManager.ScreenToWorld(_lastMouseState.X, _lastMouseState.Y);
            var touch = new CCTouch(_lastMouseId, pos.X, pos.Y);
            endedTouches.Add(touch);
        }

        if (newTouches.Count > 0)
        {
            touchDispatcher.TouchesBegan(newTouches);
        }

        if (movedTouches.Count > 0)
        {
            touchDispatcher.TouchesMoved(movedTouches);
        }

        if (endedTouches.Count > 0)
        {
            touchDispatcher.TouchesEnded(endedTouches);
        }
    }

    #endregion Run loop

    #region Rendering

    partial void PlatformPresent()
    {
        // Desktop presentation is handled by the MonoGame game loop
    }

    #endregion Rendering

    #region Cleanup

    partial void PlatformDispose(bool disposing)
    {
        // Desktop-specific cleanup
    }

    partial void PlatformCanDisposeGraphicsDevice(ref bool canDispose)
    {
        // On desktop, the graphics device is typically managed by the Game
        canDispose = false;
    }

    partial void PlatformGetServiceProvider(ref IServiceProvider serviceProvider)
    {
        // On desktop, use the Game's service provider if available
        if (_game != null && _game.Services != null)
        {
            serviceProvider = _game.Services;
        }
    }

    #endregion Cleanup
}
#endif
