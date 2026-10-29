using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cocos2D;

public class CCLayerRGBA : CCLayer, ICCRGBAProtocol
{
    // Copies of CCNode's fields. Layer subclasses built against earlier versions read and write
    // these by name, so they stay until 3.0. The members below override CCNode's, so every call,
    // through a CCNode or not, uses these copies.
    protected new byte _displayedOpacity;
    protected new byte _realOpacity;
    protected new CCColor3B _displayedColor;
    protected new CCColor3B _realColor;
    protected new bool _cascadeColorEnabled;
    protected new bool _cascadeOpacityEnabled;

    public override CCColor3B Color
    {
        get { return _realColor; }
        set
        {
            _displayedColor = _realColor = value;

            if (_cascadeColorEnabled)
            {
                var parentColor = CCTypes.CCWhite;
                var parent = m_pParent as ICCRGBAProtocol;
                if (parent != null && parent.CascadeColorEnabled)
                {
                    parentColor = parent.DisplayedColor;
                }

                UpdateDisplayedColor(parentColor);
            }
        }
    }

    public override CCColor3B DisplayedColor
    {
        get { return _displayedColor; }
    }

    public override byte Opacity
    {
        get { return _realOpacity; }
        set
        {
            _displayedOpacity = _realOpacity = value;

            if (_cascadeOpacityEnabled)
            {
                byte parentOpacity = 255;
                var pParent = m_pParent as ICCRGBAProtocol;
                if (pParent != null && pParent.CascadeOpacityEnabled)
                {
                    parentOpacity = pParent.DisplayedOpacity;
                }
                UpdateDisplayedOpacity(parentOpacity);
            }
        }
    }

    public override byte DisplayedOpacity
    {
        get { return _displayedOpacity; }
    }

    public override bool IsOpacityModifyRGB
    {
        get { return false; }
        set { }
    }
                        
    public override bool CascadeColorEnabled
    {
        get { return _cascadeColorEnabled; }
        set { _cascadeColorEnabled = value; }
    }

    public override bool CascadeOpacityEnabled
    {
        get { return _cascadeOpacityEnabled; }
        set { _cascadeOpacityEnabled = value; }
    }

    public CCLayerRGBA()
    {
        _displayedOpacity = 255;
        _realOpacity = 255;
        _displayedColor = CCTypes.CCWhite;
        _realColor = CCTypes.CCWhite;
        
        _cascadeColorEnabled = false;
        _cascadeOpacityEnabled = false;
    }

    public override bool Init()
    {
        base.Init();

        _displayedOpacity = _realOpacity = 255;
        _displayedColor = _realColor = CCTypes.CCWhite;
        CascadeOpacityEnabled = false;
        CascadeColorEnabled = false;

        return true;
    }

    public override void UpdateDisplayedColor(CCColor3B parentColor)
    {
        _displayedColor.R = (byte) (_realColor.R * parentColor.R / 255.0f);
        _displayedColor.G = (byte) (_realColor.G * parentColor.G / 255.0f);
        _displayedColor.B = (byte) (_realColor.B * parentColor.B / 255.0f);

        if (_cascadeColorEnabled)
        {
            if (_cascadeOpacityEnabled && m_pChildren != null)
            {
                for (int i = 0, count = m_pChildren.count; i < count; i++)
                {
                    var item = m_pChildren.Elements[i] as ICCRGBAProtocol;
                    if (item != null)
                    {
                        item.UpdateDisplayedColor(_displayedColor);
                    }
                }
            }
        }
    }

    public override void UpdateDisplayedOpacity(byte parentOpacity)
    {
        _displayedOpacity = (byte) (_realOpacity * parentOpacity / 255.0f);

        if (_cascadeOpacityEnabled && m_pChildren != null)
        {
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                var item = m_pChildren.Elements[i] as ICCRGBAProtocol;
                if (item != null)
                {
                    item.UpdateDisplayedOpacity(_displayedOpacity);
                }
            }
        }
    }
}

