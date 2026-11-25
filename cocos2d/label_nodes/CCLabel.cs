using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using cocos2d.label_nodes;
using Microsoft.Xna.Framework.Graphics;
#if IOS
using Foundation;
using UIKit;
#endif

namespace Cocos2D;

public partial class CCLabel : CCLabelBMFont
{
    private struct KerningInfo
    {
        /// <summary>Specifies the A spacing of the character. The A spacing is the distance to add to the current
        /// position before drawing the character glyph.</summary>
        public float A;

        /// <summary>Specifies the B spacing of the character. The B spacing is the width of the drawn portion of
        /// the character glyph.</summary>
        public float B;

        /// <summary>Specifies the C spacing of the character. The C spacing is the distance to add to the current
        /// position to provide white space to the right of the character glyph.</summary>
        public float C;
    }

    public static CCTexture2D? m_pTexture;
    private static CCSize _atlasTextureSize = new CCSize(0, 0);

    // How far right and down GetBitmapData moved the glyph it just drew, to keep ink that reaches
    // back past where the glyph starts, or above the top of its line, inside the bitmap.
    // InitializeFont takes it off the glyph's offsets, which can then be negative, as a .fnt
    // file's can. The Android and Apple backends don't move glyphs.
    private static int _glyphShiftX;
    private static int _glyphShiftY;

    protected static bool m_bTextureDirty = true;

    protected string m_FontName = "arial";
    protected float m_FontSize;
    protected int m_FontSpacing;
    protected bool m_bFontDirty;

    public string SystemFont
    {
        get { return m_FontName; }
        set
        {
            if (m_FontName != value)
            {
                m_FontName = value;
                m_pConfiguration = InitializeFont(m_FontName, m_FontSize, Text);
                m_bFontDirty = true;
            }
        }
    }

    public float SystemFontSize
    {
        get { return m_FontSize; }
        set
        {
            m_FontSize = value;
            m_pConfiguration = InitializeFont(m_FontName, m_FontSize, Text);
            m_bFontDirty = true;
        }
    }

    public int SystemFontSpacing
    {
        get { return m_FontSpacing; }
        set
        {
            if (m_FontSpacing != value)
            {
                m_FontSpacing = value;
                InitializeFont(m_FontName, m_FontSize, Text);
                m_bFontDirty = true;
            }
        }
    }

    public override string Text
    {
        get { return base.Text; }
        set
        {
            if (m_sInitialString != value)
            {
                m_pConfiguration = InitializeFont(m_FontName, m_FontSize, value);
                m_bFontDirty = true;
                base.Text = value;
            }

            UpdateLabel();
        }
    }

    /// <summary>
    /// Sets the size, in pixels, of the atlas that every <see cref="CCLabel"/> draws its glyphs
    /// into. The atlas takes the size the next time it's made: for the first label, when it fills
    /// up and starts again, and after <see cref="CCLabelBMFont.PurgeCachedData"/>. Until a size is
    /// set, the label that makes the atlas picks one from its font size: 1024 × 1024, 2048 × 1024
    /// for a size of 80 or more, or 2048 × 2048 for 105 or more. The call does nothing unless both
    /// sizes are positive.
    /// </summary>
    /// <remarks>
    /// On iOS, Mac Catalyst and tvOS, make both sizes powers of two no bigger than 2048, such as
    /// 1024 or 2048. Those platforms' GPUs carry texture coordinates as 16-bit floats, which place
    /// every glyph exactly only at those sizes. Past 2048, some glyphs are drawn a pixel off. At a
    /// size that isn't a power of two, glyphs can be drawn a fraction of a pixel off, which blurs
    /// them.
    /// </remarks>
    public static void SetTTFAtlasTextureSize(float width, float height)
    {
        if (width > 0 && height > 0)
        {
            _atlasTextureSize = new CCSize(width, height);
        }
    }

    /// <summary>
    /// Replaces the atlas that every <see cref="CCLabel"/> draws its glyphs into with a new, empty
    /// one of this size, in pixels, and disposes the old atlas's texture. To choose the size of
    /// every atlas the labels make, call <see cref="SetTTFAtlasTextureSize"/> instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It doesn't clear the glyphs that labels have already placed. Their places now point into the
    /// new, empty atlas, so labels made before the call draw blank or wrong text. And while no
    /// label font is cached, as before the first label, the next label replaces this atlas with
    /// one of its own.
    /// </para>
    /// <para>
    /// On iOS, Mac Catalyst and tvOS, make both sizes powers of two no bigger than 2048, as for
    /// <see cref="SetTTFAtlasTextureSize"/>.
    /// </para>
    /// </remarks>
    [MemberNotNull(nameof(m_pTexture), nameof(m_pData))]
    public static void InitializeTTFAtlas(int width, int height)
    {
        _width = width;
        _height = height;

        m_pTexture?.Dispose();
        m_pTexture = new CCTexture2D();
        m_pData = new int[width * height];

        _nodes.Clear();
        _nodes.Add(new ivec3() { x = 1, y = 1, z = _width - 2 });

        m_pTexture.InitWithRawData(m_pData, SurfaceFormat.Color, _width, _height, true);
    }

