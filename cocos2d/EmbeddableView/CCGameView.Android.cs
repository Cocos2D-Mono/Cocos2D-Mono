#nullable disable
#if ANDROID
using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace Cocos2D;

/// <summary>
/// Android-specific partial implementation of CCGameView.
/// On Android, CCGameView creates an internal Game to handle graphics initialization,
/// and provides a View property for embedding in Activities.
/// </summary>
public partial class CCGameView : IDisposable
{
    CCInternalGame _internalGame;
    CCAndroidScreenReceiver _screenLockHandler;
    Context _context;
    View _androidView;
    bool _renderLoopRunning;
    Thread _renderThread;

    #region Internal Game class for graphics initialization

    /// <summary>
    /// Internal Game class that handles MonoGame initialization.
    /// This allows CCGameView to be used as an embeddable View.
    /// </summary>
    class CCInternalGame : Game
    {
        GraphicsDeviceManager _graphics;
        CCGameView _owner;
        bool _initialized;

        public GraphicsDeviceManager Graphics => _graphics;
        public bool IsInitialized => _initialized;

        public CCInternalGame(CCGameView owner, int width, int height)
        {
            _owner = owner;
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            Content.RootDirectory = owner._contentRootDirectory;
        }

        protected override void Initialize()
        {
            base.Initialize();
            _initialized = true;

            // Notify the owner that we're ready
            _owner._graphicsDevice = GraphicsDevice;
            _owner.OnInternalGameInitialized();
        }

        protected override void Update(GameTime gameTime)
        {
            if (_owner._gameStarted && !_owner.Paused)
            {
                _owner.UpdateInternal(gameTime);
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_owner._gameStarted && !_owner.Paused)
            {
                _owner.DrawInternal(gameTime);
            }
            base.Draw(gameTime);
        }

        public void UpdateSize(int width, int height)
        {
            if (_graphics != null)
            {
                _graphics.PreferredBackBufferWidth = width;
                _graphics.PreferredBackBufferHeight = height;
                _graphics.ApplyChanges();
            }
        }
    }

    #endregion Internal Game class

    #region Android screen lock handling inner class

    class CCAndroidScreenReceiver : BroadcastReceiver
    {
        bool _previousPausedState;
        CCGameView _gameView;

        public CCAndroidScreenReceiver(CCGameView view)
        {
            _gameView = view;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Intent.ActionScreenOff)
                OnLocked();
            else if (intent.Action == Intent.ActionScreenOn)
            {
                KeyguardManager keyguard = (KeyguardManager)context.GetSystemService(Context.KeyguardService);
                if (!keyguard.InKeyguardRestrictedInputMode())
                    OnUnlocked();
            }
            else if (intent.Action == Intent.ActionUserPresent)
                OnUnlocked();
        }

        void OnLocked()
        {
            _previousPausedState = _gameView.Paused;
            _gameView.Paused = true;
        }

