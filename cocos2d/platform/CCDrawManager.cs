#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public enum CCClipMode
{
    /// <summary>
    /// No clipping of children
    /// </summary>
    None,
    /// <summary>
    /// Clipping with a ScissorRect
    /// </summary>
    Bounds,
    /// <summary>
    /// Clipping with the ScissorRect and in a RenderTarget
    /// </summary>
    BoundsWithRenderTarget
}

/// <summary>
/// Holds the drawing state for a CCGameView instance.
/// Used to support multiple views by saving/restoring state between draws.
/// </summary>
/// <remarks>
/// This state captures viewport, matrices, and resolution settings. It does not save
/// lower-level graphics state such as the current texture, effect, blend state, or render target.
/// Views sharing a graphics device should ensure compatible rendering states, or explicitly
/// set the required graphics state at the beginning of each draw call.
/// </remarks>
public class CCDrawManagerState
{
    public Matrix WorldMatrix;
    public Matrix ViewMatrix;
    public Matrix ProjectionMatrix;
    public Matrix CombinedMatrix;
    public Matrix[] MatrixStack;
    public int StackIndex;
    public CCSize DesignResolutionSize;
    public CCSize ScreenSize;
    public CCRect ViewPortRect;
    public float ScaleX;
    public float ScaleY;
    public CCResolutionPolicy ResolutionPolicy;
    public Viewport Viewport;
    public bool DepthTest;

    public CCDrawManagerState()
    {
        MatrixStack = new Matrix[100];
        WorldMatrix = Matrix.Identity;
        ViewMatrix = Matrix.Identity;
        ProjectionMatrix = Matrix.Identity;
        CombinedMatrix = Matrix.Identity;
        StackIndex = 0;
        ScaleX = 1.0f;
        ScaleY = 1.0f;
        DepthTest = true;
        ResolutionPolicy = CCResolutionPolicy.UnKnown;
    }
}

public static class CCDrawManager
{
    private const int DefaultQuadBufferSize = 1024 * 4;
    public static string DefaultFont = "arial";

    public static BasicEffect PrimitiveEffect;
    public static AlphaTestEffect AlphaTestEffect;

    private static BasicEffect _defaultEffect;
    private static Effect _currentEffect;
    private static readonly Stack<Effect> _effectStack = new Stack<Effect>();

    public static SpriteBatch spriteBatch;
    internal static GraphicsDevice graphicsDevice;

    internal static Matrix m_worldMatrix;
    internal static Matrix m_viewMatrix;
    internal static Matrix m_projectionMatrix;

    private static readonly Matrix[] _matrixStack = new Matrix[100];
    private static int _stackIndex;

    internal static Matrix m_Matrix;
    private static Matrix _tmpMatrix;

    private static RenderTarget2D _renderTarget = null;

    private static Texture2D _currentTexture;
    private static bool _textureEnabled;
    private static bool _vertexColorEnabled;

    private static readonly Dictionary<CCBlendFunc, BlendState> _blendStates = new Dictionary<CCBlendFunc, BlendState>();

    private static DepthStencilState _depthEnableStencilState;
    private static DepthStencilState _depthDisableStencilState;

    //Flags
    private static bool _worldMatrixChanged;
    private static bool _projectionMatrixChanged;
    private static bool _viewMatrixChanged;
    private static bool _textureChanged;
    private static bool _effectChanged;

    private static int _lastWidth;
    private static int _lastHeight;
    private static bool _depthTest = true;
    private static CCBlendFunc _currBlend = CCBlendFunc.AlphaBlend;
    private static RenderTarget2D _currRenderTarget;
    private static Viewport _savedViewport;
    private static CCQuadVertexBuffer _quadsBuffer;
    private static CCIndexBuffer<short> _quadsIndexBuffer;

    public static int DrawCount;

    /// <summary>
    /// Default sampler state for rendering. Set to SamplerState.PointClamp for pixel-perfect rendering.
    /// Must be set before CCDrawManager.Initialize() is called, or call it and then re-initialize.
    /// Default is SamplerState.LinearClamp.
    /// </summary>
    public static SamplerState DefaultSamplerState { get; set; } = SamplerState.LinearClamp;

    private static CCV3F_C4B_T2F[] _quadVertices;
    private static float _scaleX;
    private static float _scaleY;
    private static CCRect _viewPortRect;
    private static CCSize _screenSize;
    private static CCSize _designResolutionSize;
    private static CCResolutionPolicy _resolutionPolicy = CCResolutionPolicy.UnKnown;
    private static float _frameZoomFactor = 1.0f;
    private static DepthFormat _platformDepthFormat = DepthFormat.Depth24;
    // ref: http://www.khronos.org/registry/gles/extensions/NV/GL_NV_texture_npot_2D_mipmap.txt
    private static bool _allowNonPower2Textures = true;

    internal static CCRawList<CCV3F_C4B_T2F> _tmpVertices = new CCRawList<CCV3F_C4B_T2F>();

    private static bool _needReinitResources;

    internal static Game m_Game;

    public static bool VertexColorEnabled
    {
        get { return _vertexColorEnabled; }
        set
        {
            if (_vertexColorEnabled != value)
            {
                _vertexColorEnabled = value;
                _textureChanged = true;
            }
        }
    }

    public static bool TextureEnabled
    {
        get { return _textureEnabled; }
        set
        {
            if (_textureEnabled != value)
            {
                _textureEnabled = value;
                _textureChanged = true;
            }
        }
    }

    public static DepthStencilState DepthStencilState
    {
        get { return graphicsDevice.DepthStencilState; }
        set { graphicsDevice.DepthStencilState = value; }
    }

    public static GraphicsDevice GraphicsDevice
    {
        get { return graphicsDevice; }
    }

    public static BlendState BlendState
    {
        get { return graphicsDevice.BlendState; }
        set
        {
            graphicsDevice.BlendState = value;
            _currBlend.Source = -1;
            _currBlend.Destination = -1;
        }
    }

    public static Matrix ViewMatrix
    {
        get { return m_viewMatrix; }
        set
        {
            m_viewMatrix = value;
            _viewMatrixChanged = true;
        }
    }

    public static Matrix ProjectionMatrix
    {
        get { return m_projectionMatrix; }
        set
        {
            m_projectionMatrix = value;
            _projectionMatrixChanged = true;
        }
    }

    public static Matrix WorldMatrix
    {
        get { return m_Matrix; }
        set
        {
            m_Matrix = m_worldMatrix = value;
            _worldMatrixChanged = true;
        }
    }

    public static CCSize DesignResolutionSize
    {
        get { return _designResolutionSize; }
    }

    public static CCResolutionPolicy ResolutionPolicy
    {
        get { return _resolutionPolicy; }
    }

    public static CCSize FrameSize
    {
        get { return _screenSize; }
        set { _designResolutionSize = _screenSize = value; }
    }

    public static bool DepthTest
    {
        get { return _depthTest; }
        set
        {
            _depthTest = value;
            if (graphicsDevice != null)
            {
                // NOTE: This must be disabled when primitives are drawing, e.g. lines, polylines, etc.
                graphicsDevice.DepthStencilState = value ? _depthEnableStencilState : _depthDisableStencilState;
                //graphicsDevice.DepthStencilState = value ? DepthStencilState.Default : DepthStencilState.None;
            }
        }
    }

    public static CCSize VisibleSize
    {
        get
        {
            if (_resolutionPolicy == CCResolutionPolicy.NoBorder)
            {
                return new CCSize(_screenSize.Width / _scaleX, _screenSize.Height / _scaleY);
            }
            else
            {
                return _designResolutionSize;
            }
        }
    }

    public static CCPoint VisibleOrigin
    {
        get
        {
            if (_resolutionPolicy == CCResolutionPolicy.NoBorder)
            {
                return new CCPoint((_designResolutionSize.Width - _screenSize.Width / _scaleX) / 2,
                                   (_designResolutionSize.Height - _screenSize.Height / _scaleY) / 2);
            }
            else
            {
                return CCPoint.Zero;
            }
        }
    }

    private static List<RasterizerState> _rasterizerStatesCache = new List<RasterizerState>();

