#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Cocos2D;

public interface ICCDirectorDelegate
{
    void UpdateProjection();
}

/// <summary>
/// Class that creates and handle the main Window and manages how and when to execute the Scenes.
/// 
/// The CCDirector is also responsible for:
///     - initializing the OpenGL context
///     - setting the OpenGL pixel format (default on is RGB565)
///     - setting the OpenGL buffer depth (default one is 0-bit)
///     - setting the projection (default one is 3D)
///     - setting the orientation (default one is Portrait)
/// 
/// Since the CCDirector is a singleton, the standard way to use it is by calling: _
/// CCDirector::sharedDirector()->methodName();
/// 
/// The CCDirector also sets the default OpenGL context:
///     - GL_TEXTURE_2D is enabled
///     - GL_VERTEX_ARRAY is enabled
///     - GL_COLOR_ARRAY is enabled
///     - GL_TEXTURE_COORD_ARRAY is enabled.  
/// </summary>

public abstract class CCDirector
{
    // The shared director is created lazily and exactly once. Lazy<T> defaults to
    // LazyThreadSafetyMode.ExecutionAndPublication, so the instance is published only
    // after Init() has fully populated it (ActionManager, Scheduler, dispatchers);
    // concurrent first callers block until then instead of observing a half-built
    // director (which previously caused an intermittent NullReferenceException when a
    // CCNode constructed on another thread cached a null ActionManager).
    private static readonly Lazy<CCDirector> s_sharedDirector = new Lazy<CCDirector>(() =>
    {
        var director = new CCDisplayLinkDirector();
        director.Init();
        return director;
    });

    private readonly float kDefaultFPS = 60f;
    private readonly List<CCScene> _scenesStack = new List<CCScene>();
    private bool _nextDeltaTimeZero;
    private bool _paused;
    protected bool m_bPurgeDirectorInNextLoop; // this flag will be set to true in end()
    private bool _sendCleanupToScene;
    protected double m_dAnimationInterval;
    protected double m_dOldAnimationInterval;
    private CCDirectorProjection _projection;
    private float _contentScaleFactor = 1.0f;
    private float _deltaTime;
    private bool _needsInit = true;
    internal CCSize m_obWinSizeInPoints;
		
    private CCAccelerometer _accelerometer;
		private CCActionManager _actionManager;
    private CCKeypadDispatcher _keypadDispatcher;
		private CCKeyboardDispatcher _keyboardDispatcher;
    private CCScene _nextScene;
    private CCNode _notificationNode;

    private ICCDirectorDelegate _projectionDelegate;
    private CCScene _runningScene;
    private CCScheduler _scheduler;
    private CCTouchDispatcher _touchDispatcher;

    private bool _displayStats;
    
    private uint _totalFrames;
    private float _accumDt;
    private uint _updateCount;
    private float _accumDraw;
    private uint _drawCount;
    private float _accumUpdate;
    
    private CCLabel _fpsLabel;
    private CCLabel _updateTimeLabel;
    private CCLabel _drawTimeLabel;
    private CCLabel _drawsLabel;
    private CCLabel _memoryLabel;
    private CCLabel _gcLabel;

    // Stopwatch for measure the time.
    private Stopwatch _stopwatch;
    
    #region State Management
		
    private string _storageDirName = "cocos2dDirector";
    private string _saveFileName = "SceneList.dat";
    private string _sceneSaveFileName = "Scene{0}.dat";

