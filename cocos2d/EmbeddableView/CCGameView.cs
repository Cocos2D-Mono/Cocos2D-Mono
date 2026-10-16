#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

/// <summary>
/// CCGameView is an embeddable view that hosts a cocos2d-mono game.
/// This is a partial class with platform-specific implementations.
/// </summary>
public partial class CCGameView : IDisposable
{
    // (10 mill ticks per second / 60 fps) (rounded up)
    const int NumOfTicksPerUpdate = 166667;
    const int MaxUpdateTimeMilliseconds = 500;

    static readonly CCRect ExactFitViewportRatio = new CCRect(0, 0, 1, 1);

    // Currently, we have a limitation that at most one view instance can be active at any point in time
    static WeakReference s_currentViewInstance;

    /// <summary>
    /// Internal graphics device service implementation.
    /// </summary>
    class CCGraphicsDeviceService : IGraphicsDeviceService
    {
        public GraphicsDevice GraphicsDevice { get; private set; }

        public CCGraphicsDeviceService(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        // Suppress warnings about unused events
        internal void RaiseDeviceCreated() => DeviceCreated?.Invoke(this, EventArgs.Empty);
        internal void RaiseDeviceDisposing() => DeviceDisposing?.Invoke(this, EventArgs.Empty);
        internal void RaiseDeviceReset() => DeviceReset?.Invoke(this, EventArgs.Empty);
        internal void RaiseDeviceResetting() => DeviceResetting?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Internal service provider for content loading.
    /// </summary>
    class CCServiceProvider : IServiceProvider
    {
        IGraphicsDeviceService _graphicsDeviceService;

        public CCServiceProvider(IGraphicsDeviceService graphicsDeviceService)
        {
            _graphicsDeviceService = graphicsDeviceService;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IGraphicsDeviceService))
                return _graphicsDeviceService;
            return null;
        }
    }

    internal delegate void ViewportChangedEventHandler(CCGameView sender);
    internal event ViewportChangedEventHandler ViewportChanged;

    EventHandler<EventArgs> _viewCreated;

    bool _disposed;
    volatile bool _paused;
    volatile bool _viewInitialised;
    /// <summary>
    /// Indicates whether platform-specific initialization is complete.
    /// Set by platform-specific implementations after initialization.
    /// </summary>
    protected volatile bool _platformInitialised;
    volatile bool _gameLoaded;
    volatile bool _gameStarted;
    volatile bool _viewportDirty;

    CCViewResolutionPolicy _resolutionPolicy = CCViewResolutionPolicy.ShowAll;
    CCRect _viewportRatio = ExactFitViewportRatio;
    CCSize _designResolution = new CCSize(640, 480);

    Matrix _defaultViewMatrix, _defaultProjMatrix;
    Viewport _defaultViewport;
    Viewport _viewport;

    GraphicsDevice _graphicsDevice;
    CCGraphicsDeviceService _graphicsDeviceService;
    string _contentRootDirectory = "Content";

    GameTime _gameTime;
    TimeSpan _accumulatedElapsedTime;
    TimeSpan _targetElapsedTime;
    TimeSpan _maxElapsedTime;
    Stopwatch _gameTimer;
    long _previousTicks;

    // State management for multi-view support
    CCDrawManagerState _drawManagerState;

    // Per-view scene management for multi-view support
    CCScene _viewScene;
    CCScene _nextViewScene;
    bool _hasOwnScene;

    // Multi-view support - secondary views that share this view's game loop
    List<CCGameView> _secondaryViews;
    CCGameView _primaryView;
    readonly object _secondaryViewsLock = new object();

    // Split-screen support - render two scenes side by side
    CCScene _splitScreenScene;
    CCScene _nextSplitScreenScene;
    bool _splitScreenEnabled;

    #region Properties