    private static RasterizerState GetScissorRasterizerState(bool scissorEnabled)
    {
        var currentState = graphicsDevice.RasterizerState;
        
        for (int i = 0; i < _rasterizerStatesCache.Count; i++)
        {
            var state = _rasterizerStatesCache[i];
            if (
                state.ScissorTestEnable == scissorEnabled &&
                currentState.CullMode == state.CullMode &&
                currentState.DepthBias == state.DepthBias &&
                currentState.FillMode == state.FillMode &&
                currentState.MultiSampleAntiAlias == state.MultiSampleAntiAlias &&
                currentState.SlopeScaleDepthBias == state.SlopeScaleDepthBias
                )
            {
                return state;
            }
        }

        var newState = new RasterizerState
        {
            ScissorTestEnable = scissorEnabled,
            CullMode = currentState.CullMode,
            DepthBias = currentState.DepthBias,
            FillMode = currentState.FillMode,
            MultiSampleAntiAlias = currentState.MultiSampleAntiAlias,
            SlopeScaleDepthBias = currentState.SlopeScaleDepthBias
        };

        _rasterizerStatesCache.Add(newState);

        return newState;
    }

    public static bool ScissorRectEnabled
    {
        get { return graphicsDevice.RasterizerState.ScissorTestEnable; }
        set
        {
            if (graphicsDevice.RasterizerState.ScissorTestEnable != value)
            {
                graphicsDevice.RasterizerState = GetScissorRasterizerState(value);
            }
        }
    }

    public static CCRect ViewPortRect
    {
        get { return _viewPortRect; }
    }

    public static float ScaleX
    {
        get { return _scaleX; }
    }

    public static float ScaleY
    {
        get { return _scaleY; }
    }

    /// <summary>
    /// Converts screen coordinates (pixels from top-left) to game/design coordinates
    /// using the current resolution policy and scale factors.
    /// </summary>
    public static CCPoint ConvertScreenToGameCoords(float screenX, float screenY)
    {
        float gameX = (screenX - _viewPortRect.Origin.X) / _scaleX;
        float gameY = (_screenSize.Height - screenY - _viewPortRect.Origin.Y) / _scaleY;
        return new CCPoint(gameX, gameY);
    }

    private static IGraphicsDeviceService _graphicsService;
    private static PresentationParameters _presentationParameters = new PresentationParameters();
    private static GraphicsDeviceManager _graphicsDeviceMgr;

    private static void UpdatePresentationParametrs(GraphicsDeviceManager manager)
    {
        var pp = _presentationParameters;

        pp.BackBufferWidth = manager.PreferredBackBufferWidth;
        pp.BackBufferHeight = manager.PreferredBackBufferHeight;
        pp.BackBufferFormat = manager.PreferredBackBufferFormat;
        pp.DepthStencilFormat = manager.PreferredDepthStencilFormat;
        pp.RenderTargetUsage = _presentationParameters.RenderTargetUsage;
        if (manager.PreferMultiSampling && pp.MultiSampleCount == 0)
        {
            pp.MultiSampleCount = 4;
        }
        else if (!manager.PreferMultiSampling)
        {
            pp.MultiSampleCount = 0;
        }
    }

    public static void InitializeDisplay(Game game, GraphicsDeviceManager graphics, DisplayOrientation supportedOrientations)
    {
        _graphicsDeviceMgr = graphics;
        _hasStencilBuffer = (graphics.PreferredDepthStencilFormat == DepthFormat.Depth24Stencil8);
        SetOrientation(supportedOrientations, false);

        m_Game = game;

#if ANDROID
        graphics.IsFullScreen = true;
#endif

#if WINDOWS || WINDOWSGL || MACOS || LINUX
        game.IsMouseVisible = true;
        graphics.IsFullScreen = false;
#endif
    }

    public static void Init(IGraphicsDeviceService service)
    {
        _graphicsService = service;
        _presentationParameters = new PresentationParameters()
        {
            RenderTargetUsage = RenderTargetUsage.PreserveContents,
            DepthStencilFormat = DepthFormat.Depth24Stencil8,
            BackBufferFormat = SurfaceFormat.Color
        };
        
        service.DeviceCreated += GraphicsDeviceDeviceCreated;

        var manager = service as GraphicsDeviceManager;

        if (manager != null)
        {
            UpdatePresentationParametrs(manager);

            manager.PreparingDeviceSettings += GraphicsPreparingDeviceSettings;
        }
        else
        {
            if (service.GraphicsDevice != null)
            {
                Init(service.GraphicsDevice);
            }
        }
    }

    /// <summary>
    /// Called just before the graphics device for the presentation is created. This method callback is used to setup
    /// the device settings. The WindowSetup is used to set the presentation parameters.
    /// </summary>
    static void GraphicsPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
    {
        var gdipp = e.GraphicsDeviceInformation.PresentationParameters;
        var pp = _presentationParameters;

        gdipp.RenderTargetUsage = pp.RenderTargetUsage;
        gdipp.DepthStencilFormat = pp.DepthStencilFormat;
        gdipp.BackBufferFormat = pp.BackBufferFormat;
        gdipp.MultiSampleCount = pp.MultiSampleCount;

        //if (graphicsDevice == null)
        {
            // Only set the buffer dimensions when the device was not created
            gdipp.BackBufferWidth = pp.BackBufferWidth;
            gdipp.BackBufferHeight = pp.BackBufferHeight;
        }
    }

    static void GraphicsDeviceDeviceCreated(object sender, EventArgs e)
    {
        Init(_graphicsService.GraphicsDevice);
    }

    public static void Init(GraphicsDevice graphicsDevice)
    {
        CCDrawManager.graphicsDevice = graphicsDevice;

        spriteBatch = new SpriteBatch(graphicsDevice);

        _defaultEffect = new BasicEffect(graphicsDevice);

        PrimitiveEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = true
        };

        AlphaTestEffect = new AlphaTestEffect(graphicsDevice);

