#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#if WINDOWS
using BitMiracle.LibTiff.Classic;
#endif

namespace Cocos2D;

public enum CCImageFormat
{
    Jpg = 0,
    Png,
    Tiff,
    Webp,
    Gif,
    RawData,
    UnKnown
}

internal enum CCTextureCacheType
{
    None,
    AssetFile,
    Data,
    RawData,
    String
}

internal struct CCStringCache
{
    public String Text;
    public CCSize Dimensions;
    public CCTextAlignment HAlignment;
    public CCVerticalTextAlignment VAlignment;
    public String FontName;
    public float FontSize;
}

internal struct CCTextureCacheInfo
{
    public CCTextureCacheType CacheType;
    public Object Data;
}

public class CCTexture2D : CCGraphicsResource
{
    public static SurfaceFormat DefaultAlphaPixelFormat = SurfaceFormat.Color;
    public static bool OptimizeForPremultipliedAlpha = true;
    public static bool PreserveSourceSurfaceFormat = true;

    public float TextHeightPaddingMultiplier = 3f;

    private CCTextureCacheInfo _cacheInfo;
    private Texture2D _texture2D;
    private bool _hasMipmaps;
    private bool _hasPremultipliedAlpha;
    private SurfaceFormat _pixelFormat;
    private SamplerState _samplerState;
    private CCSize _contentSize;
    private int _pixelsHigh;
    private int _pixelsWide;

    private bool _managed;
    private bool _antialiased;

    /// <summary>
    /// Default antialiased state for newly created textures. Set to false for pixel-perfect rendering.
    /// Default is true (bilinear filtering).
    /// </summary>
    public static bool DefaultAntialiased { get; set; } = true;

    public Action OnReInit;

    public CCTexture2D()
    {
        _samplerState = DefaultAntialiased ? SamplerState.LinearClamp : SamplerState.PointClamp;
        IsAntialiased = DefaultAntialiased;

        RefreshAntialiasSetting ();
    }

    public CCTexture2D(string fileName) : this()
    {
        if (!InitWithFile(fileName))
        {
            CCLog.Log("CCTexture2D (string fileName): Problems initializing class");
        }
    }

    public CCTexture2D(byte[] data, SurfaceFormat pixelFormat = SurfaceFormat.Color, bool mipMap = false) : this()
    {
        if (!InitWithData(data, pixelFormat, mipMap))
        {
            CCLog.Log("CCTexture2D: Problems initializing class");
        }
    }

    public bool IsTextureDefined
    {
        get { return (_texture2D != null && !_texture2D.IsDisposed); }
    }

    public Texture2D XNATexture
    {
        get
        {
            if (_texture2D == null || _texture2D.IsDisposed)
            {
                Reinit();
            }
            return _texture2D;
        }
    }

    /// <summary>
    ///     pixel format of the texture
    /// </summary>
    public SurfaceFormat PixelFormat
    {
        get { return _pixelFormat; }
        set { _pixelFormat = value; }
    }

    /// <summary>
    ///     width in pixels
    /// </summary>
    public int PixelsWide
    {
        get { return _pixelsWide; }
        set { _pixelsWide = value; }
    }

    /// <summary>
    ///     hight in pixels
    /// </summary>
    public int PixelsHigh
    {
        get { return _pixelsHigh; }
        set { _pixelsHigh = value; }
    }

    /// <summary>
    ///     texture name
    /// </summary>
    public Texture2D Name
    {
        get { return XNATexture; }
    }

    /// <summary>
    ///     content size
    /// </summary>
    public CCSize ContentSizeInPixels
    {
        get { return _contentSize; }
        set { _contentSize = value; }
    }

    public CCSize ContentSize
    {
        get { return _contentSize.PixelsToPoints(); }
    }

    /// <summary>
    ///     whether or not the texture has their Alpha premultiplied
    /// </summary>
    public bool HasPremultipliedAlpha
    {
        get { return _hasPremultipliedAlpha; }
        set { _hasPremultipliedAlpha = value; }
    }

    public SamplerState SamplerState
    {
        get { return _samplerState; }
        set { _samplerState = value; }
    }