    /// <summary>
    /// Event raised when the view is created and ready for use.
    /// When you subscribe to this event, the game will be loaded.
    /// </summary>
    public event EventHandler<EventArgs> ViewCreated
    {
        add { _viewCreated += value; LoadGame(); }
        remove { _viewCreated -= value; }
    }

    /// <summary>
    /// Gets the director that manages scenes.
    /// </summary>
    public CCDirector Director { get { return CCDirector.SharedDirector; } }

    /// <summary>
    /// Gets the scheduler that manages scheduled callbacks.
    /// </summary>
    public CCScheduler Scheduler { get { return CCDirector.SharedDirector.Scheduler; } }

    /// <summary>
    /// Gets the action manager.
    /// </summary>
    public CCActionManager ActionManager { get { return CCDirector.SharedDirector.ActionManager; } }

    /// <summary>
    /// Gets the scene running in this view.
    /// If a view-specific scene is set, returns that; otherwise returns the shared director's scene.
    /// </summary>
    public CCScene RunningScene
    {
        get { return _hasOwnScene ? _viewScene : Director.RunningScene; }
    }

    /// <summary>
    /// Gets or sets whether split-screen mode is enabled.
    /// When enabled, the view renders two scenes side by side.
    /// </summary>
    public bool SplitScreenEnabled
    {
        get { return _splitScreenEnabled; }
        set
        {
            _splitScreenEnabled = value;
            _viewportDirty = true;
        }
    }

    /// <summary>
    /// Gets the secondary scene in split-screen mode.
    /// </summary>
    public CCScene SplitScreenScene
    {
        get { return _splitScreenScene; }
    }

    /// <summary>
    /// Gets the content manager for loading assets.
    /// </summary>
    public CCContentManager ContentManager { get { return CCContentManager.SharedContentManager; } }

    /// <summary>
    /// Gets or sets whether the view is paused.
    /// When paused, the game loop will not update or render.
    /// </summary>
    public bool Paused
    {
        get { return _paused; }
        set
        {
            if (_gameStarted && _paused != value)
            {
                _paused = value;
                _previousTicks = _gameTimer.Elapsed.Ticks;
                PlatformUpdatePaused();
            }
        }
    }

    /// <summary>
    /// Gets or sets the resolution policy that determines how the design resolution
    /// is mapped to the actual view size.
    /// </summary>
    public CCViewResolutionPolicy ResolutionPolicy
    {
        get { return _resolutionPolicy; }
        set
        {
            _resolutionPolicy = value;

            // Reset ratio if using custom resolution policy
            if (_resolutionPolicy == CCViewResolutionPolicy.Custom)
                _viewportRatio = ExactFitViewportRatio;
            _viewportDirty = true;
        }
    }

    /// <summary>
    /// Gets or sets the design resolution size.
    /// This is the logical resolution that your game is designed for.
    /// </summary>
    public CCSize DesignResolution
    {
        get { return _designResolution; }
        set
        {
            _designResolution = value;
            _viewportDirty = true;
        }
    }

    /// <summary>
    /// Gets the actual size of the view in pixels.
    /// </summary>
    public CCSize ViewSize { get; private set; }

    /// <summary>
    /// Gets or sets the custom viewport ratio.
    /// Setting this will automatically change the resolution policy to Custom.
    /// </summary>
    public CCRect ViewportRectRatio
    {
        get { return _viewportRatio; }
        set
        {
            _viewportRatio = value;
            _resolutionPolicy = CCViewResolutionPolicy.Custom;
            _viewportDirty = true;
        }
    }

    /// <summary>
    /// Gets the graphics device used for rendering.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get { return _graphicsDevice; } }

    /// <summary>
    /// Gets or sets the root directory for content loading.
    /// Must be set before calling StartGame().
    /// Default is "Content".
    /// </summary>
    public string ContentRootDirectory
    {
        get { return _contentRootDirectory; }
        set { _contentRootDirectory = value; }
    }