    public CCLabel()
    {
    }

    public CCLabel(string text, float fontSize) :
        this(text, "arial", fontSize, CCSize.Zero, CCTextAlignment.Left, CCVerticalTextAlignment.Top)
    {
    }

    public CCLabel(string text, string fontName, float fontSize) :
        this(text, fontName, fontSize, CCSize.Zero, CCTextAlignment.Left, CCVerticalTextAlignment.Top)
    {
    }

    public CCLabel(string text, string fontName, float fontSize, CCTextAlignment hAlignment) :
        this(text, fontName, fontSize, CCSize.Zero, hAlignment, CCVerticalTextAlignment.Top)
    {
    }

    public CCLabel(string text, string fontName, float fontSize, CCTextAlignment hAlignment,
        CCVerticalTextAlignment vAlignment) :
        this(text, fontName, fontSize, CCSize.Zero, hAlignment, vAlignment)
    {
    }

    public CCLabel(string text, string fontName, float fontSize, CCSize dimensions) :
        this(text, fontName, fontSize, dimensions, CCTextAlignment.Left, CCVerticalTextAlignment.Top)
    {
    }

    public CCLabel(string text, string fontName, float fontSize, CCSize dimensions, CCTextAlignment hAlignment) :
        this(text, fontName, fontSize, dimensions, hAlignment, CCVerticalTextAlignment.Top)
    {
    }

    public CCLabel(string text, string fontName, float fontSize, CCSize dimensions, CCTextAlignment hAlignment,
        CCVerticalTextAlignment vAlignment)
    {
        InitWithString(text, fontName, fontSize, dimensions, hAlignment, vAlignment);
    }

    public bool InitWithString(string text, string fontName, float fontSize)
    {
        return InitWithString(text, fontName, fontSize, CCSize.Zero, CCTextAlignment.Left,
            CCVerticalTextAlignment.Top);
    }

    public bool InitWithString(string text, string fontName, float fontSize, CCSize dimensions,
        CCTextAlignment hAlignment)
    {
        return InitWithString(text, fontName, fontSize, dimensions, hAlignment, CCVerticalTextAlignment.Top);
    }

    public bool InitWithString(string text, string fontName, float fontSize, CCSize dimensions,
        CCTextAlignment hAlignment, CCVerticalTextAlignment vAlignment)
    {
        InitializeFont(fontName, fontSize, text);
        m_FontName = fontName;
        m_FontSize = fontSize;

        var fontKey = GetFontKey(fontName, fontSize);

        return base.InitWithString(text, fontKey, dimensions.PointsToPixels(), hAlignment,
            vAlignment, CCPoint.Zero, m_pTexture);
    }