        _depthEnableStencilState = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
            TwoSidedStencilMode = true
        };

        _depthDisableStencilState = new DepthStencilState
        {
            DepthBufferEnable = false
        };
        PresentationParameters pp = graphicsDevice.PresentationParameters;
        //pp.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        //_renderTarget = new RenderTarget2D(graphicsDevice, pp.BackBufferWidth, (int)pp.BackBufferHeight, false, pp.BackBufferFormat, pp.DepthStencilFormat, pp.MultiSampleCount, RenderTargetUsage.PreserveContents);

        //_resolutionPolicy = CCResolutionPolicy.UnKnown;
        _viewPortRect = new CCRect(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
        _screenSize = _viewPortRect.Size;
        _lastWidth = 0;
        _lastHeight = 0;

        if (_resolutionPolicy != CCResolutionPolicy.UnKnown)
        {
            SetDesignResolutionSize(_designResolutionSize.Width, _designResolutionSize.Height, _resolutionPolicy);
        }
        else
        {
            _scaleY = 1.0f;
            _scaleX = 1.0f;

            _designResolutionSize = _screenSize;
        }

        m_projectionMatrix = Matrix.Identity;
        m_viewMatrix = Matrix.Identity;
        m_worldMatrix = Matrix.Identity;
        m_Matrix = Matrix.Identity;

        _worldMatrixChanged = _viewMatrixChanged = _projectionMatrixChanged = true;

        CCDrawingPrimitives.Init(graphicsDevice);

        graphicsDevice.Disposing += GraphicsDeviceDisposing;
        graphicsDevice.DeviceLost += GraphicsDeviceDeviceLost;
        graphicsDevice.DeviceReset += GraphicsDeviceDeviceReset;
        graphicsDevice.DeviceResetting += GraphicsDeviceDeviceResetting;
        graphicsDevice.ResourceCreated += GraphicsDeviceResourceCreated;
        graphicsDevice.ResourceDestroyed += GraphicsDeviceResourceDestroyed;
    }

    public static void PurgeDrawManager()
    {
        graphicsDevice = null;

        PrimitiveEffect = null;
        AlphaTestEffect = null;

        _defaultEffect = null;
        _currentEffect = null;
        _effectStack.Clear();

        spriteBatch = null;

        _renderTarget = null;

        _currentTexture = null;

        _blendStates.Clear();

        _depthEnableStencilState = null;
        _depthDisableStencilState = null;

        _currRenderTarget = null;
        _quadsBuffer = null;
        _quadsIndexBuffer = null;

        _quadVertices = null;
        _tmpVertices.Clear();
    }
    
    static void GraphicsDeviceResourceDestroyed(object sender, ResourceDestroyedEventArgs e)
    {
    }

    static void GraphicsDeviceResourceCreated(object sender, ResourceCreatedEventArgs e)
    {
    }

    static void GraphicsDeviceDeviceResetting(object sender, EventArgs e)
    {
        CCGraphicsResource.DisposeAllResources();
        CCSpriteFontCache.SharedInstance.Clear();
#if XNA
        CCContentManager.SharedContentManager.ReloadGraphicsAssets();
#endif
        _needReinitResources = true;
    }

    static void GraphicsDeviceDeviceReset(object sender, EventArgs e)
    {
    }

    static void GraphicsDeviceDeviceLost(object sender, EventArgs e)
    {
    }

    static void GraphicsDeviceDisposing(object sender, EventArgs e)
    {
    }

    private static void ResetDevice()
    {
        _defaultEffect.View = m_viewMatrix;
        _defaultEffect.World = m_worldMatrix;
        _defaultEffect.Projection = m_projectionMatrix;

        m_Matrix = m_worldMatrix;

        _defaultEffect.Alpha = 1f;
        _defaultEffect.VertexColorEnabled = true;
        _defaultEffect.Texture = null;
        _defaultEffect.TextureEnabled = false;

        _effectStack.Clear();

        _currentEffect = _defaultEffect;

        _currentTexture = null;
        _vertexColorEnabled = true;

        _worldMatrixChanged = false;
        _projectionMatrixChanged = false;
        _viewMatrixChanged = false;
        _textureChanged = false;
        _effectChanged = false;

        graphicsDevice.SetVertexBuffer(null);
        graphicsDevice.SetRenderTarget(_renderTarget);
        graphicsDevice.Indices = null;

        graphicsDevice.SamplerStates[0] = DefaultSamplerState;
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.BlendState = BlendState.AlphaBlend;

        DepthTest = _depthTest;

        if (graphicsDevice.Viewport.Width != _lastWidth || graphicsDevice.Viewport.Height != _lastHeight)
        {
            PresentationParameters pp = graphicsDevice.PresentationParameters;
            _viewPortRect = new CCRect(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
            _screenSize = _viewPortRect.Size;

            if (_resolutionPolicy != CCResolutionPolicy.UnKnown)
            {
                SetDesignResolutionSize(_designResolutionSize.Width, _designResolutionSize.Height, _resolutionPolicy);
            }
            else
            {
                CCDirector director = CCDirector.SharedDirector;
                director.Projection = director.Projection;
            }

            _lastWidth = graphicsDevice.Viewport.Width;
            _lastHeight = graphicsDevice.Viewport.Height;
        }
    }

    private static bool _hasStencilBuffer = true;

    public static bool BeginDraw()
    {
        if (graphicsDevice == null || graphicsDevice.IsDisposed)
        {
            // We are existing the game
            return(false);
        }

        if (_needReinitResources)
        {
            CCGraphicsResource.ReinitAllResources();
            _needReinitResources = false;
        }

        ResetDevice();
        if (_hasStencilBuffer)
        {
            try
            {
                Clear(Color.Black, 0, 0);
            }
            catch (InvalidOperationException)
            {
                // no stencil buffer
                _hasStencilBuffer = false;
                Clear(Color.Black);
            }
        }
        else
        {
            Clear(Color.Black);
        }
        DrawCount = 0;
        return (true);
    }

    public static void EndDraw()
    {
        if (graphicsDevice == null || graphicsDevice.IsDisposed)
        {
            // We are existing the game
            return;
        }

        Debug.Assert(_stackIndex == 0);

        if (_renderTarget != null)
        {
            graphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(_renderTarget, new Vector2(0, 0), Color.White);
            spriteBatch.End();
        }

        ResetDevice();
    }

    public static void PushEffect(Effect effect)
    {
        _effectStack.Push(_currentEffect);
        _currentEffect = effect;
        _effectChanged = true;
    }

    public static void PopEffect()
    {
        _currentEffect = _effectStack.Pop();
        _effectChanged = true;
    }

    private static void ApplyEffectTexture()
    {
        if (_currentEffect is BasicEffect)
        {
            var effect = (BasicEffect)_currentEffect;

            effect.TextureEnabled = _textureEnabled;
            effect.VertexColorEnabled = _vertexColorEnabled;
            effect.Texture = _currentTexture;
        }
        else if (_currentEffect is AlphaTestEffect)
        {
            var effect = (AlphaTestEffect)_currentEffect;
            effect.VertexColorEnabled = _vertexColorEnabled;
            effect.Texture = _currentTexture;
        }
        else
        {
            throw new Exception(String.Format("Effect {0} not supported", _currentEffect.GetType().Name));
        }
    }

    private static void ApplyEffectParams()
    {
        if (_effectChanged)
        {
            var matrices = _currentEffect as IEffectMatrices;

            if (matrices != null)
            {
                matrices.Projection = m_projectionMatrix;
                matrices.View = m_viewMatrix;
                matrices.World = m_Matrix;
            }

            ApplyEffectTexture();
        }
        else
        {
            if (_worldMatrixChanged || _projectionMatrixChanged || _viewMatrixChanged)
            {
                var matrices = _currentEffect as IEffectMatrices;

                if (matrices != null)
                {
                    if (_worldMatrixChanged)
                    {
                        matrices.World = m_Matrix;
                    }
                    if (_projectionMatrixChanged)
                    {
                        matrices.Projection = m_projectionMatrix;
                    }
                    if (_viewMatrixChanged)
                    {
                        matrices.View = m_viewMatrix;
                    }
                }
            }

            if (_textureChanged)
            {
                ApplyEffectTexture();
            }
        }

        _effectChanged = false;
        _textureChanged = false;
        _worldMatrixChanged = false;
        _projectionMatrixChanged = false;
        _viewMatrixChanged = false;
    }

    public static void DrawPrimitives<T>(PrimitiveType type, T[] vertices, int offset, int count) where T : struct, IVertexType
    {
        if (count <= 0)
        {
            return;
        }

        ApplyEffectParams();

        EffectPassCollection passes = _currentEffect.CurrentTechnique.Passes;
        for (int i = 0; i < passes.Count; i++)
        {
            passes[i].Apply();
            if (count > 65535)
            {
                count = 65535; // Hard limit for XNA
            }
            graphicsDevice.DrawUserPrimitives(type, vertices, offset, count);
        }

        DrawCount++;
    }

    public static void DrawIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData,
                                                int indexOffset, int primitiveCount) where T : struct, IVertexType
    {
        if (primitiveCount <= 0)
        {
            return;
        }

        ApplyEffectParams();

        EffectPassCollection passes = _currentEffect.CurrentTechnique.Passes;
        for (int i = 0; i < passes.Count; i++)
        {
            passes[i].Apply();
            graphicsDevice.DrawUserIndexedPrimitives(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset,
                                                     primitiveCount);
        }

        DrawCount++;
    }


    public static void BlendFunc(CCBlendFunc blendFunc)
    {

        // It looks like the blend state is being reset somewhere so this check of not setting
        // the blend states is causing multiple problems.  Took this check out and setting the 
        // blend state seems the correct modification for now.
        //if (_currBlend.Destination != blendFunc.Destination || _currBlend.Source != blendFunc.Source)
        //{
        BlendState bs = null;
        if (blendFunc == CCBlendFunc.AlphaBlend)
        {
            bs = BlendState.AlphaBlend;
        }
        else if (blendFunc == CCBlendFunc.Additive)
        {
            bs = BlendState.Additive;
        }
        else if (blendFunc == CCBlendFunc.NonPremultiplied)
        {
            bs = BlendState.NonPremultiplied;
        }
        else if (blendFunc == CCBlendFunc.Opaque)
        {
            bs = BlendState.Opaque;
        }
        else
        {
            if (!_blendStates.TryGetValue(blendFunc, out bs))
            {
                bs = new BlendState();

                bs.ColorSourceBlend = CCOGLES.GetXNABlend(blendFunc.Source);
                bs.AlphaSourceBlend = CCOGLES.GetXNABlend(blendFunc.Source);
                bs.ColorDestinationBlend = CCOGLES.GetXNABlend(blendFunc.Destination);
                bs.AlphaDestinationBlend = CCOGLES.GetXNABlend(blendFunc.Destination);

                _blendStates.Add(blendFunc, bs);
            }
        }

        if (graphicsDevice != null)
        {
            graphicsDevice.BlendState = bs;
        }

        _currBlend.Source = blendFunc.Source;
        _currBlend.Destination = blendFunc.Destination;

        //}
    }

    public static void BindTexture(CCTexture2D texture)
    {
        Texture2D tex = texture == null ? null : texture.XNATexture;

        if (!graphicsDevice.IsDisposed && graphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Normal)
        {
            if (tex == null)
            {
                graphicsDevice.SamplerStates[0] = DefaultSamplerState;
                TextureEnabled = false;
            }
            else
            {
                graphicsDevice.SamplerStates[0] = texture.SamplerState;
                TextureEnabled = true;
            }

            if (_currentTexture != tex)
            {
                _currentTexture = tex;
                _textureChanged = true;
            }
        }
    }

    public static void CreateRenderTarget(CCTexture2D pTexture, RenderTargetUsage usage)
    {
        CCSize size = pTexture.ContentSizeInPixels;
        var texture = CreateRenderTarget((int)size.Width, (int)size.Height, CCTexture2D.DefaultAlphaPixelFormat,
                                         _platformDepthFormat, usage);
        pTexture.InitWithTexture(texture, CCTexture2D.DefaultAlphaPixelFormat, true, false);
    }

    public static RenderTarget2D CreateRenderTarget(int width, int height, RenderTargetUsage usage)
    {
        return CreateRenderTarget(width, height, CCTexture2D.DefaultAlphaPixelFormat, DepthFormat.None, usage);
    }

    public static RenderTarget2D CreateRenderTarget(int width, int height, SurfaceFormat colorFormat, RenderTargetUsage usage)
    {
        return CreateRenderTarget(width, height, colorFormat, DepthFormat.None, usage);
    }

    public static RenderTarget2D CreateRenderTarget(int width, int height, SurfaceFormat colorFormat, DepthFormat depthFormat,
                                                    RenderTargetUsage usage)
    {
        if (!_allowNonPower2Textures)
        {
            width = CCUtils.CCNextPOT(width);
            height = CCUtils.CCNextPOT(height);
        }
        return new RenderTarget2D(graphicsDevice, width, height, false, colorFormat, depthFormat, 0, usage);
    }

    public static Texture2D CreateTexture2D(int width, int height)
    {
        PresentationParameters pp = graphicsDevice.PresentationParameters;
        if (!_allowNonPower2Textures)
        {
            width = CCUtils.CCNextPOT(width);
            height = CCUtils.CCNextPOT(height);
        }
        return new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
    }

    /// <summary>
    /// Sets the render target for all drawing to the given texture's core texture. The texture
    /// must be a RenderTarget2D or else you will get an assert/exception.
    /// </summary>
    /// <param name="pTexture">The target of future drawing, which must be a RenderTarget2D</param>
    public static void SetRenderTarget(CCTexture2D pTexture)
    {
        if (pTexture == null)
        {
            SetRenderTarget((RenderTarget2D)null);
        }
        else
        {
            Debug.Assert(pTexture.XNATexture is RenderTarget2D);
            SetRenderTarget((RenderTarget2D)pTexture.XNATexture);
        }
    }

    /// <summary>
    /// Resets the default render target to be the display render target (null)
    /// </summary>
    public static void ResetToDisplayRenderTarget()
    {
        if (graphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Normal)
        {
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Viewport = _savedViewport;
        }
        _currRenderTarget = null;
    }

    /// <summary>
    /// Sets the render target to the given target. This is where your drawing will happen 
    /// if the given parameter is not null.
    /// </summary>
    /// <param name="renderTarget"></param>
    public static void SetRenderTarget(RenderTarget2D renderTarget)
    {
        if (graphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Normal)
        {
            if (renderTarget == null)
            {
                graphicsDevice.SetRenderTarget(_renderTarget);
                graphicsDevice.Viewport = _savedViewport;
            }
            else
            {
                _savedViewport = graphicsDevice.Viewport;
                graphicsDevice.SetRenderTarget(renderTarget);
            }
        }
        _currRenderTarget = renderTarget;
    }

    /// <summary>
    /// Returns the current render target. If this is null, then your drawing
    /// is set to draw directly to the display frame buffer.
    /// </summary>
    /// <returns></returns>
    public static RenderTarget2D GetRenderTarget()
    {
        return _currRenderTarget;
    }

    #region Quad Buffer Integrity Checks

    private static void CheckQuadsIndexBuffer(int capacity)
    {
        if (_quadsIndexBuffer == null || _quadsIndexBuffer.Capacity < capacity * 6)
        {
            capacity = Math.Max(capacity, DefaultQuadBufferSize);

            if (_quadsIndexBuffer == null)
            {
                _quadsIndexBuffer = new CCIndexBuffer<short>(capacity * 6, BufferUsage.WriteOnly);
                _quadsIndexBuffer.Count = _quadsIndexBuffer.Capacity;
            }

            if (_quadsIndexBuffer.Capacity < capacity * 6)
            {
                _quadsIndexBuffer.Capacity = capacity * 6;
                _quadsIndexBuffer.Count = _quadsIndexBuffer.Capacity;
            }

            var indices = _quadsIndexBuffer.Data.Elements;

            int i6 = 0;
            int i4 = 0;

            for (int i = 0; i < capacity; ++i)
            {
                indices[i6 + 0] = (short)(i4 + 0);
                indices[i6 + 1] = (short)(i4 + 2);
                indices[i6 + 2] = (short)(i4 + 1);

                indices[i6 + 3] = (short)(i4 + 1);
                indices[i6 + 4] = (short)(i4 + 2);
                indices[i6 + 5] = (short)(i4 + 3);

                i6 += 6;
                i4 += 4;
            }

            _quadsIndexBuffer.UpdateBuffer();
        }
    }

    private static void CheckQuadsVertexBuffer(int capacity)
    {
        if (_quadsBuffer == null || _quadsBuffer.Capacity < capacity)
        {
            capacity = Math.Max(capacity, DefaultQuadBufferSize);

            if (_quadsBuffer == null)
            {
                _quadsBuffer = new CCQuadVertexBuffer(capacity, BufferUsage.WriteOnly);
            }
            else
            {
                _quadsBuffer.Capacity = capacity;
            }
        }
    }

    #endregion

    #region Drawing Vertices and Quads

    public static void DrawQuad(ref CCV3F_C4B_T2F_Quad quad)
    {
        CCV3F_C4B_T2F[] vertices = _quadVertices;

        if (vertices == null)
        {
            vertices = _quadVertices = new CCV3F_C4B_T2F[4];
            CheckQuadsIndexBuffer(1);
        }

        vertices[0] = quad.TopLeft;
        vertices[1] = quad.BottomLeft;
        vertices[2] = quad.TopRight;
        vertices[3] = quad.BottomRight;

        DrawIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, _quadsIndexBuffer.Data.Elements, 0, 2);
    }

    public static void DrawQuads(CCRawList<CCV3F_C4B_T2F_Quad> quads, int start, int n)
    {
        if (n == 0)
        {
            return;
        }

        CheckQuadsIndexBuffer(start + n);
        CheckQuadsVertexBuffer(start + n);

        _quadsBuffer.UpdateBuffer(quads, start, n);

        graphicsDevice.SetVertexBuffer(_quadsBuffer.VertexBuffer);
        graphicsDevice.Indices = _quadsIndexBuffer.IndexBuffer;

        ApplyEffectParams();

        EffectPassCollection passes = _currentEffect.CurrentTechnique.Passes;
        for (int i = 0; i < passes.Count; i++)
        {
            passes[i].Apply();
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, start * 6, n * 2);
        }

        graphicsDevice.SetVertexBuffer(null);
        graphicsDevice.Indices = null;

        DrawCount++;
    }

    public static void DrawBuffer<T, T2>(CCVertexBuffer<T> vertexBuffer, CCIndexBuffer<T2> indexBuffer, int start, int count)
        where T : struct, IVertexType
        where T2 : struct
    {
        graphicsDevice.Indices = indexBuffer.IndexBuffer;
        graphicsDevice.SetVertexBuffer(vertexBuffer.VertexBuffer);

        ApplyEffectParams();

        EffectPassCollection passes = _currentEffect.CurrentTechnique.Passes;
        for (int i = 0; i < passes.Count; i++)
        {
            passes[i].Apply();
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, start, count);
        }

        graphicsDevice.SetVertexBuffer(null);
        graphicsDevice.Indices = null;

        DrawCount++;
    }

    public static void DrawQuadsBuffer<T>(CCVertexBuffer<T> vertexBuffer, int start, int n) where T : struct, IVertexType
    {
        if (n == 0)         {
            return;
        }

        CheckQuadsIndexBuffer(start + n);

        graphicsDevice.Indices = _quadsIndexBuffer.IndexBuffer;
        graphicsDevice.SetVertexBuffer(vertexBuffer.VertexBuffer);

        ApplyEffectParams();

        EffectPassCollection passes = _currentEffect.CurrentTechnique.Passes;
        for (int i = 0; i < passes.Count; i++)
        {
            passes[i].Apply();
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, start * 6, n * 2);
        }

        graphicsDevice.SetVertexBuffer(null);
        graphicsDevice.Indices = null;

        DrawCount++;
    }

    #endregion

    #region Blanking The Display

    public static void Clear(ClearOptions options, Color color, float depth, int stencil)
    {
        graphicsDevice.Clear(options, color, depth, stencil);
    }

    public static void Clear(Color color, float depth, int stencil)
    {
        graphicsDevice.Clear(ClearOptions.Target | ClearOptions.Stencil | ClearOptions.DepthBuffer, color, depth, stencil);
    }

    public static void Clear(Color color, float depth)
    {
        graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, color, depth, 0);
    }

    public static void Clear(Color color)
    {
        graphicsDevice.Clear(color);
    }

    #endregion

    /// <summary>
    /// Set zoom factor for frame. This method is for debugging big resolution (e.g.new ipad) app on
    /// desktop.
    /// </summary>
    public static void SetFrameZoom(float zoomFactor)
    {
        _frameZoomFactor = zoomFactor;
    }

    public static void SetViewPort(int x, int y, int width, int height)
    {
        graphicsDevice.Viewport = new Viewport(x, y, width, height);
    }

    public static void SetViewPortInPoints(int x, int y, int width, int height)
    {
        graphicsDevice.Viewport = new Viewport(
            (int)(x * _scaleX * _frameZoomFactor + _viewPortRect.Origin.X * _frameZoomFactor),
            (int)(y * _scaleY * _frameZoomFactor + _viewPortRect.Origin.Y * _frameZoomFactor),
            (int)(width * _scaleX * _frameZoomFactor),
            (int)(height * _scaleY * _frameZoomFactor)
            );
    }

    public static void SetScissorInPoints(float x, float y, float w, float h)
    {
        y = CCDirector.SharedDirector.WinSize.Height - y - h;

        graphicsDevice.ScissorRectangle = new Rectangle(
            (int)(x * _scaleX + _viewPortRect.Origin.X),
            (int)(y * _scaleY + _viewPortRect.Origin.Y),
            (int)(w * _scaleX),
            (int)(h * _scaleY)
            );
    }

    public static CCRect ScissorRect
    {
        get
        {
            var sr = graphicsDevice.ScissorRectangle;

            float x = (sr.X - _viewPortRect.Origin.X) / _scaleX;
            float y = (sr.Y - _viewPortRect.Origin.Y) / _scaleY;
            float w = sr.Width / _scaleX;
            float h = sr.Height / _scaleY;

            y = CCDirector.SharedDirector.WinSize.Height - y - h;

            return new CCRect(x, y, w, h);
        }
    }

    public static void SetDesignResolutionSize(float width, float height, CCResolutionPolicy resolutionPolicy)
    {
        Debug.Assert(resolutionPolicy != CCResolutionPolicy.UnKnown, "should set resolutionPolicy");

        if (width == 0.0f || height == 0.0f)
        {
            return;
        }

        _designResolutionSize.Width = width;
        _designResolutionSize.Height = height;

        _scaleX = _screenSize.Width / _designResolutionSize.Width;
        _scaleY = _screenSize.Height / _designResolutionSize.Height;

        if (resolutionPolicy == CCResolutionPolicy.NoBorder)
        {
            _scaleX = _scaleY = Math.Max(_scaleX, _scaleY);
        }

        if (resolutionPolicy == CCResolutionPolicy.ShowAll)
        {
            _scaleX = _scaleY = Math.Min(_scaleX, _scaleY);
        }


        if (resolutionPolicy == CCResolutionPolicy.FixedHeight)
        {
            _scaleX = _scaleY;
            _designResolutionSize.Width = (float)Math.Ceiling(_screenSize.Width / _scaleX);
        }

        if (resolutionPolicy == CCResolutionPolicy.FixedWidth)
        {
            _scaleY = _scaleX;
            _designResolutionSize.Height = (float)Math.Ceiling(_screenSize.Height / _scaleY);
        }

        // calculate the rect of viewport    
        float viewPortW = _designResolutionSize.Width * _scaleX;
        float viewPortH = _designResolutionSize.Height * _scaleY;

        var clientBoundsX = 0;
#if ANDROID
        // When using CCGameView, m_Game may not be set
        if (m_Game != null && m_Game.Window != null)
        {
            clientBoundsX = ((AndroidGameWindow)m_Game.Window).ClientBounds.X;
        }
#endif

        _viewPortRect = new CCRect(clientBoundsX + (_screenSize.Width - viewPortW) / 2, (_screenSize.Height - viewPortH) / 2, viewPortW, viewPortH);

        _resolutionPolicy = resolutionPolicy;

        // reset director's member variables to fit visible rect
        CCDirector.SharedDirector.m_obWinSizeInPoints = DesignResolutionSize;
        if (CCConfiguration.SharedConfiguration.DisplayStats)
        {
            CCDirector.SharedDirector.CreateStatsLabel();
        }
        CCDirector.SharedDirector.SetRenderDefaultValues();
    }

    public static void SetOrientation(DisplayOrientation supportedOrientations)
    {
        SetOrientation(supportedOrientations, true);
    }

    private static void SetOrientation(DisplayOrientation supportedOrientations, bool bUpdateDimensions)
    {
        bool ll = (supportedOrientations & DisplayOrientation.LandscapeLeft) == DisplayOrientation.LandscapeLeft;
        bool lr = (supportedOrientations & DisplayOrientation.LandscapeRight) == DisplayOrientation.LandscapeRight;
        bool p = (supportedOrientations & DisplayOrientation.Portrait) == DisplayOrientation.Portrait;

        bool onlyLandscape = (ll || lr) && !p;
#if WINDOWS || WINDOWSGL || MACOS || LINUX
        bool bSwapDims = bUpdateDimensions && ((_graphicsDeviceMgr.SupportedOrientations & supportedOrientations) == DisplayOrientation.Default);
#else
        bool bSwapDims = bUpdateDimensions && ((_graphicsDeviceMgr.SupportedOrientations & supportedOrientations) == 0);
#endif
        if (bSwapDims && (ll || lr))
        {
            // Check for landscape changes that do not need a swap
#if WINDOWS || WINDOWSGL || MACOS || LINUX
            if (((_graphicsDeviceMgr.SupportedOrientations & DisplayOrientation.LandscapeLeft) != DisplayOrientation.Default) ||
                ((_graphicsDeviceMgr.SupportedOrientations & DisplayOrientation.LandscapeRight) != DisplayOrientation.Default))
#else
            if (((_graphicsDeviceMgr.SupportedOrientations & DisplayOrientation.LandscapeLeft) != 0) ||
                ((_graphicsDeviceMgr.SupportedOrientations & DisplayOrientation.LandscapeRight) != 0))
#endif
            {
                bSwapDims = false;
            }
        }
        int preferredBackBufferWidth = _graphicsDeviceMgr.PreferredBackBufferWidth;
        int preferredBackBufferHeight = _graphicsDeviceMgr.PreferredBackBufferHeight;
        if (bSwapDims)
        {
            CCSize newSize = _designResolutionSize.Inverted;
            CCDrawManager.SetDesignResolutionSize(newSize.Width, newSize.Height, _resolutionPolicy);
            /*
            _viewPortRect = _viewPortRect.InvertedSize;
            _designResolutionSize = _designResolutionSize.Inverted;
            CCDirector.SharedDirector.m_obWinSizeInPoints = CCDirector.SharedDirector.m_obWinSizeInPoints.Inverted;
            CCDirector.SharedDirector.m_obWinSizeInPixels = CCDirector.SharedDirector.m_obWinSizeInPixels.Inverted;
            _screenSize = _screenSize.Inverted;
            float f = _scaleX;
            _scaleX = _scaleY;
            _scaleY = f;
             */
        }
        preferredBackBufferWidth = _graphicsDeviceMgr.PreferredBackBufferWidth;
        preferredBackBufferHeight = _graphicsDeviceMgr.PreferredBackBufferHeight;
#if ANDROID
        if (onlyLandscape && _graphicsDeviceMgr.PreferredBackBufferHeight > _graphicsDeviceMgr.PreferredBackBufferWidth)
        {
            _graphicsDeviceMgr.PreferredBackBufferWidth = preferredBackBufferHeight;
            _graphicsDeviceMgr.PreferredBackBufferHeight = preferredBackBufferWidth;
        }
        _graphicsDeviceMgr.SupportedOrientations = supportedOrientations;
#endif

#if IOS || IPHONE
        if (bSwapDims)
        {
            _graphicsDeviceMgr.PreferredBackBufferWidth = preferredBackBufferHeight;
            _graphicsDeviceMgr.PreferredBackBufferHeight = preferredBackBufferWidth;
        }
        else if (onlyLandscape && _graphicsDeviceMgr.PreferredBackBufferHeight > _graphicsDeviceMgr.PreferredBackBufferWidth)
        {
            _graphicsDeviceMgr.PreferredBackBufferWidth = preferredBackBufferHeight;
            _graphicsDeviceMgr.PreferredBackBufferHeight = preferredBackBufferWidth;
        }
        _graphicsDeviceMgr.SupportedOrientations = supportedOrientations;
#endif
#if WINDOWS || WINDOWSGL || MACOS || LINUX
        if (bSwapDims)
        {
            _graphicsDeviceMgr.PreferredBackBufferWidth = preferredBackBufferHeight;
            _graphicsDeviceMgr.PreferredBackBufferHeight = preferredBackBufferWidth;
        }
        else
        {
            /*
            if (onlyPortrait)
            {
                _graphicsDeviceMgr.PreferredBackBufferWidth = 480;
                _graphicsDeviceMgr.PreferredBackBufferHeight = 800;
            }
            else
            {
                _graphicsDeviceMgr.PreferredBackBufferWidth = 800;
                _graphicsDeviceMgr.PreferredBackBufferHeight = 480;
            }
            */
        }
#endif
        UpdatePresentationParametrs(_graphicsDeviceMgr);

        _graphicsDeviceMgr.ApplyChanges();
    }

    public static CCPoint ScreenToWorld(float x, float y)
    {
        return new CCPoint(
            (x - _viewPortRect.MinX) / _scaleX,
            (y - _viewPortRect.MinY) / _scaleY
            );
    }

    #region Matrix

    private static Matrix _transform = Matrix.Identity;

    public static void SetIdentityMatrix()
    {
        m_Matrix = Matrix.Identity;
        _worldMatrixChanged = true;
    }

    public static void PushMatrix()
    {
        _matrixStack[_stackIndex++] = m_Matrix;
    }

    public static void PopMatrix()
    {
        m_Matrix = _matrixStack[--_stackIndex];
        _worldMatrixChanged = true;
        Debug.Assert(_stackIndex >= 0);
    }

    public static void Translate(float x, float y, int z)
    {
        _tmpMatrix = Matrix.CreateTranslation(x, y, z);
        Matrix.Multiply(ref _tmpMatrix, ref m_Matrix, out m_Matrix);
        _worldMatrixChanged = true;
    }

    public static void MultMatrix(ref Matrix matrix)
    {
        Matrix.Multiply(ref matrix, ref m_Matrix, out m_Matrix);
        _worldMatrixChanged = true;
    }

    //protected Matrix m_tCCNodeTransform;

    // | m[0] m[4] m[8]  m[12] |     | m11 m21 m31 m41 |     | a c 0 tx |
    // | m[1] m[5] m[9]  m[13] |     | m12 m22 m32 m42 |     | b d 0 ty |
    // | m[2] m[6] m[10] m[14] | <=> | m13 m23 m33 m43 | <=> | 0 0 1  0 |
    // | m[3] m[7] m[11] m[15] |     | m14 m24 m34 m44 |     | 0 0 0  1 |        
    public static void MultMatrix(CCAffineTransform transform, float z)
    {
        MultMatrix(ref transform, z);
    }

    public static void MultMatrix(ref CCAffineTransform transform, float z)
    {
        _transform.M11 = transform.a;
        _transform.M21 = transform.c;
        _transform.M12 = transform.b;
        _transform.M22 = transform.d;
        _transform.M41 = transform.tx;
        _transform.M42 = transform.ty;
        _transform.M43 = z;

        Matrix.Multiply(ref _transform, ref m_Matrix, out m_Matrix);

        _worldMatrixChanged = true;
    }

    #endregion

    #region Mask

    private struct MaskState
    {
        public int Layer;
        public bool Inverted;
        public float AlphaTreshold;
    }

    private struct MaskDepthStencilStateCacheEntry
    {
        public DepthStencilState Clear;
        public DepthStencilState ClearInvert;
        public DepthStencilState DrawMask;
        public DepthStencilState DrawMaskInvert;
        public DepthStencilState DrawContent;
        public DepthStencilState DrawContentDepth;

        public DepthStencilState GetClearState(int layer, bool inverted)
        {
            DepthStencilState result = inverted ? ClearInvert : Clear;

            if (result == null)
            {
                int maskLayer = 1 << layer;

                result = new DepthStencilState()
                {
                    DepthBufferEnable = false,

                    StencilEnable = true,

                    StencilFunction = CompareFunction.Never,

                    StencilMask = maskLayer,
                    StencilWriteMask = maskLayer,
                    ReferenceStencil = maskLayer,

                    StencilFail = !inverted ? StencilOperation.Zero : StencilOperation.Replace
                };

                if (inverted)
                {
                    ClearInvert = result;
                }
                else
                {
                    Clear = result;
                }
            }

            return result;
        }

        public DepthStencilState GetDrawMaskState(int layer, bool inverted)
        {
            DepthStencilState result = inverted ? DrawMaskInvert : DrawMask;

            if (result == null)
            {
                int maskLayer = 1 << layer;

                result = new DepthStencilState()
                {
                    DepthBufferEnable = false,

                    StencilEnable = true,

                    StencilFunction = CompareFunction.Never,

                    StencilMask = maskLayer,
                    StencilWriteMask = maskLayer,
                    ReferenceStencil = maskLayer,

                    StencilFail = !inverted ? StencilOperation.Replace : StencilOperation.Zero,
                };

                if (inverted)
                {
                    DrawMaskInvert = result;
                }
                else
                {
                    DrawMask = result;
                }
            }

            return result;
        }

        public DepthStencilState GetDrawContentState(int layer, bool depth)
        {
            DepthStencilState result = depth ? DrawContentDepth : DrawContent;

            if (result == null)
            {
                int maskLayer = 1 << layer;
                int maskLayerL = maskLayer - 1;
                int maskLayerLe = maskLayer | maskLayerL;

                result = new DepthStencilState()
                {
                    DepthBufferEnable = _maskSavedStencilStates[_maskLayer].DepthBufferEnable,

                    StencilEnable = true,

                    StencilMask = maskLayerLe,
                    StencilWriteMask = 0,
                    ReferenceStencil = maskLayerLe,

                    StencilFunction = CompareFunction.Equal,

                    StencilPass = StencilOperation.Keep,
                    StencilFail = StencilOperation.Keep,
                };

                if (depth)
                {
                    DrawContentDepth = result;
                }
                else
                {
                    DrawContent = result;
                }
            }

            return result;
        }
    }

    private static int _maskLayer = -1;
    private static bool _maskOnceLog = false;
    private static DepthStencilState[] _maskSavedStencilStates = new DepthStencilState[8];
    private static MaskState[] _maskStates = new MaskState[8];
    private static MaskDepthStencilStateCacheEntry[] _maskStatesCache = new MaskDepthStencilStateCacheEntry[8];

    public static void SetClearMaskState(int layer, bool inverted)
    {
        DepthStencilState = _maskStatesCache[layer].GetClearState(layer, inverted);
    }

    public static void SetDrawMaskState(int layer, bool inverted)
    {
        DepthStencilState = _maskStatesCache[layer].GetDrawMaskState(layer, inverted);
    }

    public static void SetDrawMaskedState(int layer, bool depth)
    {
        DepthStencilState = _maskStatesCache[layer].GetDrawContentState(layer, depth);
    }

    public static bool BeginDrawMask()
    {
        return BeginDrawMask(false, 1f);
    }

    public static bool BeginDrawMask(bool inverted)
    {
        return BeginDrawMask(inverted, 1f);
    }

    public static bool BeginDrawMask(float alphaTreshold)
    {
        return BeginDrawMask(false, alphaTreshold);
    }

    public static bool BeginDrawMask(bool inverted, float alphaTreshold)
    {
        if (_maskLayer + 1 == 8) //DepthFormat.Depth24Stencil8
        {
            if (_maskOnceLog)
            {
                CCLog.Log(
                    @"Nesting more than 8 stencils is not supported. 
                        Everything will be drawn without stencil for this node and its childs."
                    );
                _maskOnceLog = false;
            }
            return false;
        }

        _maskLayer++;

        var maskState = new MaskState() { Layer = _maskLayer, Inverted = inverted, AlphaTreshold = alphaTreshold };

        _maskStates[_maskLayer] = maskState;
        _maskSavedStencilStates[_maskLayer] = DepthStencilState;

        int maskLayer = 1 << _maskLayer;

        ///////////////////////////////////
        // CLEAR STENCIL BUFFER

        SetClearMaskState(_maskLayer, maskState.Inverted);

        // draw a fullscreen solid rectangle to clear the stencil buffer
        var size = CCDirector.SharedDirector.WinSize;

        PushMatrix();
        SetIdentityMatrix();

        CCDrawingPrimitives.Begin();
        CCDrawingPrimitives.DrawSolidRect(CCPoint.Zero, new CCPoint(size.Width, size.Height), new CCColor4B(255, 255, 255, 255));
        CCDrawingPrimitives.End();

        PopMatrix();
        
        ///////////////////////////////////
        // PREPARE TO DRAW MASK

        SetDrawMaskState(_maskLayer, maskState.Inverted);

        if (maskState.AlphaTreshold < 1f)
        {
            AlphaTestEffect.AlphaFunction = CompareFunction.Greater;
            AlphaTestEffect.ReferenceAlpha = (byte)(255 * maskState.AlphaTreshold);

            PushEffect(AlphaTestEffect);
        }

        return true;
    }

    public static void EndDrawMask()
    {
        var maskState = _maskStates[_maskLayer];

        ///////////////////////////////////
        // PREPARE TO DRAW MASKED CONTENT

        if (maskState.AlphaTreshold < 1)
        {
            PopEffect();
        }

        SetDrawMaskedState(_maskLayer, _maskSavedStencilStates[_maskLayer].DepthBufferEnable);
    }

    public static void EndMask()
    {
        ///////////////////////////////////
        // RESTORE STATE

        DepthStencilState = _maskSavedStencilStates[_maskLayer];

        _maskLayer--;
    }

    #endregion

    #region State Save/Restore for Multi-View Support

    /// <summary>
    /// Saves the current drawing state to a CCDrawManagerState object.
    /// Used for multi-view support where each view maintains its own state.
    /// </summary>
    public static CCDrawManagerState SaveState()
    {
        var state = new CCDrawManagerState();

        state.WorldMatrix = m_worldMatrix;
        state.ViewMatrix = m_viewMatrix;
        state.ProjectionMatrix = m_projectionMatrix;
        state.CombinedMatrix = m_Matrix;
        state.StackIndex = _stackIndex;

        // Copy matrix stack (valid entries are [0, _stackIndex))
        for (int i = 0; i < _stackIndex && i < state.MatrixStack.Length; i++)
        {
            state.MatrixStack[i] = _matrixStack[i];
        }

        state.DesignResolutionSize = _designResolutionSize;
        state.ScreenSize = _screenSize;
        state.ViewPortRect = _viewPortRect;
        state.ScaleX = _scaleX;
        state.ScaleY = _scaleY;
        state.ResolutionPolicy = _resolutionPolicy;
        state.DepthTest = _depthTest;

        if (graphicsDevice != null)
        {
            state.Viewport = graphicsDevice.Viewport;
        }

        return state;
    }

    /// <summary>
    /// Restores drawing state from a CCDrawManagerState object.
    /// Used for multi-view support where each view maintains its own state.
    /// </summary>
    public static void RestoreState(CCDrawManagerState state)
    {
        if (state == null)
            return;

        m_worldMatrix = state.WorldMatrix;
        m_viewMatrix = state.ViewMatrix;
        m_projectionMatrix = state.ProjectionMatrix;
        m_Matrix = state.CombinedMatrix;
        _stackIndex = state.StackIndex;

        // Restore matrix stack (valid entries are [0, StackIndex))
        for (int i = 0; i < state.StackIndex && i < _matrixStack.Length; i++)
        {
            _matrixStack[i] = state.MatrixStack[i];
        }

        _designResolutionSize = state.DesignResolutionSize;
        _screenSize = state.ScreenSize;
        _viewPortRect = state.ViewPortRect;
        _scaleX = state.ScaleX;
        _scaleY = state.ScaleY;
        _resolutionPolicy = state.ResolutionPolicy;
        _depthTest = state.DepthTest;

        if (graphicsDevice != null && state.Viewport.Width > 0 && state.Viewport.Height > 0)
        {
            graphicsDevice.Viewport = state.Viewport;
        }

        // Mark matrices as changed so they get recalculated
        _worldMatrixChanged = true;
        _viewMatrixChanged = true;
        _projectionMatrixChanged = true;
    }

    /// <summary>
    /// Creates a new state with default values for a view with the given size.
    /// </summary>
    public static CCDrawManagerState CreateStateForView(int width, int height, CCSize designResolution, CCResolutionPolicy policy)
    {
        var state = new CCDrawManagerState();

        state.ScreenSize = new CCSize(width, height);
        state.DesignResolutionSize = designResolution;
        state.ResolutionPolicy = policy;
        state.ViewPortRect = new CCRect(0, 0, width, height);
        state.Viewport = new Viewport(0, 0, width, height);

        // Guard against zero design resolution to avoid division by zero
        if (designResolution.Width == 0 || designResolution.Height == 0)
            return state;

        // Calculate scale based on policy
        float scaleX = width / designResolution.Width;
        float scaleY = height / designResolution.Height;

        switch (policy)
        {
            case CCResolutionPolicy.NoBorder:
                scaleX = scaleY = Math.Max(scaleX, scaleY);
                break;
            case CCResolutionPolicy.ShowAll:
                scaleX = scaleY = Math.Min(scaleX, scaleY);
                break;
            case CCResolutionPolicy.FixedHeight:
                scaleX = scaleY;
                state.DesignResolutionSize = new CCSize((float)Math.Ceiling(width / scaleX), designResolution.Height);
                break;
            case CCResolutionPolicy.FixedWidth:
                scaleY = scaleX;
                state.DesignResolutionSize = new CCSize(designResolution.Width, (float)Math.Ceiling(height / scaleY));
                break;
        }

        state.ScaleX = scaleX;
        state.ScaleY = scaleY;

        return state;
    }

    #endregion
}