    internal Viewport Viewport
    {
        get
        {
            if (_viewportDirty)
                UpdateViewport();
            return _viewport;
        }
        private set
        {
            _viewport = value;
            ViewportChanged?.Invoke(this);
        }
    }

    #endregion Properties

    #region Initialisation

    /// <summary>
    /// Starts the game loop.
    /// </summary>
    public void StartGame()
    {
        if (!_gameStarted)
        {
            PlatformStartGame();
            _gameStarted = true;
        }
    }

    /// <summary>
    /// Starts the game and runs the specified scene.
    /// When useViewScene is true, the scene runs independently in this view,
    /// allowing multiple views to show different scenes.
    /// </summary>
    /// <param name="scene">The scene to run.</param>
    /// <param name="useViewScene">If true, the scene runs only in this view. Default is false for backward compatibility.</param>
    public void RunWithScene(CCScene scene, bool useViewScene = false)
    {
        StartGame();

        if (useViewScene)
        {
            // Run scene independently in this view
            _nextViewScene = scene;
            _hasOwnScene = true;
        }
        else
        {
            // Use the shared director (backward compatible)
            Director.RunWithScene(scene);
        }
    }

    /// <summary>
    /// Replaces the current scene running in this view.
    /// Only works if the view is running its own scene (useViewScene was true).
    /// </summary>
    /// <param name="scene">The new scene to run.</param>
    public void ReplaceScene(CCScene scene)
    {
        if (_hasOwnScene)
        {
            _nextViewScene = scene;
        }
        else
        {
            Director.ReplaceScene(scene);
        }
    }

    /// <summary>
    /// Sets the secondary scene for split-screen mode.
    /// The primary scene renders on the left, the split-screen scene renders on the right.
    /// </summary>
    /// <param name="scene">The scene to render in the right half of the view.</param>
    public void SetSplitScreenScene(CCScene scene)
    {
        _nextSplitScreenScene = scene;
        _splitScreenEnabled = true;
    }

    /// <summary>
    /// Replaces the split-screen scene.
    /// </summary>
    /// <param name="scene">The new scene to render in the right half.</param>
    public void ReplaceSplitScreenScene(CCScene scene)
    {
        _nextSplitScreenScene = scene;
    }

    /// <summary>
    /// Process pending scene transitions for view-owned scenes.
    /// Mirrors Director.SetNextScene behavior: when transitioning via a CCTransitionScene,
    /// the transition itself manages calling OnExit/OnEnter on the wrapped scenes.
    /// </summary>
    void SetNextViewScene()
    {
        if (_nextViewScene == null)
            return;

        bool runningIsTransition = _viewScene != null && _viewScene.IsTransition;

        // If the next scene is not a transition, exit and clean up the current scene now.
        // When the next scene IS a transition, it manages calling OnExit on the old scene itself.
        if (!_nextViewScene.IsTransition)
        {
            if (_viewScene != null)
            {
                _viewScene.OnExitTransitionDidStart();
                _viewScene.OnExit();
                _viewScene.Cleanup();
            }
        }

        _viewScene = _nextViewScene;
        _nextViewScene = null;

        // If the old scene was a transition it already called OnEnter on the new scene;
        // only call it here when we're doing a direct (non-transition) scene swap.
        if (!runningIsTransition && _viewScene != null)
        {
            _viewScene.OnEnter();
            _viewScene.OnEnterTransitionDidFinish();
        }
    }

    /// <summary>
    /// Process pending scene transitions for split-screen scenes.
    /// Mirrors the transition-safe pattern used in SetNextViewScene.
    /// </summary>
    void SetNextSplitScreenScene()
    {
        if (_nextSplitScreenScene == null)
            return;

        bool runningIsTransition = _splitScreenScene != null && _splitScreenScene.IsTransition;

        if (!_nextSplitScreenScene.IsTransition)
        {
            if (_splitScreenScene != null)
            {
                _splitScreenScene.OnExitTransitionDidStart();
                _splitScreenScene.OnExit();
                _splitScreenScene.Cleanup();
            }
        }

        _splitScreenScene = _nextSplitScreenScene;
        _nextSplitScreenScene = null;

        if (!runningIsTransition && _splitScreenScene != null)
        {
            _splitScreenScene.OnEnter();
            _splitScreenScene.OnEnterTransitionDidFinish();
        }
    }