    private CCBMFontConfiguration InitializeFont(string fontName, float fontSize, string charset, bool retry = false)
    {
        var fontKey = GetFontKey(fontName, fontSize);

        if (m_pData == null || s_pConfigurations.Count == 0)
        {
            if (_atlasTextureSize.Width > 0 && _atlasTextureSize.Height > 0)
            {
                InitializeTTFAtlas((int)_atlasTextureSize.Width, (int)_atlasTextureSize.Height);
            }
            else if (fontSize >= 105)
            {
                InitializeTTFAtlas(2048, 2048);
            }
            else if (fontSize >= 80)
            {
                InitializeTTFAtlas(2048, 1024);
            }
            else
            {
                InitializeTTFAtlas(1024, 1024);
            }
        }

        if (Texture.IsDisposed && retry)
        {
            Debug.Assert(m_pTexture != null, "InitializeTTFAtlas creates the texture along with the data");
            Texture = m_pTexture;
        }


        if (string.IsNullOrEmpty(charset))
        {
            charset = " ";
        }

        var chars = new CCRawList<char>();

        CCBMFontConfiguration? fontConfig;

        if (!s_pConfigurations.TryGetValue(fontKey, out fontConfig))
        {
            fontConfig = new CCBMFontConfiguration();
            s_pConfigurations.Add(fontKey, fontConfig);
        }

        for (int i = 0; i < charset.Length; i++)
        {
            var ch = charset[i];
            if (!fontConfig.m_pFontDefDictionary.ContainsKey(ch) && chars.IndexOf(ch) == -1)
            {
                chars.Add(ch);
            }
            else if (fontConfig.m_pFontDefDictionary.ContainsKey(ch))
            {
                var fontDef = fontConfig.m_pFontDefDictionary[ch];
                fontDef.xAdvance = fontDef.xKern + m_FontSpacing;
            }
        }

        if (chars.Count == 0)
        {
            return fontConfig;
        }

        CreateFont(fontName, fontSize * CCMacros.CCContentScaleFactor(), chars);

        fontConfig.m_nCommonHeight = (int)Math.Ceiling(GetFontHeight());

        int[]? data = null;

        for (int i = 0; i < chars.Count; i++)
        {
            var s = chars[i].ToString();

            var charSize = GetMeasureString(s);

            int w = (int)Math.Ceiling(charSize.Width + 2);
            int h = (int)Math.Ceiling(charSize.Height + 2);

            if (data == null || data.Length < (w * h))
            {
                data = new int[w * h];
            }

            unsafe
            {
                int stride;
                _glyphShiftX = 0;
                _glyphShiftY = 0;
                byte* pBase = GetBitmapData(s, out stride);

                int minX = w;
                int maxX = 0;
                int minY = h;
                int maxY = 0;
                bool hasPixels = false;

                for (int y = 0; y < h; y++)
                {
                    var row = (int*)(pBase + y * stride);

                    for (int x = 0; x < w; x++)
                    {
                        if (row[x] != 0)
                        {
                            hasPixels = true;
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                        }
                    }
                }

                // A glyph with no pixels, like a space, takes no room in the atlas. As a space in a
                // .fnt file does, it has an empty rect, so it draws nothing and only advances. With
                // a region of its own, it got its one pixel from data, which still held the previous
                // glyph's pixels, and drew that pixel as a dot.
                if (!hasPixels)
                {
                    var info = GetKerningInfo(chars[i]);
                    fontConfig.CharacterSet.Add(chars[i]);
                    fontConfig.m_pFontDefDictionary.Add(chars[i], new CCBMFontConfiguration.CCBMFontDef()
                    {
                        charID = chars[i],
                        rect = CCRect.Zero,
                        xKern = (int)Math.Ceiling(info.A + info.B + info.C),
                        xAdvance = (int)Math.Ceiling(info.A + info.B + info.C) + m_FontSpacing
                    });
                    continue;
                }

                w = Math.Max(maxX - minX + 1, 1);
                h = Math.Max(maxY - minY + 1, 1);

                int index = 0;
                for (int y = minY; y <= maxY; y++)
                {
                    var row = (int*)(pBase + y * stride);
                    for (int x = minX; x <= maxX; x++)
                    {
                        data[index] = row[x];
                        index++;
                    }
                }

                // The atlas samples linearly, so a glyph drawn between pixels takes in the texels
                // just outside its rect. Packed edge to edge, those were the next glyph's, and
                // showed as a faint line along the edge. So each glyph gets a transparent texel all
                // around it.
                var region = AllocateRegion(w + 2 * GlyphGap, h + 2 * GlyphGap);

                if (region.x >= 0)
                {
                    region = new ivec4() { x = region.x + GlyphGap, y = region.y + GlyphGap, width = w, height = h };

                    SetRegionData(region, data, w);

                    var info = GetKerningInfo(chars[i]);

                    // The rect is exactly the glyph's pixels. Every platform used to shift or trim
                    // it to hide the line that the gap now prevents, which cut into the glyph.
                    var fontDef = new CCBMFontConfiguration.CCBMFontDef()
                    {
                        charID = chars[i],
                        rect = new CCRect(region.x, region.y, region.width, region.height),
                        xOffset = minX - _glyphShiftX,
                        yOffset = minY - _glyphShiftY,
                        xKern = (int)Math.Ceiling(info.A + info.B + info.C),
                        xAdvance = (int)Math.Ceiling(info.A + info.B + info.C) + m_FontSpacing
                    };
                    fontConfig.CharacterSet.Add(chars[i]);
                    fontConfig.m_pFontDefDictionary.Add(chars[i], fontDef);
                }
                else
                {
                    if (!retry)
                    {
                        m_pData = null;
                        s_pConfigurations.Clear();
                        return InitializeFont(fontName, fontSize, charset, true);
                    }
                    CCLog.Log("Texture atlas is full");
                }
            }
        }

        m_bTextureDirty = true;

        return fontConfig;
    }