public enum CCResolutionPolicy
{
    UnKnown,

    // The entire application is visible in the specified area without trying to preserve the original aspect ratio. 
    // Distortion can occur, and the application may appear stretched or compressed.
    ExactFit,
    // The entire application fills the specified area, without distortion but possibly with some cropping, 
    // while maintaining the original aspect ratio of the application.
    NoBorder,
    // The entire application is visible in the specified area without distortion while maintaining the original 
    // aspect ratio of the application. Borders can appear on two sides of the application.
    ShowAll,
    // The application takes the height of the design resolution size and modifies the width of the internal
    // canvas so that it fits the aspect ratio of the device
    // no distortion will occur however you must make sure your application works on different
    // aspect ratios
    FixedHeight,
    // The application takes the width of the design resolution size and modifies the height of the internal
    // canvas so that it fits the aspect ratio of the device
    // no distortion will occur however you must make sure your application works on different
    // aspect ratios
    FixedWidth
}


public class CCGraphicsResource : IDisposable
{
    private static CCRawList<WeakReference> _createdResources = new CCRawList<WeakReference>();

    private bool _isDisposed;

    private WeakReference _wr;

    public bool IsDisposed
    {
        get { return _isDisposed; }
    }

    public CCGraphicsResource()
    {
        _wr = new WeakReference(this);

        lock (_createdResources)
        {
            _createdResources.Add(_wr);
        }
    }