    #region Multi-View Support

    /// <summary>
    /// Attaches a secondary view to share this view's game loop.
    /// The secondary view will be updated and drawn when this view is updated/drawn.
    /// Use this for side-by-side multi-view layouts where views share the same graphics device.
    /// </summary>
    /// <param name="secondaryView">The view to attach as a secondary view.</param>
    public void AttachSecondaryView(CCGameView secondaryView)
    {
        if (secondaryView == null || secondaryView == this)
            return;

        lock (_secondaryViewsLock)
        {
            if (_secondaryViews == null)
                _secondaryViews = new List<CCGameView>();

            if (!_secondaryViews.Contains(secondaryView))
            {
                _secondaryViews.Add(secondaryView);
                secondaryView._primaryView = this;

                // Share graphics device with secondary view
                if (secondaryView._graphicsDevice == null && _graphicsDevice != null)
                {
                    secondaryView._graphicsDevice = _graphicsDevice;
                }
            }
        }
    }

    /// <summary>
    /// Detaches a secondary view from this view's game loop.
    /// </summary>
    /// <param name="secondaryView">The view to detach.</param>
    public void DetachSecondaryView(CCGameView secondaryView)
    {
        if (secondaryView == null)
            return;

        lock (_secondaryViewsLock)
        {
            if (_secondaryViews == null)
                return;

            if (_secondaryViews.Remove(secondaryView))
            {
                secondaryView._primaryView = null;
            }
        }
    }

    /// <summary>
    /// Gets whether this view is a secondary view attached to another view's game loop.
    /// </summary>
    public bool IsSecondaryView => _primaryView != null;

    /// <summary>
    /// Updates all attached secondary views.
    /// Called by platform-specific update methods.
    /// </summary>
    protected void UpdateSecondaryViews(GameTime gameTime)
    {
        if (_secondaryViews == null)
            return;

        CCGameView[] views;
        lock (_secondaryViewsLock)
        {
            views = _secondaryViews.ToArray();
        }

        foreach (var view in views)
        {
            if (view._gameStarted && !view.Paused)
            {
                view.UpdateViewInternal(gameTime);
            }
        }
    }

    /// <summary>
    /// Draws all attached secondary views.
    /// Called by platform-specific draw methods.
    /// </summary>
    protected void DrawSecondaryViews(GameTime gameTime)
    {
        if (_secondaryViews == null)
            return;

        CCGameView[] views;
        lock (_secondaryViewsLock)
        {
            views = _secondaryViews.ToArray();
        }

        foreach (var view in views)
        {
            if (view._gameStarted && !view.Paused)
            {
                view.DrawViewInternal(gameTime);
            }
        }
    }

    /// <summary>
    /// Internal update for secondary views - handles scene transitions and scheduler.
    /// </summary>
    void UpdateViewInternal(GameTime gameTime)
    {
        _gameTime = gameTime;

        // Handle view-owned scene transitions
        if (_hasOwnScene && _nextViewScene != null)
        {
            SetNextViewScene();
        }

        if (_hasOwnScene)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (RunningScene != null)
            {
                Scheduler.update(deltaTime);
            }
        }