    public override void Draw()
    {
        if (m_bFontDirty)
        {
            m_pConfiguration = InitializeFont(m_FontName, m_FontSize, Text);
            m_bFontDirty = false;
        }

        if (m_pobTextureAtlas.Texture.IsDisposed)
        {
            m_pConfiguration = InitializeFont(m_FontName, m_FontSize, Text, true);
            m_bFontDirty = true;
            m_sInitialString = Text;
            m_bLabelDirty = true;

            UpdateLabel();
        }

        // There's no atlas to upload until a label sets its font.
        if (m_bTextureDirty && m_pTexture != null && m_pData != null)
        {
            m_pTexture.InitWithRawData(m_pData, SurfaceFormat.Color, _width, _height, true);

            m_bTextureDirty = false;
        }

        base.Draw();
    }


    private string GetFontKey(string fontName, float fontSize)
    {
        return String.Format("ttf-{0}-{1}", fontName, fontSize);
    }

    #region Skyline Bottom Left

    public struct ivec3
    {
        public int x;
        public int y;
        public int z;
    }

    public struct ivec4
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }

    // The transparent texels InitializeFont leaves on each side of a glyph in the atlas.
    private const int GlyphGap = 1;

    private static CCRawList<ivec3> _nodes = new CCRawList<ivec3>();
    private static int _used;
    private static int _width;
    private static int _height;
    public static int[]? m_pData;

    private int Fit(int index, int width, int height)
    {
        var node = _nodes[index];

        var x = node.x;
        var y = node.y;
        var widthLeft = width;
        var i = index;

        if ((x + width) > (_width - 1))
        {
            return -1;
        }

        while (widthLeft > 0)
        {
            node = _nodes[i];

            if (node.y > y)
            {
                y = node.y;
            }

            if ((y + height) > (_height - 1))
            {
                return -1;
            }

            widthLeft -= node.z;

            ++i;
        }

        return y;
    }

    private void Merge()
    {
        var nodes = _nodes.Elements;
        for (int i = 0, count = _nodes.Count; i < count - 1; ++i)
        {
            if (nodes[i].y == nodes[i + 1].y)
            {
                nodes[i].z += nodes[i + 1].z;
                _nodes.RemoveAt(i + 1);
                --count;
                --i;
            }
        }
    }

    public ivec4 AllocateRegion(int width, int height)
    {
        ivec3 node, prev;
        ivec4 region = new ivec4() { x = 0, y = 0, width = width, height = height };
        int i;

        int bestHeight = int.MaxValue;
        int bestIndex = -1;
        int bestWidth = int.MaxValue;

        for (i = 0; i < _nodes.Count; ++i)
        {
            int y = Fit(i, width, height);

            if (y >= 0)
            {
                node = _nodes[i];
                if (((y + height) < bestHeight) || (((y + height) == bestHeight) && (node.z < bestWidth)))
                {
                    bestHeight = y + height;
                    bestIndex = i;
                    bestWidth = node.z;
                    region.x = node.x;
                    region.y = y;
                }
            }
        }

        if (bestIndex == -1)
        {
            region.x = -1;
            region.y = -1;
            region.width = 0;
            region.height = 0;
            return region;
        }

        //New node
        node.x = region.x;
        node.y = region.y + height;
        node.z = width;
        _nodes.Insert(bestIndex, node);

        for (i = bestIndex + 1; i < _nodes.Count; ++i)
        {
            node = _nodes[i];
            prev = _nodes[i - 1];

            if (node.x < (prev.x + prev.z))
            {
                int shrink = prev.x + prev.z - node.x;
                node.x += shrink;
                node.z -= shrink;
                if (node.z <= 0)
                {
                    _nodes.RemoveAt(i);
                    --i;
                }
                else
                {
                    _nodes[i] = node;
                    break;
                }
            }
            else
            {
                break;
            }
        }

        Merge();

        _used += width * height;

        return region;
    }

    public void SetRegionData(ivec4 region, int[] data, int stride)
    {
        var x = region.x;
        var y = region.y;
        var width = region.width;
        var height = region.height;

        Debug.Assert(m_pData != null, "InitializeTTFAtlas creates the atlas data");
        Debug.Assert(x > 0);
        Debug.Assert(y > 0);
        Debug.Assert(x < (_width - 1));
        Debug.Assert((x + width) <= (_width - 1));
        Debug.Assert(y < (_height - 1));
        Debug.Assert((y + height) <= (_height - 1));

        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; j++)
            {
                var b = (byte)((data[i * stride + j] & 0xFF0000) >> 16);
                m_pData[((y + i) * _width + x) + j] = b << 24 | b << 16 | b << 8 | b;
            }
            //    Array.Copy(data, (i * stride), m_pData, ((y + i) * _width + x), width);
            //                Buffer.BlockCopy(data, (i * stride), m_pData, ((y + i) * _width + x) * _depth, width * _depth);
        }
    }

    #endregion
}