    public bool IsAntialiased
    {
        get { return _antialiased; }

        set
        {
            if (_antialiased != value)
            {
                _antialiased = value;

                RefreshAntialiasSetting ();
            }
        }
    }

    void RefreshAntialiasSetting ()
    {
        var saveState = _samplerState;

        if (_antialiased && _samplerState.Filter != TextureFilter.Linear)
        {
            if (_samplerState == SamplerState.PointClamp)
            {
                _samplerState = SamplerState.LinearClamp;
                return;
            }

            _samplerState = new SamplerState
            {
                Filter = TextureFilter.Linear
            };
        }
        else if (!_antialiased && _samplerState.Filter != TextureFilter.Point)
        {
            if (_samplerState == SamplerState.LinearClamp)
            {
                _samplerState = SamplerState.PointClamp;
                return;
            }

            _samplerState = new SamplerState
            {
                Filter = TextureFilter.Point
            };
        }
        else
        {
            return;
        }

        _samplerState.AddressU = saveState.AddressU;
        _samplerState.AddressV = saveState.AddressV;
        _samplerState.AddressW = saveState.AddressW;
    }

    public uint BytesPerPixelForFormat
    {
        //from MG: Microsoft.Xna.Framework.Graphics.GraphicsExtensions
        get
        {
            switch (_pixelFormat)
            {
                case SurfaceFormat.Dxt1:
#if !WINDOWS
                case SurfaceFormat.Dxt1a:
                case SurfaceFormat.RgbPvrtc2Bpp:
                case SurfaceFormat.RgbaPvrtc2Bpp:
                case SurfaceFormat.RgbEtc1:
#endif
                    // One texel in DXT1, PVRTC 2bpp and ETC1 is a minimum 4x4 block, which is 8 bytes
                    return 8;

                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
#if !WINDOWS
                case SurfaceFormat.RgbPvrtc4Bpp:
                case SurfaceFormat.RgbaPvrtc4Bpp:
#endif
                    // One texel in DXT3, DXT5 and PVRTC 4bpp is a minimum 4x4 block, which is 16 bytes
                    return 16;

                case SurfaceFormat.Alpha8:
                    return 1;

                case SurfaceFormat.Bgr565:
                case SurfaceFormat.Bgra4444:
                case SurfaceFormat.Bgra5551:
                case SurfaceFormat.HalfSingle:
                case SurfaceFormat.NormalizedByte2:
                    return 2;

                case SurfaceFormat.Color:
                case SurfaceFormat.Single:
                case SurfaceFormat.Rg32:
                case SurfaceFormat.HalfVector2:
                case SurfaceFormat.NormalizedByte4:
                case SurfaceFormat.Rgba1010102:
                    return 4;

                case SurfaceFormat.HalfVector4:
                case SurfaceFormat.Rgba64:
                case SurfaceFormat.Vector2:
                    return 8;

                case SurfaceFormat.Vector4:
                    return 16;

                default:
                    throw new NotImplementedException();
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_texture2D != null && !_texture2D.IsDisposed && !_managed)
        {
            _texture2D.Dispose();
        }
        _texture2D = null;
    }

    public virtual int TotalBytes
    {
        get
        {
            int size = _pixelsHigh * _pixelsWide;
            if (IsTextureDefined)
            {
#if !XNA
                size *= XNATexture.Format.GetSize();
#else
                size *= (int)BytesPerPixelForFormat;
#endif
            }
            return (size);
        }
    }

    public override string ToString()
    {
        int size = TotalBytes;
        size = size >> 10; // KB
        return String.Format("[CCTexture2D | Dimensions = {0} x {1} | {2} | {3} | {4} KB)]", _pixelsWide, _pixelsHigh, _managed ? "Managed" : "Native", IsTextureDefined ? "Valid" : "InValid", size);
    }

    public void SaveAsJpeg(Stream stream, int width, int height)
    {
        if (_texture2D != null)
        {
            _texture2D.SaveAsJpeg(stream, width, height);
        }
    }

    public void SaveAsPng(Stream stream, int width, int height)
    {
        if (_texture2D != null)
        {
            _texture2D.SaveAsPng(stream, width, height);
        }
    }

    #region Initialization

    public bool Init(int pixelsWide, int pixelsHigh)
    {
        return Init(pixelsWide, pixelsHigh, DefaultAlphaPixelFormat, true, false);
    }

    public bool Init(int pixelsWide, int pixelsHigh, SurfaceFormat pixelFormat)
    {
        return Init(pixelsWide, pixelsHigh, pixelFormat, true, false);
    }

    public bool Init(int pixelsWide, int pixelsHigh, SurfaceFormat pixelFormat, bool premultipliedAlpha)
    {
        return Init(pixelsWide, pixelsHigh, pixelFormat, premultipliedAlpha, false);
    }

    public bool Init(int pixelsWide, int pixelsHigh, SurfaceFormat pixelFormat, bool premultipliedAlpha, bool mipMap)
    {
        try
        {
            var texture = new Texture2D(CCDrawManager.GraphicsDevice, pixelsWide, pixelsHigh, mipMap, pixelFormat);

            if (InitWithTexture(texture, pixelFormat, premultipliedAlpha, false))
            {
                _cacheInfo.CacheType = CCTextureCacheType.None;
                _cacheInfo.Data = null;

                return true;
            }
        }
        catch (Exception)
        {
        }
        return false;
    }

    public bool InitWithData(byte[] data)
    {
        return InitWithData(data, DefaultAlphaPixelFormat);
    }

    public bool InitWithData(byte[] data, SurfaceFormat pixelFormat)
    {
        return InitWithData(data, DefaultAlphaPixelFormat, false);
    }

    public bool InitWithData(byte[] data, SurfaceFormat pixelFormat, bool mipMap)
    {
        if (data == null)
        {
            return (false);
        }
        var texture = LoadTexture(new MemoryStream(data, false));

        if (texture != null)
        {
            if (InitWithTexture(texture, pixelFormat, true, false))
            {
                _cacheInfo.CacheType = CCTextureCacheType.Data;
                _cacheInfo.Data = data;

                if (mipMap)
                {
                    GenerateMipmap();
                }

                return true;
            }
        }

        return false;
    }

    public bool InitWithStream(Stream stream)
    {
        return InitWithStream(stream, DefaultAlphaPixelFormat);
    }

    public bool InitWithStream(Stream stream, SurfaceFormat pixelFormat)
    {
        Texture2D texture;
        try
        {
            texture = LoadTexture(stream);

            InitWithTexture(texture, pixelFormat, false, false);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool InitWithRawData<T>(T[] data, SurfaceFormat pixelFormat, int pixelsWide, int pixelsHigh,
                                   bool premultipliedAlpha)
        where T : struct
    {
        return InitWithRawData(data, pixelFormat, pixelsWide, pixelsHigh,
                               premultipliedAlpha, false, new CCSize(pixelsWide, pixelsHigh));
    }

    public bool InitWithRawData<T>(T[] data, SurfaceFormat pixelFormat, int pixelsWide, int pixelsHigh,
                                   bool premultipliedAlpha, bool mipMap)
        where T : struct
    {
        return InitWithRawData(data, pixelFormat, pixelsWide, pixelsHigh,
                               premultipliedAlpha, mipMap, new CCSize(pixelsWide, pixelsHigh));
    }

    public bool InitWithRawData<T>(T[] data, SurfaceFormat pixelFormat, int pixelsWide, int pixelsHigh,
                                   bool premultipliedAlpha, bool mipMap, CCSize contentSize) where T : struct
    {
        try
        {
            var texture = LoadRawData(data, pixelsWide, pixelsHigh, pixelFormat, mipMap);

            if (InitWithTexture(texture, pixelFormat, premultipliedAlpha, false))
            {
                _contentSize = contentSize;

                _cacheInfo.CacheType = CCTextureCacheType.RawData;
                _cacheInfo.Data = data;

                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public bool InitWithString(string text, string fontName, float fontSize)
    {
        return InitWithString(text, CCSize.Zero, CCTextAlignment.Center, CCVerticalTextAlignment.Top, fontName,
                              fontSize);
    }

    public bool InitWithString(string text, CCSize dimensions, CCTextAlignment hAlignment,
                               CCVerticalTextAlignment vAlignment, string fontName,
                               float fontSize)
    {
        try
        {
            Debug.Assert(dimensions.Width >= 0 || dimensions.Height >= 0);

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            float loadedSize = fontSize;
#if DEBUG
            CCLog.Log("Loading font '{0}' at size {1}", fontName, fontSize);
#endif
            SpriteFont font = CCSpriteFontCache.SharedInstance.TryLoadFont(fontName, fontSize, out loadedSize);

            if (font == null)
            {
                CCLog.Log("Failed to load default font. No font supported.");
                return (false);
            }

            float scale = 1f;

            if (loadedSize != 0)
            {
                scale = fontSize / loadedSize * CCSpriteFontCache.FontScale;
            }

            scale *= CCMacros.CCContentScaleFactor();

            if (dimensions.Equals(CCSize.Zero))
            {
                Vector2 temp = font.MeasureString(text);
                dimensions.Width = temp.X * scale;
                dimensions.Height = temp.Y * scale * TextHeightPaddingMultiplier;
            }

            var textList = new List<String>();
            string[] lineList = text.Split('\n');

            StringBuilder next = new StringBuilder();
            string last = null;
            for (int j = 0; j < lineList.Length; ++j)
            {
                string[] wordList = lineList[j].Split(' ');
                for (int i = 0; i < wordList.Length; )
                {
                    // Run through the list of words to create a sentence that fits in the dimensions.Width space
                    while (i < wordList.Length)
                    {
                        if ((font.MeasureString(next.ToString()).X * scale) > dimensions.Width)
                        {
                            i--;
                            break;
                        }
                        last = next.ToString();
                        if (next.Length > 0)
                        {
                            next.Append(' ');
                        }
                        next.Append(wordList[i]);
                        i++;
                    }
                    if (i == wordList.Length || i == -1) // -1 means the default width was too small for the string.
                    {
                        string nstr = next.ToString();
                        if ((font.MeasureString(nstr).X * scale) > dimensions.Width)
                        {
                            // Last line could have bleed into the margin
                            if(last != null && last.Length > 0)
                                textList.Add(last); // Single word label has a null last which can cause problems
                            textList.Add(wordList[wordList.Length - 1]); // last word bleeds
                        }
                        else if(nstr.Length > 0)
                        {
                            textList.Add(nstr);
                        }
                    }
                    else if(last.Length > 0)
                    {
                        textList.Add(last);
                    }
                    last = null;
                    next.Length = 0;
                }
            }

            if (textList.Count == 0 && text.Length > 0)
            {
                textList.Add(text);
            }

            if (dimensions.Height == 0)
            {
                dimensions.Height = textList.Count * font.LineSpacing * scale * TextHeightPaddingMultiplier;
            }

            //*  for render to texture
            RenderTarget2D renderTarget = CCDrawManager.CreateRenderTarget(
                (int)dimensions.Width, (int)dimensions.Height,
                DefaultAlphaPixelFormat, RenderTargetUsage.DiscardContents
                );

            try
            {
                CCDrawManager.SetRenderTarget(renderTarget);
            }
            catch (Exception)
            {
                CCTextureCache.SharedTextureCache.RemoveUnusedTextures();
                CCDrawManager.SetRenderTarget(renderTarget);
            }
            CCDrawManager.Clear(Color.Transparent);

            SpriteBatch sb = CCDrawManager.spriteBatch;
            sb.Begin();

            float textHeight = textList.Count * font.LineSpacing * scale * TextHeightPaddingMultiplier;
            float nextY = textHeight / TextHeightPaddingMultiplier;

            if (vAlignment == CCVerticalTextAlignment.Bottom)
            {
                nextY = dimensions.Height - textHeight;
            }
            else if (vAlignment == CCVerticalTextAlignment.Center)
            {
                nextY = (dimensions.Height - textHeight) / 2.0f;
            }

            for (int j = 0; j < textList.Count; ++j)
            {
                string line = textList[j];

                var position = new Vector2(0, nextY);

                if (hAlignment == CCTextAlignment.Right)
                {
                    position.X = dimensions.Width - font.MeasureString(line).X * scale;
                }
                else if (hAlignment == CCTextAlignment.Center)
                {
                    position.X = (dimensions.Width - font.MeasureString(line).X * scale) / 2.0f;
                }

                sb.DrawString(font, line, position, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);

                nextY += font.LineSpacing * scale;
            }

            sb.End();

            CCDrawManager.graphicsDevice.RasterizerState = RasterizerState.CullNone;
            CCDrawManager.graphicsDevice.DepthStencilState = DepthStencilState.Default;

            CCDrawManager.SetRenderTarget((RenderTarget2D)null);

            if (InitWithTexture(renderTarget, renderTarget.Format, true, false))
            {
                _cacheInfo.CacheType = CCTextureCacheType.String;
                _cacheInfo.Data = new CCStringCache()
                {
                    Dimensions = dimensions,
                    Text = text,
                    FontName = fontName,
                    FontSize = fontSize,
                    HAlignment = hAlignment,
                    VAlignment = vAlignment
                };

                return true;
            }
        }
        catch (Exception ex)
        {
            CCLog.Log(ex.ToString());
        }
        return false;
    }

    public bool InitWithTexture(Texture2D texture)
    {
        return InitWithTexture(texture, texture.Format, true, false);
    }

    internal bool InitWithTexture(Texture2D texture, SurfaceFormat format, bool premultipliedAlpha, bool managed)
    {
        _managed = managed;

        if (null == texture)
        {
            return false;
        }

        if (OptimizeForPremultipliedAlpha && !premultipliedAlpha)
        {
            _texture2D = ConvertToPremultiplied(texture, format);

            if (!_managed)
            {
                texture.Dispose();
                _managed = false;
            }
        }
        else
        {
            if (texture.Format != format)
            {
                _texture2D = ConvertSurfaceFormat(texture, format);

                if (!_managed)
                {
                    texture.Dispose();
                    _managed = false;
                }
            }
            else
            {
                _texture2D = texture;
            }
        }

        _pixelFormat = texture.Format;
        _pixelsWide = texture.Width;
        _pixelsHigh = texture.Height;
        _contentSize.Width = texture.Width;
        _contentSize.Height = texture.Height;
        _hasMipmaps = texture.LevelCount > 1;
        _hasPremultipliedAlpha = premultipliedAlpha;

        return true;
    }

    public bool InitWithFile(string file)
    {
        _managed = false;

        Texture2D texture = null;

        _cacheInfo.CacheType = CCTextureCacheType.AssetFile;
        _cacheInfo.Data = file;

        //TODO: may be move this functional to CCContentManager?

        var contentManager = CCContentManager.SharedContentManager;

        var loadedFile = file;

        // first try to download xnb
        if (Path.HasExtension(loadedFile))
        {
            loadedFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
        }

        // use WeakReference. Link for regular textures are stored in CCTextureCache
        texture = contentManager.TryLoad<Texture2D>(loadedFile, true);

        if (texture != null)
        {
            // usually xnb texture prepared as PremultipliedAlpha
            return InitWithTexture(texture, PreserveSourceSurfaceFormat ? texture.Format : DefaultAlphaPixelFormat, true, true);
        }

        // try load raw image
        if (loadedFile != file)
        {
            texture = contentManager.TryLoad<Texture2D>(file, true);

            if (texture != null)
            {
                // not premultiplied alpha
                return InitWithTexture(texture, PreserveSourceSurfaceFormat ? texture.Format : DefaultAlphaPixelFormat, false, true);
            }
        }

        // try load not supported format (for example tga)
        try
        {
            using (var stream = contentManager.GetAssetStream(file))
            {
                return InitWithStream(stream, DefaultAlphaPixelFormat);
            }
        }
        catch (Exception)
        {
        }

        CCLog.Log("Texture {0} was not found.", file);
        return false;
    }

    public override void Reinit()
    {
			CCLog.Log("reinit called on {1} '{0}' {2}", ToString(),
            _cacheInfo.CacheType,
				(_cacheInfo.CacheType == CCTextureCacheType.AssetFile || _cacheInfo.CacheType == CCTextureCacheType.String) ? _cacheInfo.Data : string.Empty);

        Texture2D textureToDispose = null;
        if (_texture2D != null && !_texture2D.IsDisposed && !_managed)
        {
            textureToDispose = _texture2D;
            // _texture2D.Dispose();
        }

        _managed = false;
        _texture2D = null;

        if (_cacheInfo.CacheType != CCTextureCacheType.None)
        {
            switch (_cacheInfo.CacheType)
            {
                case CCTextureCacheType.None:
                    return;

                case CCTextureCacheType.AssetFile:
                    InitWithFile((string)_cacheInfo.Data);
                    break;

                case CCTextureCacheType.Data:
                    InitWithData((byte[])_cacheInfo.Data, _pixelFormat, _hasMipmaps);
                    break;

                case CCTextureCacheType.RawData:
                    var methodInfo = typeof(CCTexture2D).GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "InitWithRawData" && m.IsGenericMethod && m.GetParameters().Length == 7);
                    if (methodInfo != null)
                    {
                        var genericMethod = methodInfo.MakeGenericMethod(_cacheInfo.Data.GetType().GetElementType());
                        genericMethod.Invoke(this, new object[]
                        {
                        _cacheInfo.Data,
                        _pixelFormat, _pixelsWide, _pixelsHigh,
                        _hasPremultipliedAlpha, _hasMipmaps, _contentSize
                        });
                    }

                    //                    InitWithRawData((byte[])_cacheInfo.Data, _pixelFormat, _pixelsWide, _pixelsHigh,
                    //                                    _hasPremultipliedAlpha, _hasMipmaps, _contentSize);
                    break;

                case CCTextureCacheType.String:
                    var si = (CCStringCache)_cacheInfo.Data;
                    InitWithString(si.Text, si.Dimensions, si.HAlignment, si.VAlignment, si.FontName, si.FontSize);
                    if (_hasMipmaps)
                    {
                        _hasMipmaps = false;
                        GenerateMipmap();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        if (textureToDispose != null && !textureToDispose.IsDisposed)
        {
            textureToDispose.Dispose();
        }
        if (OnReInit != null)
        {
            OnReInit();
        }
    }

    #endregion

    #region Conversion

    public void GenerateMipmap()
    {
        if (!_hasMipmaps)
        {
            var target = new RenderTarget2D(CCDrawManager.GraphicsDevice, PixelsWide, PixelsHigh, true, PixelFormat,
                                            DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            CCDrawManager.SetRenderTarget(target);

            SpriteBatch sb = CCDrawManager.spriteBatch;

            sb.Begin();
            sb.Draw(_texture2D, Vector2.Zero, Color.White);
            sb.End();

            if (!_managed)
            {
                _texture2D.Dispose();
            }

            _managed = false;
            _texture2D = target;

            _hasMipmaps = true;
        }
    }

    private Texture2D ConvertSurfaceFormat(Texture2D texture, SurfaceFormat format)
    {
        if (texture.Format == format)
        {
            return texture;
        }

        var renderTarget = new RenderTarget2D(
            CCDrawManager.GraphicsDevice,
            texture.Width, texture.Height, _hasMipmaps, format,
            DepthFormat.None, 0, RenderTargetUsage.DiscardContents
            );

        CCDrawManager.SetRenderTarget(renderTarget);
        CCDrawManager.Clear(Color.Transparent);
        CCDrawManager.spriteBatch.Begin(SpriteSortMode.Immediate, HasPremultipliedAlpha ? BlendState.AlphaBlend : BlendState.NonPremultiplied);
        CCDrawManager.spriteBatch.Draw(texture, new Vector2(0, 0), Color.White);
        CCDrawManager.spriteBatch.End();
        CCDrawManager.SetRenderTarget((CCTexture2D)null);

        return renderTarget;
    }

    private Texture2D ConvertToPremultiplied(Texture2D texture, SurfaceFormat format)
    {
        //Jake Poznanski - Speeding up XNA Content Load
        //http://jakepoz.com/jake_poznanski__speeding_up_xna.html

        //Setup a render target to hold our final texture which will have premulitplied alpha values
        var result = new RenderTarget2D(
            CCDrawManager.graphicsDevice,
            texture.Width, texture.Height, _hasMipmaps, format,
            DepthFormat.None, 0, RenderTargetUsage.DiscardContents
            );

        CCDrawManager.SetRenderTarget(result);

        CCDrawManager.Clear(Color.Transparent);

        var spriteBatch = CCDrawManager.spriteBatch;

        if (format != SurfaceFormat.Alpha8)
        {
            //Multiply each color by the source alpha, and write in just the color values into the final texture
            var blendColor = new BlendState();
            blendColor.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green |
                                            ColorWriteChannels.Blue;

            blendColor.AlphaDestinationBlend = Blend.Zero;
            blendColor.ColorDestinationBlend = Blend.Zero;

            blendColor.AlphaSourceBlend = Blend.SourceAlpha;
            blendColor.ColorSourceBlend = Blend.SourceAlpha;

            spriteBatch.Begin(SpriteSortMode.Immediate, blendColor);
            spriteBatch.Draw(texture, texture.Bounds, Color.White);
            spriteBatch.End();
        }

        //Now copy over the alpha values from the PNG source texture to the final one, without multiplying them
        var blendAlpha = new BlendState();

        blendAlpha.ColorWriteChannels = ColorWriteChannels.Alpha;

        blendAlpha.AlphaDestinationBlend = Blend.Zero;
        blendAlpha.ColorDestinationBlend = Blend.Zero;

        blendAlpha.AlphaSourceBlend = Blend.One;
        blendAlpha.ColorSourceBlend = Blend.One;

        spriteBatch.Begin(SpriteSortMode.Immediate, blendAlpha);
        spriteBatch.Draw(texture, texture.Bounds, Color.White);
        spriteBatch.End();

        //Release the GPU back to drawing to the screen
        CCDrawManager.SetRenderTarget((CCTexture2D) null);

        return result;
    }

    #endregion

    #region Loading Texture

    private Texture2D LoadTexture(Stream stream)
    {
        return LoadTexture(stream, CCImageFormat.UnKnown);
    }

    private Texture2D LoadRawData<T>(T[] data, int width, int height, SurfaceFormat pixelFormat, bool mipMap) where T : struct
    {
        var result = new Texture2D(CCDrawManager.GraphicsDevice, width, height, mipMap, pixelFormat);
        result.SetData(data);
        return result;
    }

    private Texture2D LoadTexture(Stream stream, CCImageFormat imageFormat)
    {
        Texture2D result = null;

        if (imageFormat == CCImageFormat.UnKnown)
        {
            imageFormat = DetectImageFormat(stream);
        }

        if (imageFormat == CCImageFormat.Tiff)
        {
            result = LoadTextureFromTiff(stream);
        }

        if (imageFormat == CCImageFormat.Jpg || imageFormat == CCImageFormat.Png || imageFormat == CCImageFormat.Gif)
        {
            result = Texture2D.FromStream(CCDrawManager.GraphicsDevice, stream);
        }

        return result;
    }

    public static CCImageFormat DetectImageFormat(Stream stream)
    {
        var data = new byte[8];

        var pos = stream.Position;
        var dataLen = stream.Read(data, 0, 8);
        stream.Position = pos;

        if (dataLen >= 8)
        {
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            {
                return CCImageFormat.Png;
            }
        }

        if (dataLen >= 3)
        {
            if (data[0] == 0x47 && data[1] == 0x49 && data[1] == 0x46)
            {
                return CCImageFormat.Gif;
            }
        }

        if (dataLen >= 2)
        {
            if ((data[0] == 0x49 && data[1] == 0x49) || (data[0] == 0x4d && data[1] == 0x4d))
            {
                return CCImageFormat.Tiff;
            }
        }

        if (dataLen >= 2)
        {
            if (data[0] == 0xff && data[1] == 0xd8)
            {
                return CCImageFormat.Jpg;
            }
        }

        return CCImageFormat.UnKnown;
    }

    private Texture2D LoadTextureFromTiff(Stream stream)
    {
#if WINDOWS
        var tiff = Tiff.ClientOpen("file.tif", "r", stream, new TiffStream());

        var w = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
        var h = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

        var raster = new int[w * h];

        if (tiff.ReadRGBAImageOriented(w, h, raster, Orientation.LEFTTOP))
        {
            var result = new Texture2D(CCDrawManager.GraphicsDevice, w, h, false, SurfaceFormat.Color);
            result.SetData(raster);
            return result;
        }
			else
			{
				return null;
			}
#elif MACOS || IOS

			return Texture2D.FromStream(CCDrawManager.GraphicsDevice, stream);
#else
			return null;
#endif
    }


    #endregion
}

// On the AOT console fork (NETFRAMEWORK) this is renamed from GraphicsExtensions to avoid a
// BRUTE C++ name collision with MonoGame's own
// Microsoft.Xna.Framework.Graphics.GraphicsExtensions.GetSize(SurfaceFormat): BRUTE mangles
// static calls by class+method only (namespace-blind), so two "GraphicsExtensions.GetSize"
// classes produce the same C++ symbol and every call site becomes ambiguous. Callers use the
// .GetSize() extension syntax, so the class name is irrelevant to C# resolution. Other targets
// keep the original name so this stays a non-breaking, inert change off the console fork.
#if NETFRAMEWORK
public static class CCGraphicsExtensions
#else
public static class GraphicsExtensions
#endif
{
    public static int GetSize(this SurfaceFormat surfaceFormat)
    {
        switch (surfaceFormat)
        {
            case SurfaceFormat.Dxt1:
            case SurfaceFormat.Dxt1SRgb:
            case SurfaceFormat.Dxt1a:
            case SurfaceFormat.RgbPvrtc2Bpp:
            case SurfaceFormat.RgbaPvrtc2Bpp:
            case SurfaceFormat.RgbPvrtc4Bpp:
            case SurfaceFormat.RgbaPvrtc4Bpp:
            case SurfaceFormat.RgbEtc1:
                // One texel in DXT1, PVRTC (2bpp and 4bpp) and ETC1 is a minimum 4x4 block (8x4 for PVRTC 2bpp), which is 8 bytes
                return 8;
            case SurfaceFormat.Dxt3:
            case SurfaceFormat.Dxt3SRgb:
            case SurfaceFormat.Dxt5:
            case SurfaceFormat.Dxt5SRgb:
            case SurfaceFormat.RgbaAtcExplicitAlpha:
            case SurfaceFormat.RgbaAtcInterpolatedAlpha:
                // One texel in DXT3 and DXT5 is a minimum 4x4 block, which is 16 bytes
                return 16;
            case SurfaceFormat.Alpha8:
                return 1;
            case SurfaceFormat.Bgr565:
            case SurfaceFormat.Bgra4444:
            case SurfaceFormat.Bgra5551:
            case SurfaceFormat.HalfSingle:
            case SurfaceFormat.NormalizedByte2:
                return 2;
            case SurfaceFormat.Color:
            case SurfaceFormat.ColorSRgb:
            case SurfaceFormat.Single:
            case SurfaceFormat.Rg32:
            case SurfaceFormat.HalfVector2:
            case SurfaceFormat.NormalizedByte4:
            case SurfaceFormat.Rgba1010102:
            case SurfaceFormat.Bgra32:
            case SurfaceFormat.Bgra32SRgb:
            case SurfaceFormat.Bgr32:
            case SurfaceFormat.Bgr32SRgb:
                return 4;
            case SurfaceFormat.HalfVector4:
            case SurfaceFormat.Rgba64:
            case SurfaceFormat.Vector2:
                return 8;
            case SurfaceFormat.Vector4:
                return 16;
            default:
                throw new ArgumentException();
        }
    }
}