        void OnUnlocked()
        {
            _gameView.Paused = _previousPausedState;
        }
    }

    #endregion Android screen lock handling inner class

    #region Constructors

    /// <summary>
    /// Creates a new CCGameView for embedding in an Android Activity.
    /// The view will be available through the AndroidView property after initialization.
    /// </summary>
    /// <param name="context">The Android context (typically the Activity).</param>
    public CCGameView(Context context)
    {
        _context = context;

        // Get default display size
        var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var display = windowManager.DefaultDisplay;
        var size = new Android.Graphics.Point();
        display.GetSize(size);

        ViewSize = new CCSize(size.X, size.Y);
    }

    /// <summary>
    /// Creates a new CCGameView with specified dimensions.
    /// </summary>
    /// <param name="context">The Android context (typically the Activity).</param>
    /// <param name="width">Initial width in pixels.</param>
    /// <param name="height">Initial height in pixels.</param>
    public CCGameView(Context context, int width, int height)
    {
        _context = context;
        ViewSize = new CCSize(width, height);
    }

    /// <summary>
    /// Creates a new CCGameView with an existing MonoGame Game instance.
    /// Use this if you already have a Game and want to integrate CCGameView.
    /// </summary>
    /// <param name="game">The MonoGame Game instance.</param>
    /// <param name="graphics">The GraphicsDeviceManager from the game.</param>
    public CCGameView(Game game, GraphicsDeviceManager graphics)
    {
        _graphicsDevice = game.GraphicsDevice;
        _context = Android.App.Application.Context;
        ViewSize = new CCSize(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

        // Mark as initialized since we have a working graphics device
        _platformInitialised = true;
    }

    /// <summary>
    /// Creates a secondary CCGameView that will share another view's game loop.
    /// Use AttachSecondaryView on the primary view to connect them.
    /// </summary>
    /// <param name="context">The Android context.</param>
    /// <param name="primaryView">The primary view that will drive the game loop.</param>
    public CCGameView(Context context, CCGameView primaryView)
    {
        _context = context;
        _primaryView = primaryView;

        // Share the graphics device from the primary view
        _graphicsDevice = primaryView._graphicsDevice;

        // Get default display size
        var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var display = windowManager.DefaultDisplay;
        var size = new Android.Graphics.Point();
        display.GetSize(size);

        ViewSize = new CCSize(size.X, size.Y);
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Gets the Android View that can be added to a layout.
    /// This is the MonoGame surface view.
    /// </summary>
    public View AndroidView
    {
        get
        {
            if (_androidView == null && _internalGame != null)
            {
                _androidView = _internalGame.Services.GetService(typeof(View)) as View;
            }
            return _androidView;
        }
    }

    /// <summary>
    /// Gets the internal MonoGame Game instance.
    /// </summary>
    public Game Game => _internalGame;

    #endregion Properties

    #region Initialisation

    partial void PlatformInitialise()
    {
        IntentFilter filter = new IntentFilter();
        filter.AddAction(Intent.ActionScreenOff);
        filter.AddAction(Intent.ActionScreenOn);
        filter.AddAction(Intent.ActionUserPresent);

        _screenLockHandler = new CCAndroidScreenReceiver(this);
        _context.RegisterReceiver(_screenLockHandler, filter);

        _platformInitialised = true;
    }

    partial void PlatformInitialiseGraphicsDevice(ref PresentationParameters presParams)
    {
        // Graphics device is created by the internal game
        presParams.BackBufferWidth = (int)ViewSize.Width;
        presParams.BackBufferHeight = (int)ViewSize.Height;
    }

    partial void PlatformStartGame()
    {
        if (_internalGame == null && _graphicsDevice == null)
        {
            // Create internal game to initialize graphics
            // On Android, the View is available from Game.Services after construction
            _internalGame = new CCInternalGame(this, (int)ViewSize.Width, (int)ViewSize.Height);

            // Get the view immediately - it's created during Game construction
            _androidView = _internalGame.Services.GetService(typeof(View)) as View;

            _renderLoopRunning = true;
        }
        else if (!_viewInitialised)
        {
            // Graphics device already exists (Game-based constructor)
            Initialise();
            LoadGame();
        }
    }

    /// <summary>
    /// Runs the internal game loop. Call this after setting the content view.
    /// This method blocks until the game exits.
    /// </summary>
    public void Run()
    {
        if (_internalGame != null)
        {
            _internalGame.Run();
        }
    }

    /// <summary>
    /// Initializes this view as a secondary view attached to a primary view.
    /// The secondary view will share the primary view's game loop and graphics device.
    /// Call this instead of StartGame() and Run() for secondary views.
    /// </summary>
    public void StartAsSecondaryView()
    {
        if (_primaryView == null)
        {
            throw new InvalidOperationException("StartAsSecondaryView requires this view to have a primary view set. Use the CCGameView(context, primaryView) constructor.");
        }

        // Share graphics device from primary
        _graphicsDevice = _primaryView._graphicsDevice;

        // Initialize without creating internal game
        Initialise();

        // Attach to primary view
        _primaryView.AttachSecondaryView(this);

        _gameStarted = true;

        // Fire ViewCreated event
        LoadGame();
    }

    /// <summary>
    /// Called when the internal game has finished initializing.
    /// </summary>
    void OnInternalGameInitialized()
    {
        // Get the Android view from the game
        _androidView = _internalGame.Services.GetService(typeof(View)) as View;

        // Complete initialization
        Initialise();
        LoadGame();
    }

    partial void InitialiseInputHandling()
    {
        InitialiseMobileInputHandling();
    }

    #endregion Initialisation

    #region Cleaning up

    partial void PlatformDispose(bool disposing)
    {
        _renderLoopRunning = false;

        if (_screenLockHandler != null)
        {
            try
            {
                _context.UnregisterReceiver(_screenLockHandler);
            }
            catch (Exception)
            {
                // Receiver may already be unregistered
            }
            _screenLockHandler = null;
        }

        if (_internalGame != null && disposing)
        {
            _internalGame.Exit();
            _internalGame = null;
        }
    }

    partial void PlatformCanDisposeGraphicsDevice(ref bool canDispose)
    {
        // The internal game owns the graphics device
        canDispose = false;
    }

    partial void PlatformGetServiceProvider(ref IServiceProvider serviceProvider)
    {
        if (_internalGame != null && _internalGame.Services != null)
        {
            serviceProvider = _internalGame.Services;
        }
    }

    #endregion Cleaning up

    #region Run loop

    partial void PlatformUpdatePaused()
    {
        MobilePlatformUpdatePaused();
    }

    /// <summary>
    /// Internal update method called by the internal game.
    /// </summary>
    void UpdateInternal(GameTime gameTime)
    {
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
    /// Internal draw method called by the internal game.
    /// </summary>
    void DrawInternal(GameTime gameTime)
    {
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

    /// <summary>
    /// Call this from your MonoGame Game.Update() method if using Game-based constructor.
    /// </summary>
    /// <param name="gameTime">The game time from MonoGame.</param>
    public void UpdateView(GameTime gameTime)
    {
        if (!_gameStarted || Paused)
            return;

        UpdateInternal(gameTime);
    }

    /// <summary>
    /// Call this from your MonoGame Game.Draw() method if using Game-based constructor.
    /// </summary>
    /// <param name="gameTime">The game time from MonoGame.</param>
    public void DrawView(GameTime gameTime)
    {
        if (!_gameStarted || Paused)
            return;

        DrawInternal(gameTime);
    }

    partial void ProcessInput()
    {
        ProcessMobileInput();
    }

    /// <summary>
    /// Updates the view size. Call this when the window is resized.
    /// </summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    public void UpdateViewSize(int width, int height)
    {
        ViewSize = new CCSize(width, height);
        _viewportDirty = true;

        if (_internalGame != null)
        {
            _internalGame.UpdateSize(width, height);
        }
    }

    #endregion Run loop

    #region Touch handling

    partial void PlatformUpdateTouchEnabled()
    {
        // Touch handling is done through MonoGame's TouchPanel
    }

    /// <summary>
    /// Process touch input from MonoGame's TouchPanel.
    /// Call this if you need to manually process touch events.
    /// </summary>
    public void ProcessTouchInput()
    {
        if (!TouchEnabled || Paused)
            return;

        var touchCollection = TouchPanel.GetState();
        foreach (var touch in touchCollection)
        {
            var position = new CCPoint(touch.Position.X, touch.Position.Y);
            int id = touch.Id;

            switch (touch.State)
            {
                case TouchLocationState.Pressed:
                    AddIncomingNewTouch(id, ref position);
                    break;
                case TouchLocationState.Moved:
                    UpdateIncomingMoveTouch(id, ref position);
                    break;
                case TouchLocationState.Released:
                    UpdateIncomingReleaseTouch(id);
                    break;
            }
        }
    }

    #endregion Touch handling
}
#endif