        ProcessInput();
    }

    /// <summary>
    /// Internal draw for secondary views - handles state save/restore and rendering.
    /// Note: Secondary views intentionally skip BeginDraw/EndDraw because
    /// BeginDraw resets the device and clears the backbuffer, which would
    /// wipe the primary view's already-rendered frame.
    /// </summary>
    void DrawViewInternal(GameTime gameTime)
    {
        _gameTime = gameTime;

        // Restore this view's state before drawing
        if (_drawManagerState != null)
        {
            CCDrawManager.RestoreState(_drawManagerState);
        }

        // Ensure viewport is updated before drawing
        if (_viewportDirty)
        {
            UpdateViewport();
            _drawManagerState = CCDrawManager.SaveState();
        }

        {
            CCScene runningScene = RunningScene;

            if (runningScene != null)
            {
                if (_hasOwnScene)
                {
                    runningScene.Visit();
                }
                else
                {
                    Director.MainLoop(gameTime);
                }
            }

            _drawManagerState = CCDrawManager.SaveState();
        }
    }

    #endregion Multi-View Support

    void Initialise()
    {
        if (_viewInitialised)
            return;

        RemoveExistingView();

        PlatformInitialise();

        // Initialize director subsystems if needed
        if (CCDirector.SharedDirector.NeedsInit)
        {
            CCDirector.SharedDirector.Init();
        }

        InitialiseGraphicsDevice();
        InitialiseRunLoop();
        InitialiseInputHandling();

        _viewInitialised = true;
        s_currentViewInstance = new WeakReference(this);
    }

    void InitialiseGraphicsDevice()
    {
        // If a graphics device was already set (e.g., by desktop constructor), use it
        if (_graphicsDevice == null)
        {
            var presParams = new PresentationParameters();
            presParams.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            presParams.DepthStencilFormat = DepthFormat.Depth24Stencil8;
            presParams.BackBufferFormat = SurfaceFormat.Color;
            PlatformInitialiseGraphicsDevice(ref presParams);

            // Try to create graphics device with hi-def profile
            try
            {
                _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, presParams);
            }
            // Otherwise, if unsupported defer to using the low-def profile
            catch (NotSupportedException)
            {
                _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, presParams);
            }
        }

        CCDrawManager.Init(_graphicsDeviceService = new CCGraphicsDeviceService(_graphicsDevice));

        // Initialize content manager if not already initialized
        if (CCContentManager.SharedContentManager == null)
        {
            var serviceProvider = GetServiceProvider();
            CCContentManager.Initialize(serviceProvider, _contentRootDirectory);
        }
    }

    /// <summary>
    /// Gets the service provider for content loading.
    /// Desktop platforms may override this to use the Game's service provider.
    /// </summary>
    IServiceProvider GetServiceProvider()
    {
        IServiceProvider serviceProvider = null;
        PlatformGetServiceProvider(ref serviceProvider);

        if (serviceProvider == null)
        {
            // Create a simple service provider with our graphics device service
            serviceProvider = new CCServiceProvider(_graphicsDeviceService);
        }

        return serviceProvider;
    }

    void InitialiseRunLoop()
    {
        _gameTimer = Stopwatch.StartNew();
        _gameTime = new GameTime();

        _accumulatedElapsedTime = TimeSpan.Zero;
        _targetElapsedTime = TimeSpan.FromTicks(NumOfTicksPerUpdate);
        _maxElapsedTime = TimeSpan.FromMilliseconds(MaxUpdateTimeMilliseconds);
        _previousTicks = 0;
    }

    void LoadGame()
    {
        if (_viewInitialised && _platformInitialised && !_gameLoaded && _viewCreated != null)
        {
            // Ensure viewport and design resolution are set up before firing ViewCreated
            // This ensures CCDirector.SharedDirector.WinSize has the correct value
            if (_viewportDirty && ViewSize.Width > 0 && ViewSize.Height > 0)
            {
                UpdateViewport();
            }

            // Also ensure CCDirector is properly initialized with the design resolution
            if (CCDirector.SharedDirector.WinSize.Width <= 0 || CCDirector.SharedDirector.WinSize.Height <= 0)
            {
                CCDirector.SharedDirector.SetOpenGlView();
            }

            _viewCreated(this, null);
            _gameLoaded = true;
        }
    }

    #endregion Initialisation

    #region Cleaning up

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~CCGameView()
    {
        Dispose(false);
    }

    /// <summary>
    /// Disposes the view and releases all resources.
    /// </summary>
