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

    public static void SetTTFAtlasTextureSize(float width, float height)
    {
        if (width > 0 && height > 0)
        {
            _atlasTextureSize = new CCSize(width, height);
        }
    }

    [MemberNotNull(nameof(m_pTexture), nameof(m_pData))]
    public static void InitializeTTFAtlas(int width, int height)
    {
        _width = width;
        _height = height;
        _depth = 4;

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
                byte* pBase = GetBitmapData(s, out stride);

                int minX = w;
                int maxX = 0;
                int minY = h;
                int maxY = 0;

                for (int y = 0; y < h; y++)
                {
                    var row = (int*)(pBase + y * stride);

                    for (int x = 0; x < w; x++)
                    {
                        if (row[x] != 0)
                        {
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                        }
                    }
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

                var region = AllocateRegion(w, h);

                if (region.x >= 0)
                {
                    SetRegionData(region, data, w);

                    var info = GetKerningInfo(chars[i]);
                    var regionX = region.x + 0.015f;
                    var regionWidth = region.width;
#if ANDROID
                    regionX = region.x + 0.55f;
#endif

#if IOS
                    regionX = region.x + 0.33f;
                    regionWidth -= 1;
#endif

                    var fontDef = new CCBMFontConfiguration.CCBMFontDef()
                    {
                        charID = chars[i],
                        rect = new CCRect(regionX, region.y, regionWidth, region.height),
                        xOffset = minX,
                        yOffset = minY,
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

    private static CCRawList<ivec3> _nodes = new CCRawList<ivec3>();
    private static int _used;
    private static int _width;
    private static int _height;
    private static int _depth;
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
