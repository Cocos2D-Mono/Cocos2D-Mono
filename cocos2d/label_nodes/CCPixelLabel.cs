#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cocos2D;

/// <summary>
/// A label optimized for frequent text updates. Pre-caches individual character
/// textures at construction time, then repositions sprites on text changes —
/// no texture regeneration. Ideal for scores, timers, health counters, and
/// other HUD elements that change every frame.
/// </summary>
public class CCPixelLabel : CCNode
{
    private static readonly Dictionary<string, Dictionary<char, CCTexture2D>> s_fontCache
        = new Dictionary<string, Dictionary<char, CCTexture2D>>();
    private static readonly Dictionary<string, Dictionary<char, float>> s_widthCache
        = new Dictionary<string, Dictionary<char, float>>();
    private static readonly Dictionary<string, float> s_heightCache
        = new Dictionary<string, float>();

    private Dictionary<char, CCTexture2D> _charTextures;
    private Dictionary<char, float> _charWidths;
    private float _charHeight;

    private CCSprite[] _glyphs;
    private int _maxChars;
    private string _text = "";
    private CCTextAlignment _alignment;
    private bool _antialiased;
    private string _fontName;
    private float _fontSize;
    private string _cacheKey;

    private const string DefaultCharSet =
        "0123456789+-/:*x[](){}ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?@#$%&=<>\"'`~^_;\\| ";

    /// <summary>
    /// Gets or sets the displayed text. Changing this repositions existing
    /// glyph sprites without regenerating any textures.
    /// </summary>
    public string Text
    {
        get { return _text; }
        set
        {
            string newText = value ?? "";
            if (_text == newText) return;
            _text = newText;
            CacheCharacters(_text);
            Layout();
        }
    }

    /// <summary>
    /// Gets or sets text alignment (Left, Center, Right).
    /// </summary>
    public CCTextAlignment Alignment
    {
        get { return _alignment; }
        set
        {
            if (_alignment == value) return;
            _alignment = value;
            Layout();
        }
    }