    /// <summary>
    /// Write out the current state of the director and all of its scenes.
    /// </summary>
    public void SerializeState()
    {
        // open up isolated storage
        using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
        {
            // if our screen manager directory already exists, delete the contents
            if (storage.DirectoryExists(_storageDirName))
            {
                DeleteState(storage);
            }

            // otherwise just create the directory
            else
            {
                storage.CreateDirectory(_storageDirName);
            }

            // create a file we'll use to store the list of screens in the stack

            CCLog.Log("Saving CCDirector state to file: " + Path.Combine(_storageDirName, _saveFileName));

            try
            {
                using (IsolatedStorageFileStream stream = storage.OpenFile(Path.Combine(_storageDirName, _saveFileName), FileMode.OpenOrCreate))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                {
                    // write out the full name of all the types in our stack so we can
                    // recreate them if needed.
                    foreach (CCScene scene in _scenesStack)
                    {
                        if (scene.IsSerializable)
                        {
                                writer.WriteLine(scene.GetType().AssemblyQualifiedName);
                            }
                            else
                            {
                                CCLog.Log("Scene is not serializable: " + scene.GetType().FullName);
                        }
                    }
                    // Write out our local state
                    if (_runningScene != null && _runningScene.IsSerializable)
                    {
                            writer.WriteLine("m_pRunningScene"); // stable on-disk marker - intentionally decoupled from the field name; do not rename
                            writer.WriteLine(_runningScene.GetType().AssemblyQualifiedName);
                    }
                    // Add my own state 
                    // [*]name=value
                    //

                }
            }

            // now we create a new file stream for each screen so it can save its state
            // if it needs to. we name each file "ScreenX.dat" where X is the index of
            // the screen in the stack, to ensure the files are uniquely named
            int screenIndex = 0;
            string fileName = null;
            foreach (CCScene scene in _scenesStack)
            {
                if (scene.IsSerializable)
                {
                    fileName = string.Format(Path.Combine(_storageDirName, _sceneSaveFileName), screenIndex);

                    // open up the stream and let the screen serialize whatever state it wants
                    using (IsolatedStorageFileStream stream = storage.CreateFile(fileName))
                    {
                        scene.Serialize(stream);
                    }

                    screenIndex++;
                }
            }
            // Write the current running scene
            if (_runningScene != null && _runningScene.IsSerializable)
            {
                fileName = string.Format(Path.Combine(_storageDirName, _sceneSaveFileName), "XX");
                // open up the stream and let the screen serialize whatever state it wants
                using (IsolatedStorageFileStream stream = storage.CreateFile(fileName))
                {
                    _runningScene.Serialize(stream);
                }
            }
        }
            catch (Exception ex)
            {
                CCLog.Log("Failed to serialize the CCDirector state. Erasing the save files.");
                CCLog.Log(ex.ToString());
                DeleteState(storage);
            }
    }
    }

    private void DeserializeMyState(string name, string v)
    {
        // TODO
    }

    public bool DeserializeState()
    {
        try
        {
        // open up isolated storage
        using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
        {
            // see if our saved state directory exists
            if (storage.DirectoryExists(_storageDirName))
            {
                string saveFile = System.IO.Path.Combine(_storageDirName, _saveFileName);
                try
                {
                        CCLog.Log("Loading director data file: {0}", saveFile);
                    // see if we have a screen list
                    if (storage.FileExists(saveFile))
                    {
                        // load the list of screen types
                        using (IsolatedStorageFileStream stream = storage.OpenFile(saveFile, FileMode.Open, FileAccess.Read))
                        {
                                using (StreamReader reader = new StreamReader(stream))
                            {
                                    CCLog.Log("Director save file contains {0} bytes.", reader.BaseStream.Length);
                                    try
                                    {
                                        while (true)
                                {
                                    // read a line from our file
                                            string line = reader.ReadLine();
                                            if (line == null)
                                            {
                                                break;
                                            }
                                            CCLog.Log("Restoring: {0}", line);

                                    // if it isn't blank, we can create a screen from it
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        if (line.StartsWith("[*]"))
                                        {
                                            // Reading my state
                                            string s = line.Substring(3);
                                            int idx = s.IndexOf('=');
                                            if (idx > -1)
                                            {
                                                string name = s.Substring(0, idx);
                                                string v = s.Substring(idx + 1);
                                                        CCLog.Log("Restoring: {0} = {1}", name, v);
                                                DeserializeMyState(name, v);
                                            }
                                        }
                                        else
                                        {
                                            Type screenType = Type.GetType(line);
                                            CCScene scene = Activator.CreateInstance(screenType) as CCScene;
                                                    PushScene(scene);
                                                    //                                                    _scenesStack.Add(scene);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // EndOfStreamException
                                        // this is OK here.
                                }
                            }
                        }
                        // Now we deserialize our own state.
                    }
                        else
                        {
                            CCLog.Log("save file does not exist.");
                        }

                    // next we give each screen a chance to deserialize from the disk
                    for (int i = 0; i < _scenesStack.Count; i++)
                    {
                            string filename = System.IO.Path.Combine(_storageDirName, string.Format(_sceneSaveFileName, i));
                            if (storage.FileExists(filename))
                            {
                        using (IsolatedStorageFileStream stream = storage.OpenFile(filename, FileMode.Open, FileAccess.Read))
                        {
                                    CCLog.Log("Restoring state for scene {0}", filename);
                            _scenesStack[i].Deserialize(stream);
                        }
                    }
                        }
                    if (_scenesStack.Count > 0)
                    {
                            CCLog.Log("Director is running with scene..");

                        RunWithScene(_scenesStack[_scenesStack.Count - 1]); // always at the top of the stack
                    }
                    return (_scenesStack.Count > 0 && _runningScene != null);
                }
                    catch (Exception ex)
                {
                    // if an exception was thrown while reading, odds are we cannot recover
                    // from the saved state, so we will delete it so the game can correctly
                    // launch.
                    DeleteState(storage);
                        CCLog.Log("Failed to deserialize the director state, removing old storage file.");
                        CCLog.Log(ex.ToString());
                }
            }
        }
        }
        catch (Exception ex)
        {
            CCLog.Log("Failed to deserialize director state.");
            CCLog.Log(ex.ToString());
        }

        return false;
    }

    /// <summary>
    /// Deletes the saved state files from isolated storage.
    /// </summary>
    private void DeleteState(IsolatedStorageFile storage)
    {
        // glob on all of the files in the directory and delete them
        string[] files = storage.GetFileNames(System.IO.Path.Combine(_storageDirName, "*"));
        foreach (string file in files)
        {
            storage.DeleteFile(Path.Combine(_storageDirName, file));
        }
                    }
    #endregion

    public ICCDirectorDelegate Delegate
    {
        get { return _projectionDelegate; }
        set { _projectionDelegate = value; }
    }

    /// <summary>
    /// Calls SetViewPortInPoints found in CCDrawManager. This will setup the project viewport.
    /// </summary>
    public void SetViewport()
    {
        CCDrawManager.SetViewPortInPoints(0, 0, (int)m_obWinSizeInPoints.Width, (int)m_obWinSizeInPoints.Height);
    }

    public CCDirectorProjection Projection
    {
        get { return _projection; }
        set
        {
            SetViewport();

            CCSize size = m_obWinSizeInPoints;

            switch (value)
            {
                case CCDirectorProjection.Projection2D:

                    CCDrawManager.ProjectionMatrix = Matrix.CreateOrthographicOffCenter(
                        0, size.Width,
                        0, size.Height,
                        -1024.0f, 1024.0f
                        );

                    CCDrawManager.ViewMatrix = Matrix.Identity;
                    
                    CCDrawManager.WorldMatrix = Matrix.Identity;
                    break;

                case CCDirectorProjection.Projection3D:

                    CCDrawManager.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                        MathHelper.Pi / 3.0f,
                        size.Width / size.Height,
                        0.1f, Math.Max(size.Width,size.Height)*2f // 1500 //ZEye * 2f
                        );

                    CCDrawManager.ViewMatrix = Matrix.CreateLookAt(
                        new Vector3(size.Width / 2.0f, size.Height / 2.0f, ZEye),
                        new Vector3(size.Width / 2.0f, size.Height / 2.0f, 0f),
                        Vector3.Up
                        );

                        CCDrawManager.WorldMatrix = Matrix.Identity;
                    break;

                case CCDirectorProjection.Custom:
                    if (_projectionDelegate != null)
                    {
                        _projectionDelegate.UpdateProjection();
                    }
                    break;

                default:
                    Debug.Assert(true, "cocos2d: Director: unrecognized projection");
                    break;
            }

            _projection = value;
        }
    }

    public float ZEye
    {
        get { return (m_obWinSizeInPoints.Height / 1.1566f); }
    }

    public CCSize VisibleSize
    {
        get { return CCDrawManager.VisibleSize; }
    }

    public CCPoint VisibleOrigin
    {
        get { return CCDrawManager.VisibleOrigin; }
    }

    public CCScheduler Scheduler
    {
        get { return _scheduler; }
        set { _scheduler = value; }
    }

    public CCActionManager ActionManager
    {
        get { return _actionManager; }
        set { _actionManager = value; }
    }

    public CCTouchDispatcher TouchDispatcher
    {
        get { return _touchDispatcher; }
        set { _touchDispatcher = value; }
    }

    public CCKeypadDispatcher KeypadDispatcher
    {
        get { return _keypadDispatcher; }
        set { _keypadDispatcher = value; }
    }

		public CCKeyboardDispatcher KeyboardDispatcher
		{
			get { return _keyboardDispatcher; }
			set { _keyboardDispatcher = value; }
		}

		public CCAccelerometer Accelerometer
    {
        get { return _accelerometer; }
        set { _accelerometer = value; }
    }
    public CCScene RunningScene
    {
        get { return _runningScene; }
    }

    public virtual double AnimationInterval
    {
        get { return m_dAnimationInterval; }
        set { m_dAnimationInterval = value; }
    }

    public bool DisplayStats
    {
        get { return _displayStats; }
        set
        {
            if (value != _displayStats)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
            }
            _displayStats = value;
        }
    }

    public void ResetStats()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
        _accumDt = 0.0f;
        _totalFrames = 0;
    }

    public bool IsPaused
    {
        get { return _paused; }
    }

    public CCNode NotificationNode
    {
        get { return _notificationNode; }
        set { _notificationNode = value; }
    }


    /// <summary>
    /// returns a shared instance of the director
    /// </summary>
    /// <value> </value>
    public static CCDirector SharedDirector
    {
        get { return s_sharedDirector.Value; }
    }

    public virtual bool NeedsInit
    {
        get { return _needsInit; }
        set { _needsInit = value; }
    }

    public virtual bool Init()
    {
        SetDefaultValues();

        // scenes
        _runningScene = null;
        _nextScene = null;

        _notificationNode = null;

        m_dOldAnimationInterval = m_dAnimationInterval = 1.0 / kDefaultFPS;

        // Set default projection (3D)
        _projection = CCDirectorProjection.Default;

        // projection delegate if "Custom" projection is used
        _projectionDelegate = null;

        // FPS
        _accumDt = 0.0f;
        _fpsLabel = null;
        _updateTimeLabel = null;
        _drawTimeLabel = null;
        _drawsLabel = null;
        _displayStats = false;
        _totalFrames = 0;

        _stopwatch = new Stopwatch();

        // paused ?
        _paused = false;

        // purge ?
        m_bPurgeDirectorInNextLoop = false;

        m_obWinSizeInPoints = CCSize.Zero;

        //m_pobOpenGLView = null;

        _contentScaleFactor = 1.0f;

        // scheduler
        _scheduler = new CCScheduler();
        // action manager
        _actionManager = new CCActionManager();
        _scheduler.ScheduleUpdateForTarget(_actionManager, CCScheduler.kCCPrioritySystem, false);
        // touchDispatcher
        _touchDispatcher = new CCTouchDispatcher();
        _touchDispatcher.Init();

        // KeypadDispatcher
        _keypadDispatcher = new CCKeypadDispatcher();

			// KeyboardDispatcher
			_keyboardDispatcher = new CCKeyboardDispatcher();

			// Accelerometer
        _accelerometer = new CCAccelerometer();

        _needsInit = false;
        return true;
    }

    private bool _gamePadEnabled = false;

    /// <summary>
    /// Set to true if this platform has a game pad connected.
    /// </summary>
    public bool GamePadEnabled
    {
        get { 
            return (_gamePadEnabled); 
        }
        set {
            _gamePadEnabled = value;
        }
    }

    /// <summary>
    /// Enables alpha transparency, disables the depth stencil and sets the initial projection.
    /// </summary>
    internal void SetRenderDefaultValues()
    {
        SetAlphaBlending(true);
        SetDepthTest(false);
        Projection = _projection;
    }

    protected void SetDefaultValues()
    {
    }

    public void Update(GameTime gameTime)
    {
        float startTime = 0;
        
        if (_displayStats)
        {
            startTime = (float)_stopwatch.Elapsed.TotalMilliseconds;
        }

        if (!_paused)
        {
            if (_nextDeltaTimeZero)
            {
                _deltaTime = 0;
                _nextDeltaTimeZero = false;
            }
            else
            {
                _deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
            }

            // In Seconds
            _scheduler.update(_deltaTime);
        }

        /* to avoid flickr, nextScene MUST be here: after tick and before draw.
         XXX: Which bug is this one. It seems that it can't be reproduced with v0.9 */
        if (_nextScene != null)
        {
            SetNextScene();
        }

        _accumDt += _deltaTime;

        if (_displayStats)
        {
            _updateCount++;
            _accumUpdate += (float)_stopwatch.Elapsed.TotalMilliseconds - startTime;
        }
    }

    public int GraphIndex { get; set; }

    /// <summary>
    /// Draw the scene.
    /// This method is called every frame. Don't call it manually.
    /// </summary>
    protected void DrawScene(GameTime gameTime)
    {
        if (_needsInit)
        {
            return;
        }

        float startTime = 0;
        
        if (_displayStats)
        {
            startTime = (float)_stopwatch.Elapsed.TotalMilliseconds;
        }

        CCDrawManager.PushMatrix();

        // draw the scene
        if (_runningScene != null)
        {
            GraphIndex = 0;
            _runningScene.Visit();
        }

        // draw the notifications node
        if (_notificationNode != null)
        {
            NotificationNode.Visit();
        }

        if (_displayStats)
        {
            ShowStats();
        }

        CCDrawManager.PopMatrix();

        _totalFrames++;

        if (_displayStats)
        {
            _drawCount++;
            _accumDraw += (float)_stopwatch.Elapsed.TotalMilliseconds - startTime;
        }
    }


    public abstract void MainLoop(GameTime gameTime);

    public void SetOpenGlView()
    {
        // set size
        m_obWinSizeInPoints = CCDrawManager.DesignResolutionSize;

        SetRenderDefaultValues();
        EnableTouchDispatcher();
    }

    public void EnableTouchDispatcher()
    {
        // When using CCGameView, CCApplication.SharedApplication may not exist
        // Touch events are handled by CCGameView in that case
        if (CCApplication.SharedApplication != null)
        {
            CCApplication.SharedApplication.TouchDelegate = _touchDispatcher;
        }
        _touchDispatcher.IsDispatchEvents = true;
    }

    public void SetNextDeltaTimeZero(bool bNextDeltaTimeZero)
    {
        _nextDeltaTimeZero = bNextDeltaTimeZero;
    }

    public void PurgeCachedData()
    {
        CCLabelBMFont.PurgeCachedData();
        CCPixelLabel.PurgeCachedData();
        CCTextureCache.SharedTextureCache.RemoveAllTextures();
        //CCFileUtils::sharedFileUtils()->purgeCachedEntries();
    }

    /// <summary>
    /// enables/disables OpenGL alpha blending 
    /// </summary>
    /// <param name="bOn"></param>
    public void SetAlphaBlending(bool bOn)
    {
        if (bOn)
        {
            CCDrawManager.BlendFunc(CCBlendFunc.AlphaBlend);
        }
        else
        {
            CCDrawManager.BlendFunc(new CCBlendFunc(CCOGLES.GL_ONE, CCOGLES.GL_ZERO));
        }
    }

    /// <summary>
    /// enables/disables OpenGL depth test
    /// </summary>
    /// <param name="bOn"></param>
    public void SetDepthTest(bool bOn)
    {
        CCDrawManager.DepthTest = bOn;
    }

    public CCPoint ConvertToGl(CCPoint uiPoint)
    {
        return new CCPoint(uiPoint.X, m_obWinSizeInPoints.Height - uiPoint.Y);
    }

    public CCPoint ConvertToUi(CCPoint glPoint)
    {
        return new CCPoint(glPoint.X, m_obWinSizeInPoints.Height - glPoint.Y);
    }

    public CCSize WinSize
    {
        get { return m_obWinSizeInPoints; }
    }

    public CCSize WinSizeInPixels
    {
        get { return m_obWinSizeInPoints * _contentScaleFactor; }
    }

    public void End()
    {
        m_bPurgeDirectorInNextLoop = true;
    }

    protected void PurgeDirector()
    {
        // cleanup scheduler
        Scheduler.UnscheduleAll();

        // don't release the event handlers
        // They are needed in case the director is run again
        _touchDispatcher.RemoveAllDelegates();

        if (_runningScene != null)
        {
            _runningScene.OnExitTransitionDidStart();
            _runningScene.OnExit();
            _runningScene.Cleanup();
        }

        _runningScene = null;
        _nextScene = null;

        // remove all objects, but don't release it.
        // runWithScene might be executed after 'end'.
        _scenesStack.Clear();

        StopAnimation();

        // purge bitmap cache
        CCLabelBMFont.PurgeCachedData();
        CCPixelLabel.PurgeCachedData();

        // purge all managed caches
        CCAnimationCache.PurgeSharedAnimationCache();
        CCSpriteFrameCache.PurgeSharedSpriteFrameCache();
        CCTextureCache.PurgeSharedTextureCache();
        // CCFileUtils.PurgeFileUtils();
        // CCConfiguration.purgeConfiguration();

        // cocos2d-x specific data structures
        CCUserDefault.PurgeSharedUserDefault();
        // CCNotificationCenter.purgeNotificationCenter();

        CCDrawManager.PurgeDrawManager();

        _needsInit = true;
    }

    public void Pause()
    {
        if (_paused)
        {
            return;
        }

        m_dOldAnimationInterval = m_dAnimationInterval;

        // when paused, don't consume CPU
        AnimationInterval = 1 / 4.0;
        _paused = true;
    }

    public void ResumeFromBackground()
    {
        Resume();
        
        if (_runningScene != null)
        {
            bool runningIsTransition = _runningScene is CCTransitionScene;
            if (!runningIsTransition)
            {
                _runningScene.OnEnter();
                _runningScene.OnEnterTransitionDidFinish();
            }
        }
    }

    public void Resume()
    {
        if (_needsInit)
        {
            CCLog.Log("CCDirector(): Resume needs Init(). The director will re-initialize.");
            Init();
        }
        if (!_paused)
        {
            return;
        }

        CCLog.Log("CCDirector(): Resume called with {0} scenes", _scenesStack.Count);

        AnimationInterval = m_dOldAnimationInterval;

        _paused = false;
        _deltaTime = 0;
    }

    public bool IsSendCleanupToScene()
    {
        return _sendCleanupToScene;
    }

    public abstract void StopAnimation();

    public abstract void StartAnimation();

    #region mobile platforms specific functions

    public float ContentScaleFactor
    {
        get { return _contentScaleFactor; }
        set
        {
            if (value != _contentScaleFactor)
            {
                _contentScaleFactor = value;
            }
        }
    }

    #endregion

    #region Scene Management

    public void ResetSceneStack()
    {
        CCLog.Log("CCDirector(): ResetSceneStack, clearing out {0} scenes.", _scenesStack.Count);

        _runningScene = null;
        _scenesStack.Clear();
        _nextScene = null;
    }

    public void RunWithScene(CCScene pScene)
    {
        Debug.Assert(pScene != null, "the scene should not be null");
        Debug.Assert(_runningScene == null, "Use runWithScene: instead to start the director");

        PushScene(pScene);
        StartAnimation();
    }

    /// <summary>
    /// Replaces the current scene at the top of the stack with the given scene.
    /// </summary>
    /// <param name="pScene"></param>
    public void ReplaceScene(CCScene pScene)
    {
        Debug.Assert(_runningScene != null, "Use runWithScene: instead to start the director");
        Debug.Assert(pScene != null, "the scene should not be null");

        int index = _scenesStack.Count;

        _sendCleanupToScene = true;
        if (index == 0)
        {
            _scenesStack.Add(pScene);
        }
        else
        {
        _scenesStack[index - 1] = pScene;
        }
        _nextScene = pScene;
    }

    /** Give the number of scenes present in the scene stack.
     *  Note: this count also includes the root scene node.
     */
    public int SceneCount
    {
        get { return _scenesStack.Count; }
    }

    /// <summary>
    /// Push the given scene to the top of the scene stack.
    /// </summary>
    /// <param name="pScene"></param>
    public void PushScene(CCScene pScene)
    {
        Debug.Assert(pScene != null, "the scene should not null");

        _sendCleanupToScene = false;

        _scenesStack.Add(pScene);
        _nextScene = pScene;
    }

    /// <summary>
    /// Returns true if there is more than 1 scene on the stack.
    /// </summary>
    /// <returns></returns>
    public bool CanPopScene
    {
        get
        {
            int c = _scenesStack.Count;
            return (c > 1);
        }
    }

    public void PopScene(float t, CCTransitionScene s)
    {
        Debug.Assert(_runningScene != null, "_runningScene cannot be null");

        if (_scenesStack.Count > 0)
        {
            // CCScene s = _scenesStack[_scenesStack.Count - 1];
            _scenesStack.RemoveAt(_scenesStack.Count - 1);
        }
        int c = _scenesStack.Count;

        if (c == 0)
        {
            End(); // This should not happen here b/c we need to capture the current state and just deactivate the game (for Android).
        }
        else
        {
            _sendCleanupToScene = true;
            _nextScene = _scenesStack[c - 1];
            if (s != null)
            {
                _nextScene.Visible = true;
                s.Reset(t, _nextScene);
                _scenesStack.Add(s);
                _nextScene = s;
            }
        }
    }

    public void PopScene()
    {
        PopScene(0, null);
    }

    /** Pops out all scenes from the queue until the root scene in the queue.
    * This scene will replace the running one.
    * Internally it will call `PopToSceneStackLevel(1)`
    */
    public void PopToRootScene()
    {
        PopToSceneStackLevel(1);
    }

    /** Pops out all scenes from the queue until it reaches `level`.
     *   If level is 0, it will end the director.
     *   If level is 1, it will pop all scenes until it reaches to root scene.
     *   If level is <= than the current stack level, it won't do anything.
     */
    public void PopToSceneStackLevel(int level)
    {
        Debug.Assert(_runningScene != null, "A running Scene is needed");
        int c = _scenesStack.Count;

        // level 0? -> end
        if (level == 0)
        {
            End();
            return;
        }
        
        // current level or lower -> nothing
        if (level >= c)
            return;
        
        // pop stack until reaching desired level
        while (c > level)
        {
            var current = _scenesStack[_scenesStack.Count - 1];
            
            if (current.IsRunning)
            {
                current.OnExitTransitionDidStart();
                current.OnExit();
            }
            
            current.Cleanup();
            _scenesStack.RemoveAt(_scenesStack.Count - 1);
            c--;
        }
        
        _nextScene = _scenesStack[_scenesStack.Count - 1];
        _sendCleanupToScene = false;
    }

    protected void SetNextScene()
    {
        bool runningIsTransition = _runningScene != null && _runningScene.IsTransition;// is CCTransitionScene;

        // If it is not a transition, call onExit/cleanup
        if (!_nextScene.IsTransition)
        {
            if (_runningScene != null)
            {
                _runningScene.OnExitTransitionDidStart(); 
                _runningScene.OnExit();

                // issue #709. the root node (scene) should receive the cleanup message too
                // otherwise it might be leaked.
                if (_sendCleanupToScene)
                {
                    _runningScene.Cleanup();

                    GC.Collect();
                }
            }
        }

        _runningScene = _nextScene;
        _nextScene = null;

        if (!runningIsTransition && _runningScene != null)
        {
            _runningScene.OnEnter();
            _runningScene.OnEnterTransitionDidFinish();
        }
    }

		#endregion

    public void CreateStatsLabel()
    {
        if (_fpsLabel == null)
        {
            CCTexture2D texture;
            CCTextureCache textureCache = CCTextureCache.SharedTextureCache;

            try
            {
                if (!textureCache.Contains("cc_fps_images"))
                {
                    texture = textureCache.AddImage(CCFPSImage.PngData, "cc_fps_images", SurfaceFormat.Bgra4444);
                }
                else
                {
                    texture = textureCache.TextureForKey("cc_fps_images");
                }

                if (texture == null || (texture.ContentSize.Width == 0 && texture.ContentSize.Height == 0))
                {
                    _displayStats = false;
                    return;
                }
            }
            catch (Exception)
            {
                // MonoGame may not allow texture.fromstream, so catch this exception here
                // and disable the stats
                _displayStats = false;
                return;
            }

            try
            {
                _fpsLabel = new CCLabel("00.0", "arial", 12);
                _fpsLabel.AnchorPoint = CCPoint.AnchorMiddleLeft;

                _updateTimeLabel = new CCLabel("00.0", "arial", 12);
                _updateTimeLabel.AnchorPoint = CCPoint.AnchorMiddleLeft;

                _drawTimeLabel = new CCLabel("00.0", "arial", 12);
                _drawTimeLabel.AnchorPoint = CCPoint.AnchorMiddleLeft;

                _drawsLabel = new CCLabel("00.0", "arial", 12);
                _drawsLabel.AnchorPoint = CCPoint.AnchorMiddleLeft;

                _memoryLabel = new CCLabel("00.0", "arial", 12);
                _memoryLabel.Color = new CCColor3B(0, 0, 255);
                _memoryLabel.AnchorPoint = CCPoint.AnchorMiddleLeft;

                _gcLabel = new CCLabel("00.0", "arial", 12);
                _gcLabel.Color = new CCColor3B(255, 0, 0);
                _gcLabel.AnchorPoint = CCPoint.AnchorMiddleLeft;
            }
            catch (Exception ex)
            {
                _fpsLabel = null;
                _displayStats = false;
                CCLog.Log("Failed to create the stats labels.");
                CCLog.Log(ex.ToString());
                return;
            }
        }

        float factor = CCDrawManager.DesignResolutionSize.Height / 320.0f;
        var pos = CCDirector.SharedDirector.VisibleOrigin;

        _fpsLabel.Scale = factor;
        _updateTimeLabel.Scale = factor;
        _drawTimeLabel.Scale = factor;
        _drawsLabel.Scale = factor;
        _memoryLabel.Scale = factor;
        _gcLabel.Scale = factor;

        _memoryLabel.Position = new CCPoint(0, 97 * factor) + pos;
        _gcLabel.Position = new CCPoint(0, 80 * factor) + pos;
        _drawsLabel.Position = new CCPoint(0, 63 * factor) + pos;
        _updateTimeLabel.Position = new CCPoint(0, 46 * factor) + pos;
        _drawTimeLabel.Position = new CCPoint(0, 29 * factor) + pos;
        _fpsLabel.Position = new CCPoint(0, 12 * factor) + pos;
    }

    private WeakReference _wk = new WeakReference(new object());
    private int _GCCount;  

    // display the FPS using a LabelAtlas
    // updates the FPS every frame
    private void ShowStats()
    {
        if (_displayStats)
        {
            if (!_wk.IsAlive)
            {
                _GCCount++;
                _wk = new WeakReference(new object());
            }

            if (_fpsLabel == null)
                CreateStatsLabel();

            if (_fpsLabel != null && _updateTimeLabel != null && _drawsLabel != null)
            {
                if (_accumDt > CCMacros.CCDirectorStatsUpdateIntervalInSeconds)
                {
                    _fpsLabel.Text = (String.Format("FPS: {0:00.0}", _drawCount / _accumDt));

                    _updateTimeLabel.Text = (String.Format("Update: {0:0.000}", _accumUpdate / _updateCount));
                    _drawTimeLabel.Text = (String.Format("Draw: {0:0.000}", _accumDraw / _drawCount));
                    _drawsLabel.Text = (String.Format("Draws: {0:000}", CCDrawManager.DrawCount));

                    _accumDt = _accumDraw = _accumUpdate = 0;
                    _drawCount = _updateCount = 0;


                    _memoryLabel.Text = String.Format("Memory: {0}", GC.GetTotalMemory(false) / 1024);
                    _gcLabel.Text = String.Format("GC: {0}", _GCCount);
                }

                _drawsLabel.Visit();
                _fpsLabel.Visit();
                _updateTimeLabel.Visit();
                _drawTimeLabel.Visit();
                _memoryLabel.Visit();
                _gcLabel.Visit();
            }
        }    
    }
}

/// <summary>
///  Possible OpenGL projections used by director
/// </summary>
public enum CCDirectorProjection
{
    /// sets a 2D projection (orthogonal projection)
    Projection2D,

    /// sets a 3D projection with a fovy=60, znear=0.5f and zfar=1500.
    Projection3D,

    /// it calls "updateProjection" on the projection delegate.
    Custom,

    /// Default projection is 3D projection
    Default = Projection3D
}