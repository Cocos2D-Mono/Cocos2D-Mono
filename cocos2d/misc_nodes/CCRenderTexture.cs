#nullable disable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public partial class CCRenderTexture : CCNode
{
    private bool _firstUsage = true;
    protected SurfaceFormat m_ePixelFormat;
    private RenderTarget2D _renderTarget2D;
    protected CCSprite m_pSprite;
    protected CCTexture2D m_pTexture;

    public CCRenderTexture()
    {
        m_ePixelFormat = SurfaceFormat.Color;
    }

    public CCSprite Sprite
    {
        get { return m_pSprite; }
        set { m_pSprite = value; }
    }

    public CCRenderTexture(int w, int h)
    {
        InitWithWidthAndHeight(w, h, SurfaceFormat.Color, DepthFormat.None, RenderTargetUsage.DiscardContents);
    }

    public CCRenderTexture(int w, int h, SurfaceFormat format)
    {
        InitWithWidthAndHeight(w, h, format, DepthFormat.None, RenderTargetUsage.DiscardContents);
    }

    public CCRenderTexture(int w, int h, SurfaceFormat format, DepthFormat depthFormat, RenderTargetUsage usage)
    {
        InitWithWidthAndHeight(w, h, format, depthFormat, usage);
    }

    private void TextureReInit()
    {
        _renderTarget2D = null;
        m_pTexture = null;
        if (m_pSprite != null)
        {
            m_pSprite.RemoveFromParent();
            m_pSprite = null;
        }
        MakeTexture();
    }

    private void MakeTexture()
    {
        m_pTexture = new CCTexture2D();
        m_pTexture.OnReInit = TextureReInit;
        m_pTexture.IsAntialiased = false;

        _renderTarget2D = CCDrawManager.CreateRenderTarget(_width, _height, _colorFormat, _depthFormat, _usage);
        m_pTexture.InitWithTexture(_renderTarget2D, _colorFormat, true, false);

        _firstUsage = true;

        m_pSprite = new CCSprite(m_pTexture);
        //m_pSprite.scaleY = -1;
        m_pSprite.BlendFunc = CCBlendFunc.AlphaBlend;

        AddChild(m_pSprite);
    }

    private SurfaceFormat _colorFormat;
    private DepthFormat _depthFormat;
    private RenderTargetUsage _usage;
    private int _width, _height;

    protected virtual bool InitWithWidthAndHeight(int w, int h, SurfaceFormat colorFormat, DepthFormat depthFormat, RenderTargetUsage usage)
    {
        _width = (int)Math.Ceiling(w * CCMacros.CCContentScaleFactor());
        _height = (int)Math.Ceiling(h * CCMacros.CCContentScaleFactor());
        _colorFormat = colorFormat;
        _depthFormat = depthFormat;
        _usage = usage;
        MakeTexture();
        return true;
    }

    public virtual void Begin()
    {
        if (m_pTexture.IsDisposed)
        {
            TextureReInit();
        }
        // Save the current matrix
        CCDrawManager.PushMatrix();

        CCSize texSize = m_pTexture.ContentSizeInPixels;

        // Calculate the adjustment ratios based on the old and new projections
        CCDirector director = CCDirector.SharedDirector;
        CCSize size = director.WinSizeInPixels;
        float widthRatio = size.Width / texSize.Width;
        float heightRatio = size.Height / texSize.Height;

        CCDrawManager.SetRenderTarget(m_pTexture);

        CCDrawManager.SetViewPort(0, 0, (int) texSize.Width, (int) texSize.Height);

        Matrix projection = Matrix.CreateOrthographicOffCenter(
            -1.0f / widthRatio, 1.0f / widthRatio,
            -1.0f / heightRatio, 1.0f / heightRatio,
            -1, 1
            );

        CCDrawManager.MultMatrix(ref projection);

        if (_firstUsage)
        {
            CCDrawManager.Clear(Microsoft.Xna.Framework.Color.Transparent);
            _firstUsage = false;
        }
    }

    public void BeginWithClear(float r, float g, float b, float a)
    {
        Begin();
        CCDrawManager.Clear(new Color(r, g, b, a));
    }

    public void BeginWithClear(float r, float g, float b, float a, float depthValue)
    {
        Begin();
        CCDrawManager.Clear(new Color(r, g, b, a), depthValue);
    }

    public void BeginWithClear(float r, float g, float b, float a, float depthValue, int stencilValue)
    {
        Begin();
        CCDrawManager.Clear(new Color(r, g, b, a), depthValue, stencilValue);
    }

    public void ClearDepth(float depthValue)
    {
        Begin();
        CCDrawManager.Clear(ClearOptions.DepthBuffer, Microsoft.Xna.Framework.Color.White, depthValue, 0);
        End();
    }

    public void ClearStencil(int stencilValue)
    {
        Begin();
        CCDrawManager.Clear(ClearOptions.Stencil, Microsoft.Xna.Framework.Color.White, 0, stencilValue);
        End();
    }

    public virtual void End()
    {
        CCDrawManager.PopMatrix();

        CCDirector director = CCDirector.SharedDirector;

        CCDrawManager.SetRenderTarget((CCTexture2D) null);

        director.Projection = director.Projection;
    }

    public void Clear(float r, float g, float b, float a)
    {
        BeginWithClear(r, g, b, a);
        End();
    }

    public bool SaveToStream(Stream stream, CCImageFormat format)
    {
        if (format == CCImageFormat.Png)
        {
            m_pTexture.SaveAsPng(stream, m_pTexture.PixelsWide, m_pTexture.PixelsHigh);
        }
        else if (format == CCImageFormat.Jpg)
        {
            m_pTexture.SaveAsJpeg(stream, m_pTexture.PixelsWide, m_pTexture.PixelsHigh);
        }
        else
        {
            return false;
        }
        
        return true;
    }
}