    /// <summary>
    /// Gets or sets the color applied to all glyph sprites.
    /// </summary>
    public override CCColor3B Color
    {
        get { return base.Color; }
        set
        {
            base.Color = value;
            for (int i = 0; i < _glyphs.Length; i++)
                _glyphs[i].Color = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity applied to all glyph sprites.
    /// </summary>
    public override byte Opacity
    {
        get { return base.Opacity; }
        set
        {
            base.Opacity = value;
            for (int i = 0; i < _glyphs.Length; i++)
                _glyphs[i].Opacity = value;
        }
    }

    /// <summary>
    /// Gets the font name used by this label.
    /// </summary>
    public string FontName
    {
        get { return _fontName; }
    }

    /// <summary>
    /// Gets the font size used by this label.
    /// </summary>
    public float FontSize
    {
        get { return _fontSize; }
    }

    /// <summary>
    /// Gets the measured height of a single character in this font.
    /// </summary>
    public float CharHeight
    {
        get { return _charHeight; }
    }

    /// <summary>
    /// Gets or sets whether glyph sprites use antialiased (linear) filtering.
    /// When false, uses point filtering for pixel-perfect rendering.
    /// </summary>
    public bool IsAntialiased
    {
        get { return _antialiased; }
        set
        {
            if (_antialiased == value) return;

            _antialiased = value;
            _cacheKey = BuildCacheKey(_fontName, _fontSize, value);

            // Switch to a separate texture cache for this filtering mode
            // so shared textures are not mutated across labels
            Dictionary<char, CCTexture2D> textures;
            if (!s_fontCache.TryGetValue(_cacheKey, out textures))
            {
                textures = new Dictionary<char, CCTexture2D>();
                var widths = new Dictionary<char, float>();
                float maxH = 0;

                foreach (char c in DefaultCharSet)
                {
                    if (c == ' ') continue;

                    var label = new CCLabelTTF(c.ToString(), _fontName, _fontSize);
                    if (label.Texture != null)
                    {
                        label.Texture.IsAntialiased = value;
                        textures[c] = label.Texture;
                        float w = label.Texture.ContentSize.Width;
                        float h = label.Texture.ContentSize.Height;
                        widths[c] = w;
                        if (h > maxH) maxH = h;
                    }
                }

                s_fontCache[_cacheKey] = textures;
                s_widthCache[_cacheKey] = widths;
                s_heightCache[_cacheKey] = maxH;
            }

            _charTextures = textures;
            _charWidths = s_widthCache[_cacheKey];
            _charHeight = s_heightCache[_cacheKey];

            Layout();
        }
    }

    /// <summary>
    /// Creates a new CCPixelLabel.
    /// </summary>
    /// <param name="text">Initial text to display.</param>
    /// <param name="fontName">System font name (e.g. "arial").</param>
    /// <param name="fontSize">Font size in points.</param>
    /// <param name="alignment">Text alignment.</param>
    /// <param name="antialiased">Whether to use antialiased filtering.</param>
    /// <param name="maxChars">Maximum number of visible characters. Pre-allocates this many sprites.</param>
    public CCPixelLabel(string text, string fontName, float fontSize,
        CCTextAlignment alignment = CCTextAlignment.Left,
        bool antialiased = false, int maxChars = 32)
    {
        _fontName = fontName;
        _fontSize = fontSize;
        _alignment = alignment;
        _antialiased = antialiased;
        _maxChars = maxChars;
        _cacheKey = BuildCacheKey(fontName, fontSize, antialiased);

        Dictionary<char, CCTexture2D> textures;
        if (!s_fontCache.TryGetValue(_cacheKey, out textures))
        {
            textures = new Dictionary<char, CCTexture2D>();
            var widths = new Dictionary<char, float>();
            float maxH = 0;

            foreach (char c in DefaultCharSet)
            {
                if (c == ' ') continue;

                var label = new CCLabelTTF(c.ToString(), fontName, fontSize);
                if (label.Texture != null)
                {
                    label.Texture.IsAntialiased = antialiased;
                    textures[c] = label.Texture;
                    float w = label.Texture.ContentSize.Width;
                    float h = label.Texture.ContentSize.Height;
                    widths[c] = w;
                    if (h > maxH) maxH = h;
                }
            }

            s_fontCache[_cacheKey] = textures;
            s_widthCache[_cacheKey] = widths;
            s_heightCache[_cacheKey] = maxH;
        }

        _charTextures = textures;
        _charWidths = s_widthCache[_cacheKey];
        _charHeight = s_heightCache[_cacheKey];

        AllocateGlyphs(maxChars);

        _text = text ?? "";
        if (_text.Length > 0)
        {
            CacheCharacters(_text);
            Layout();
        }
    }

    /// <summary>
    /// Creates a new CCPixelLabel with default settings.
    /// </summary>
    /// <param name="text">Initial text to display.</param>
    /// <param name="fontName">System font name.</param>
    /// <param name="fontSize">Font size in points.</param>
    public CCPixelLabel(string text, string fontName, float fontSize)
        : this(text, fontName, fontSize, CCTextAlignment.Left, false, 32)
    {
    }

    /// <summary>
    /// Pre-caches additional characters beyond the default character set.
    /// Call this once at startup if your text will include characters not in
    /// the default set (e.g. Unicode or special symbols).
    /// </summary>
    public void CacheCharacters(string characters)
    {
        foreach (char c in characters)
        {
            if (c == ' ' || _charTextures.ContainsKey(c))
                continue;

            var label = new CCLabelTTF(c.ToString(), _fontName, _fontSize);
            if (label.Texture != null)
            {
                label.Texture.IsAntialiased = _antialiased;
                _charTextures[c] = label.Texture;
                float w = label.Texture.ContentSize.Width;
                float h = label.Texture.ContentSize.Height;
                _charWidths[c] = w;

                if (h > _charHeight)
                {
                    _charHeight = h;
                    s_heightCache[_cacheKey] = h;
                }
            }
        }
    }

    /// <summary>
    /// Grows the glyph sprite pool if the current maxChars is insufficient.
    /// </summary>
    public void SetMaxChars(int maxChars)
    {
        if (maxChars <= _maxChars) return;

        for (int i = 0; i < _glyphs.Length; i++)
        {
            RemoveChild(_glyphs[i], true);
        }

        _maxChars = maxChars;
        AllocateGlyphs(maxChars);
        Layout();
    }

    /// <summary>
    /// Measures the width of a string in points without rendering it.
    /// </summary>
    public float MeasureString(string text)
    {
        if (text == null) return 0f;

        float width = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
                width += _charHeight * 0.4f;
            else if (_charWidths.TryGetValue(c, out float w))
                width += w;
        }
        return width;
    }

    /// <summary>
    /// Clears all static font caches. Cached textures are released for GC
    /// once no live labels reference them. Call this when changing scenes
    /// or when you need to free memory from fonts that are no longer used.
    /// </summary>
    public static void PurgeCachedData()
    {
        s_fontCache.Clear();
        s_widthCache.Clear();
        s_heightCache.Clear();
    }

    private static string BuildCacheKey(string fontName, float fontSize, bool antialiased)
    {
        return fontName + "_" + fontSize.ToString("R", CultureInfo.InvariantCulture)
            + (antialiased ? "_aa" : "_px");
    }

    private void AllocateGlyphs(int count)
    {
        _glyphs = new CCSprite[count];
        for (int i = 0; i < count; i++)
        {
            _glyphs[i] = new CCSprite();
            _glyphs[i].AnchorPoint = CCPoint.Zero;
            _glyphs[i].Visible = false;
            AddChild(_glyphs[i]);
        }
    }

    private void Layout()
    {
        // First pass: measure total width across full string
        float totalWidth = 0;
        for (int i = 0; i < _text.Length; i++)
        {
            char c = _text[i];
            if (c == ' ')
                totalWidth += _charHeight * 0.4f;
            else if (_charWidths.TryGetValue(c, out float w))
                totalWidth += w;
        }

        // Alignment offset
        float offsetX = 0;
        switch (_alignment)
        {
            case CCTextAlignment.Center:
                offsetX = -totalWidth / 2f;
                break;
            case CCTextAlignment.Right:
                offsetX = -totalWidth;
                break;
        }

        float offsetY = -_charHeight;

        // Second pass: position glyphs, iterating the full string
        // and only counting rendered characters against the sprite budget
        float x = offsetX;
        int glyphIdx = 0;

        for (int i = 0; i < _text.Length; i++)
        {
            char c = _text[i];
            if (c == ' ')
            {
                x += _charHeight * 0.4f;
                continue;
            }

            if (!_charTextures.TryGetValue(c, out var tex))
                continue;

            if (glyphIdx >= _maxChars) break;

            var glyph = _glyphs[glyphIdx];
            glyph.InitWithTexture(tex);
            glyph.IsAntialiased = _antialiased;
            glyph.AnchorPoint = CCPoint.Zero;
            glyph.PositionX = x;
            glyph.PositionY = offsetY;
            glyph.Color = base.Color;
            glyph.Opacity = base.Opacity;
            glyph.Visible = true;
            glyphIdx++;

            x += _charWidths.TryGetValue(c, out float w) ? w : _charHeight * 0.5f;
        }

        // Hide unused glyphs
        for (int i = glyphIdx; i < _maxChars; i++)
            _glyphs[i].Visible = false;

        // Update content size
        ContentSize = new CCSize(totalWidth, _charHeight);
    }
}