    ~CCGraphicsResource()
    {
        if (!IsDisposed)
        {
            Dispose();
        }

        lock (_createdResources)
        {
            _createdResources.Remove(_wr);
        }
    }

    public virtual void Dispose()
    {
        _isDisposed = true;
    }

    public virtual void Reinit()
    {
    }

    internal static void ReinitAllResources()
    {
        lock (_createdResources)
        {
            var resources = _createdResources.Elements;
            for (int i = 0, count = _createdResources.Count; i < count; i++)
            {
                if (resources[i].IsAlive)
                {
                    ((CCGraphicsResource) resources[i].Target).Reinit();
                }
            }
        }
    }

    public static void DisposeAllResources()
    {
        lock (_createdResources)
        {
            var resources = _createdResources.Elements;
            for (int i = 0, count = _createdResources.Count; i < count; i++)
            {
                if (resources[i].IsAlive)
                {
                    ((CCGraphicsResource) resources[i].Target).Dispose();
                }
            }
        }
    }
}


public class CCVertexBuffer<T> : CCGraphicsResource where T : struct, IVertexType
{
    protected VertexBuffer _vertexBuffer;
    protected BufferUsage _usage;
    protected CCRawList<T> _data;

    internal VertexBuffer VertexBuffer
    {
        get { return _vertexBuffer; }
    }

