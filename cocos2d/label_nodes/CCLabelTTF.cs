#nullable disable
using System;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public class CCLabelTTF : CCSprite, ICCLabelProtocol
{
    private float _fontSize;
    private CCTextAlignment _hAlignment;
    private string _fontName;
    protected string m_pString = String.Empty;
    private CCSize _dimensions;
    private CCVerticalTextAlignment _vAlignment;

    public CCLabelTTF ()
    {
        _hAlignment = CCTextAlignment.Center;
        _vAlignment = CCVerticalTextAlignment.Top;
        _fontName = string.Empty;
        _fontSize = 0.0f;

        Init();
    }
    
    public CCLabelTTF (string text, string fontName, float fontSize) : 
        this (text, fontName, fontSize, CCSize.Zero, CCTextAlignment.Center,
              CCVerticalTextAlignment.Top)
    { }
    
    public CCLabelTTF (string text, string fontName, float fontSize, CCSize dimensions, CCTextAlignment hAlignment) :
        this (text, fontName, fontSize, dimensions, hAlignment, CCVerticalTextAlignment.Top)
    { }
    
    public CCLabelTTF (string text, string fontName, float fontSize, CCSize dimensions, CCTextAlignment hAlignment,
                       CCVerticalTextAlignment vAlignment)
    {
        InitWithString(text, fontName, fontSize, dimensions, hAlignment, vAlignment);
    }

    public string FontName
    {
        get { return _fontName; }
        set
        {
            if (_fontName != value)
            {
                _fontName = value;
                if (m_pString.Length > 0)
                {
                    Refresh();
                }
            }
        }
    }

    public float FontSize
    {
        get { return _fontSize; }
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                if (m_pString.Length > 0)
                {
                    Refresh();
                }
            }
        }
    }

    public CCSize Dimensions
    {
        get { return _dimensions; }
        set
        {
            if (!_dimensions.Equals(value))
            {
                _dimensions = value;
                if (m_pString.Length > 0)
                {
                    Refresh();
                }
            }
        }
    }

    public CCVerticalTextAlignment VerticalAlignment
    {
        get { return _vAlignment; }
        set
        {
            if (_vAlignment != value)
            {
                _vAlignment = value;
                if (m_pString.Length > 0)
                {
                    Refresh();
                }
            }
        }
    }

    public CCTextAlignment HorizontalAlignment
    {
        get { return _hAlignment; }
        set
        {
            if (_hAlignment != value)
            {
                _hAlignment = value;
                if (m_pString.Length > 0)
                {
                    Refresh();
                }
            }
                }
            }

    internal void Refresh()
    {
        //
        // This can only happen when the frame buffer is ready...
        //
        try
        {
            updateTexture();
            Dirty = false;
        }
        catch (Exception)
        {
        }
    }

    #region ICCLabelProtocol Members

/*
* This is where the texture should be created, but it messes with the drawing 
* of the object tree
* 
    public override void Draw()
    {
        if (Dirty)
        {
            updateTexture();
            Dirty = false;
        }
        base.Draw();
    }
*/
    public string Text
    {
        get { return m_pString; }
        set
        {
            // This is called in the update() call, so it should not do any drawing ...
            if (m_pString != value)
            {
                m_pString = value;
                updateTexture();
                Dirty = false;
            }
            //            Dirty = true;
        }
    }

    #endregion

    public override string ToString()
    {
        return string.Format("FontName:{0}, FontSize:{1}", _fontName, _fontSize);
    }

    public override bool Init()
    {
        return InitWithString("", "Helvetica", 12);
    }

    public bool InitWithString(string label, string fontName, float fontSize, CCSize dimensions, CCTextAlignment alignment)
    {
        return InitWithString(label, fontName, fontSize, dimensions, alignment, CCVerticalTextAlignment.Top);
    }

    public bool InitWithString(string label, string fontName, float fontSize)
    {
        return InitWithString(label, fontName, fontSize, CCSize.Zero, CCTextAlignment.Left,
                              CCVerticalTextAlignment.Top);
    }


    public bool InitWithString(string text, string fontName, float fontSize,
                               CCSize dimensions, CCTextAlignment hAlignment,
                               CCVerticalTextAlignment vAlignment)
    {
        if (base.Init())
        {
            // shader program
            //this->setShaderProgram(CCShaderCache::sharedShaderCache()->programForKey(SHADER_PROGRAM));

            _dimensions = new CCSize(dimensions.Width, dimensions.Height);
            _hAlignment = hAlignment;
            _vAlignment = vAlignment;
            _fontName = fontName;
            _fontSize = fontSize;

            Text = (text);

            return true;
        }

        return false;
    }

    private void updateTexture()
    {
        CCTexture2D tex = new CCTexture2D();
#if DEBUG
        CCLog.Log("CCLabelTTF: updating texture with string '{0}'", m_pString);
        CCLog.Log("Font: {0}, Size: {1}", _fontName, _fontSize);
        CCLog.Log("Dimensions: {0}, HAlignment: {1}, VAlignment: {2}", _dimensions, _hAlignment, _vAlignment);
        CCLog.Log("Content Scale Factor: {0}", CCMacros.CCContentScaleFactor());
#endif
        var result = tex.InitWithString(m_pString,
                           _dimensions.PointsToPixels(),
                           _hAlignment,
                           _vAlignment,
                           _fontName,
                           _fontSize);

        if (result)
        {
            // Preserve IsAntialiased setting from the old texture
            bool wasAntialiased = Texture?.IsAntialiased ?? CCTexture2D.DefaultAntialiased;

            // Dispose of the old texture, if it exists
            if (Texture != null)
            {
                Texture.Dispose();
            }

            Texture = tex;
            Texture.IsAntialiased = wasAntialiased;

            CCRect rect = CCRect.Zero;
            rect.Size = m_pobTexture.ContentSize;
            SetTextureRect(rect);
        }
        else
        {
            // Dispose of the new texture if it wasn't used
            tex.Dispose();
        }
    }
}