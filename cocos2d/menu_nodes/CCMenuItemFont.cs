#nullable disable
using System;

namespace Cocos2D;

public class CCMenuItemFont : CCMenuItemLabel
{
    protected string m_strFontName;
    protected uint m_uFontSize;

    public CCMenuItemFont (string value) : this(value, null)
    { }
    
		public CCMenuItemFont (string value, Action<CCMenuItem> selector)
    {
        InitWithString(value, _fontName, (int)_fontSize, selector);
    }

    public CCMenuItemFont(string value, string fontName, Action<CCMenuItem> selector)
    {
        InitWithString(value, fontName, (int)_fontSize, selector);
    }

    public CCMenuItemFont(string value, string fontName, int fontSize, Action<CCMenuItem> selector)
    {
        InitWithString(value, fontName, fontSize, selector);
    }

    /// <summary>
    /// Sets the font size for all items.
    /// </summary>
    public static uint FontSize
    {
        get { return _fontSize; }
        set { _fontSize = value; }
    }

    /// <summary>
    /// Sets the font name for all items.
    /// </summary>
    public static string FontName
    {
        get { return _fontName; }
        set { _fontName = value; }
    }

    /// <summary>
    /// Sets the font size for this menu item
    /// </summary>
    public uint ItemFontSize
    {
        set
        {
            m_uFontSize = value;
            RecreateLabel();
        }
        get { return m_uFontSize; }
    }

    /// <summary>
    /// Sets the name of the font for this item.
    /// </summary>
    public string ItemFontName
    {
        set
        {
            m_strFontName = value;
            RecreateLabel();
        }
        get { return m_strFontName; }
    }

    public override CCColor3B Color
    {
        set
        {
            base.Color = value;
            RecreateLabel();
        }
    }

		protected virtual bool InitWithString(string value, string fontName, int fontSize, Action<CCMenuItem> selector)
    {
        //CCAssert( value != NULL && strlen(value) != 0, "Value length must be greater than 0");

        m_strFontName = fontName;
        m_uFontSize = (uint)fontSize;

        CCLabelTTF label = new CCLabelTTF(value, m_strFontName, m_uFontSize);
        base.InitWithLabel(label, selector);
        return true;
    }

    protected virtual void RecreateLabel()
    {
        CCLabelTTF label = new CCLabelTTF((m_pLabel as ICCLabelProtocol).Text, m_strFontName, m_uFontSize);
        label.Color = Color;
        Label = label;
    }
}