    public CCRawList<T> Data
    {
        get { return _data; }
    }

    public int Count
    {
        get { return _data.Count; }
        set
        {
            Debug.Assert(value <= _data.Capacity);
            _data.count = value;
        }
    }

    public int Capacity
    {
        get { return _data.Capacity; }
        set
        {
            if (_data.Capacity != value)
            {
                _data.Capacity = value;
                Reinit();
            }
        }
    }

    public CCVertexBuffer(int vertexCount, BufferUsage usage)
    {
        _data = new CCRawList<T>(vertexCount);
        _usage = usage;
        Reinit();
    }
    
    public void UpdateBuffer()
    {
        UpdateBuffer(0, _data.Count);
    }

    public virtual void UpdateBuffer(int startIndex, int elementCount)
    {
        if (elementCount > 0)
        {
            _vertexBuffer.SetData(_data.Elements, startIndex, elementCount);
        }
    }

    public override void Reinit()
    {
        if (_vertexBuffer != null && !_vertexBuffer.IsDisposed)
        {
            _vertexBuffer.Dispose();
        }
        _vertexBuffer = new VertexBuffer(CCDrawManager.GraphicsDevice, typeof(T), _data.Capacity, _usage);
    }
}

public class CCQuadVertexBuffer : CCVertexBuffer<CCV3F_C4B_T2F_Quad>
{
    public CCQuadVertexBuffer(int vertexCount, BufferUsage usage) : base(vertexCount, usage)
    {
    }