#if ANDROID || IOS || __IOS__
    public new void Dispose()
#else
    public void Dispose()
#endif
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the view and releases resources.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
#if ANDROID || IOS || __IOS__
    protected new virtual void Dispose(bool disposing)
#else
    protected virtual void Dispose(bool disposing)
#endif
    {
        if (_disposed)
            return;

        PlatformDispose(disposing);

        bool canDisposeGraphics = PlatformCanDisposeGraphicsDevice();

        if (disposing)
        {
            if (_graphicsDevice != null && canDisposeGraphics)
            {
                _graphicsDevice.Dispose();
                _graphicsDevice = null;
            }
        }

        s_currentViewInstance = null;
        _disposed = true;
    }

    // MonoGame maintains static references to the underlying context, and more generally, 
    // there's no guarantee that a previous instance of CCGameView will have been disposed by the GC.
    // However, given the limitation imposed by MonoGame to only have one active instance, we need
    // to ensure that this indeed the case and explicitly Dispose of any old game views.
    void RemoveExistingView()
    {
        if (s_currentViewInstance != null && s_currentViewInstance.Target != this)
        {
            var prevGameView = s_currentViewInstance.Target as CCGameView;

            if (prevGameView != null)
            {
                prevGameView.Paused = true;
                prevGameView.Dispose();
            }
        }
    }

    #endregion Cleaning up

    #region Drawing

    void UpdateViewport()
    {
        int width = (int)ViewSize.Width;
        int height = (int)ViewSize.Height;

        // The GraphicsDevice BackBuffer dimensions are used by MonoGame when laying out the viewport
        // so make sure they're updated
        _graphicsDevice.PresentationParameters.BackBufferWidth = width;
        _graphicsDevice.PresentationParameters.BackBufferHeight = height;

        if (_resolutionPolicy != CCViewResolutionPolicy.Custom)
        {
            if (_designResolution.Width <= 0 || _designResolution.Height <= 0)
                return;

            float resolutionScaleX = width / _designResolution.Width;
            float resolutionScaleY = height / _designResolution.Height;

            switch (_resolutionPolicy)
            {
                case CCViewResolutionPolicy.NoBorder:
                    resolutionScaleX = resolutionScaleY = Math.Max(resolutionScaleX, resolutionScaleY);
                    break;
                case CCViewResolutionPolicy.ShowAll:
                    resolutionScaleX = resolutionScaleY = Math.Min(resolutionScaleX, resolutionScaleY);
                    break;
                case CCViewResolutionPolicy.FixedHeight:
                    resolutionScaleX = resolutionScaleY;
                    _designResolution = new CCSize((float)Math.Ceiling(width / resolutionScaleX), _designResolution.Height);
                    break;
                case CCViewResolutionPolicy.FixedWidth:
                    resolutionScaleY = resolutionScaleX;
                    _designResolution = new CCSize(_designResolution.Width, (float)Math.Ceiling(height / resolutionScaleY));
                    break;
                default:
                    break;
            }

            float viewPortW = _designResolution.Width * resolutionScaleX;
            float viewPortH = _designResolution.Height * resolutionScaleY;

            CCRect viewPortRect = new CCRect((width - viewPortW) / 2, (height - viewPortH) / 2,
                viewPortW, viewPortH);

            _viewportRatio = new CCRect(
                viewPortRect.Origin.X / width,
                viewPortRect.Origin.Y / height,
                viewPortRect.Size.Width / width,
                viewPortRect.Size.Height / height
            );
        }

        Viewport = new Viewport((int)(width * _viewportRatio.Origin.X), (int)(height * _viewportRatio.Origin.Y),
            (int)(width * _viewportRatio.Size.Width), (int)(height * _viewportRatio.Size.Height));

        CCPoint center = new CCPoint(ViewSize.Width / 2.0f, ViewSize.Height / 2.0f);
        _defaultViewMatrix = Microsoft.Xna.Framework.Matrix.CreateLookAt(new Vector3(center.X, center.Y, 300.0f), new Vector3(center.X, center.Y, 0.0f), Vector3.Up);
        _defaultProjMatrix = Microsoft.Xna.Framework.Matrix.CreateOrthographic(ViewSize.Width, ViewSize.Height, 1024f, -1024);
        _defaultViewport = new Viewport(0, 0, (int)ViewSize.Width, (int)ViewSize.Height);

        // Update the draw manager with the new resolution
        CCDrawManager.SetDesignResolutionSize(_designResolution.Width, _designResolution.Height, 
            ConvertResolutionPolicy(_resolutionPolicy));

        _viewportDirty = false;
    }

    CCResolutionPolicy ConvertResolutionPolicy(CCViewResolutionPolicy policy)
    {
        switch (policy)
        {
            case CCViewResolutionPolicy.ExactFit:
                return CCResolutionPolicy.ExactFit;
            case CCViewResolutionPolicy.NoBorder:
                return CCResolutionPolicy.NoBorder;
            case CCViewResolutionPolicy.ShowAll:
                return CCResolutionPolicy.ShowAll;
            case CCViewResolutionPolicy.FixedHeight:
                return CCResolutionPolicy.FixedHeight;
            case CCViewResolutionPolicy.FixedWidth:
                return CCResolutionPolicy.FixedWidth;
            default:
                return CCResolutionPolicy.ShowAll;
        }
    }

    void Draw()
    {
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
                        Director.MainLoop(_gameTime);
                    }
                }

                CCDrawManager.EndDraw();

                // Save state after drawing for next frame
                _drawManagerState = CCDrawManager.SaveState();
            }
        }
    }

    void DrawSplitScreen()
    {
        int fullWidth = (int)ViewSize.Width;
        int fullHeight = (int)ViewSize.Height;
        int halfWidth = fullWidth / 2;

        // Only call BeginDraw once - it clears the screen
        if (!CCDrawManager.BeginDraw())
            return;

        float designWidth = _designResolution.Width;
        float designHeight = _designResolution.Height;

        // Draw left side (primary scene)
        SetupSplitScreenProjection(0, 0, halfWidth, fullHeight, designWidth, designHeight);
        CCScene runningScene = RunningScene;
        if (runningScene != null)
        {
            runningScene.Visit();
        }

        // Draw right side (split-screen scene)
        SetupSplitScreenProjection(halfWidth, 0, halfWidth, fullHeight, designWidth, designHeight);
        if (_splitScreenScene != null)
        {
            _splitScreenScene.Visit();
        }

        // Restore full viewport
        _graphicsDevice.Viewport = new Viewport(0, 0, fullWidth, fullHeight);

        CCDrawManager.EndDraw();

        // Save primary state
        _drawManagerState = CCDrawManager.SaveState();
    }

    void SetupSplitScreenProjection(int viewportX, int viewportY, int viewportWidth, int viewportHeight, float designWidth, float designHeight)
    {
        // Calculate scale to fit design resolution in viewport (ShowAll policy)
        float scaleX = viewportWidth / designWidth;
        float scaleY = viewportHeight / designHeight;
        float scale = Math.Min(scaleX, scaleY);

        float scaledWidth = designWidth * scale;
        float scaledHeight = designHeight * scale;

        // Center the content within the viewport region
        float offsetX = (viewportWidth - scaledWidth) / 2f;
        float offsetY = (viewportHeight - scaledHeight) / 2f;

        // Apply centering by constraining the GPU viewport to the scaled content area
        _graphicsDevice.Viewport = new Viewport(
            viewportX + (int)offsetX,
            viewportY + (int)offsetY,
            (int)scaledWidth,
            (int)scaledHeight);

        var projection = Matrix.CreateOrthographicOffCenter(
            0, designWidth,
            0, designHeight,
            -1024f, 1024f);

        CCDrawManager.ProjectionMatrix = projection;
        CCDrawManager.ViewMatrix = Matrix.Identity;
        CCDrawManager.WorldMatrix = Matrix.Identity;
    }

    #endregion Drawing

    #region Run loop

    void Tick()
    {
        while (true)
        {
            var currentTicks = _gameTimer.Elapsed.Ticks;
            _accumulatedElapsedTime += TimeSpan.FromTicks(currentTicks - _previousTicks);
            _previousTicks = currentTicks;

            if (_accumulatedElapsedTime >= _targetElapsedTime)
                break;

            var sleepTime = (int)(_targetElapsedTime - _accumulatedElapsedTime).TotalMilliseconds;
            if (sleepTime > 0)
            {
                System.Threading.Thread.Sleep(sleepTime);
            }
        }

        if (_accumulatedElapsedTime > _maxElapsedTime)
            _accumulatedElapsedTime = _maxElapsedTime;

        _gameTime.ElapsedGameTime = _targetElapsedTime;
        var stepCount = 0;

        while (_accumulatedElapsedTime >= _targetElapsedTime)
        {
            _gameTime.TotalGameTime += _targetElapsedTime;
            _accumulatedElapsedTime -= _targetElapsedTime;
            ++stepCount;

            Update(_gameTime);
        }

        _gameTime.ElapsedGameTime = TimeSpan.FromTicks(_targetElapsedTime.Ticks * stepCount);
    }

    void Update(GameTime time)
    {
        float deltaTime = (float)_gameTime.ElapsedGameTime.TotalSeconds;

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
            if (RunningScene != null)
            {
                Scheduler.update(deltaTime);
            }
        }
        else
        {
            // Director.Update handles SetNextScene for director-managed transitions
            // as well as the scheduler update
            Director.Update(time);
        }

        ProcessInput();
    }

    #endregion Run loop

    #region Platform-specific partial methods

    /// <summary>
    /// Platform-specific initialization.
    /// </summary>
    partial void PlatformInitialise();

    /// <summary>
    /// Platform-specific graphics device initialization.
    /// </summary>
    partial void PlatformInitialiseGraphicsDevice(ref PresentationParameters presParams);

    /// <summary>
    /// Platform-specific game start.
    /// </summary>
    partial void PlatformStartGame();

    /// <summary>
    /// Platform-specific input handling initialization.
    /// </summary>
    partial void InitialiseInputHandling();

    /// <summary>
    /// Platform-specific input processing.
    /// </summary>
    partial void ProcessInput();

    /// <summary>
    /// Platform-specific pause state update.
    /// </summary>
    partial void PlatformUpdatePaused();

    /// <summary>
    /// Platform-specific presentation/buffer swap.
    /// </summary>
    partial void PlatformPresent();

    /// <summary>
    /// Platform-specific disposal.
    /// </summary>
    partial void PlatformDispose(bool disposing);

    /// <summary>
    /// Platform-specific check if graphics device can be safely disposed.
    /// </summary>
    partial void PlatformCanDisposeGraphicsDevice(ref bool canDispose);

    bool PlatformCanDisposeGraphicsDevice()
    {
        bool canDispose = true;
        PlatformCanDisposeGraphicsDevice(ref canDispose);
        return canDispose;
    }

    /// <summary>
    /// Platform-specific service provider retrieval.
    /// Desktop platforms should provide the Game's service provider.
    /// </summary>
    partial void PlatformGetServiceProvider(ref IServiceProvider serviceProvider);

    #endregion Platform-specific partial methods
}