    public void UpdateBuffer(CCRawList<CCV3F_C4B_T2F_Quad> data, int startIndex, int elementCount)
    {
        //TODO: 
        var tmp = _data;
        _data = data;
        
        UpdateBuffer(startIndex, elementCount);

        _data = tmp;
    }

    public override void UpdateBuffer(int startIndex, int elementCount)
    {
        if (elementCount == 0)
        {
            return;
        }

        var quads = _data.Elements;

        var tmp = CCDrawManager._tmpVertices;

        while (tmp.Capacity < elementCount)
        {
            tmp.Capacity = tmp.Capacity * 2;
        }
        tmp.Count = elementCount * 4;

        var vertices = tmp.Elements;

        int i4 = 0;
        for (int i = startIndex; i < startIndex + elementCount; i++)
        {
            vertices[i4 + 0] = quads[i].TopLeft;
            vertices[i4 + 1] = quads[i].BottomLeft;
            vertices[i4 + 2] = quads[i].TopRight;
            vertices[i4 + 3] = quads[i].BottomRight;

            i4 += 4;
        }

        _vertexBuffer.SetData(vertices, startIndex * 4, elementCount * 4);
    }

    public override void Reinit()
    {
        if (_vertexBuffer != null && !_vertexBuffer.IsDisposed)
        {
            _vertexBuffer.Dispose();
        }
        _vertexBuffer = new VertexBuffer(CCDrawManager.GraphicsDevice, typeof(CCV3F_C4B_T2F), _data.Capacity * 4, _usage);

        UpdateBuffer();
    }

    public override void Dispose()
    {
        base.Dispose();
        
        if (_vertexBuffer != null && !_vertexBuffer.IsDisposed)
        {
            _vertexBuffer.Dispose();
        }
        
        _vertexBuffer = null;
    }
}

public class CCIndexBuffer<T> : CCGraphicsResource where T : struct
{
    private IndexBuffer _indexBuffer;
    private BufferUsage _usage;
    private CCRawList<T> _data;

    internal IndexBuffer IndexBuffer
    {
        get { return _indexBuffer; }
    }

    public CCRawList<T> Data
    {
        get { return _data; }
    }

    public int Count
    {
        get { return _data.Count; }
        set
        {
            Debug.Assert(value <= _data.Capacity);
            _data.count = value;
        }
    }

    public int Capacity
    {
        get { return _data.Capacity; }
        set
        {
            if (_data.Capacity != value)
            {
                _data.Capacity = value;
                Reinit();
            }
        }
    }

    public CCIndexBuffer(int indexCount, BufferUsage usage)
    {
        _data = new CCRawList<T>(indexCount);
        _usage = usage;
        Reinit();
    }

    public override void Reinit()
    {
        if (_indexBuffer != null && !_indexBuffer.IsDisposed)
        {
            _indexBuffer.Dispose();
        }

        _indexBuffer = new IndexBuffer(CCDrawManager.GraphicsDevice, typeof(T), _data.Capacity, _usage);

        UpdateBuffer();
    }

    public void UpdateBuffer()
    {
        UpdateBuffer(0, _data.Count);
    }

    public void UpdateBuffer(int startIndex, int elementCount)
    {
        if (elementCount > 0)
        {
            _indexBuffer.SetData(_data.Elements, startIndex, elementCount);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_indexBuffer != null && !_indexBuffer.IsDisposed)
        {
            _indexBuffer.Dispose();
        }

        _indexBuffer = null;
    }
}