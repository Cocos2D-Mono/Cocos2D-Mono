#if DESKTOPGL 
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SkiaSharp;


namespace Cocos2D;

public partial class CCLabel
{
    private static SKTypeface? _defaultTypeface;
    private static SKTypeface? _currentTypeface;
    private static float _currentFontSize;

    private static SKBitmap? _bitmapSkia;
    private static SKCanvas? _canvas;
    private static Dictionary<char, KerningInfo> _kerningInfo = new Dictionary<char, KerningInfo>();
    private static Dictionary<string, SKTypeface> _typefaceCache = new Dictionary<string, SKTypeface>();

    // A label's font size is in points, as on Windows, where GDI+ and DirectWrite draw a point as
    // 96/72 of a pixel at the usual 96 DPI. Skia's sizes are in pixels.
    private const float SkiaPixelsPerPoint = 96f / 72f;

    // GDI+ draws a string about a sixth of an em to the right of where it's asked to, so on Windows
    // each glyph sits that far right of where it advances from. Skia draws them as far in, which
    // also leaves room for a glyph that reaches back past where it starts, like j.
    private static float SkiaLeftMargin => _currentFontSize / 6;

#if (LINUX || MACOS)

    private string CreateFont(string fontName, float fontSize, CCRawList<char> charset)
    {
        return CreateFontSkia(fontName, fontSize, charset);
    }

    private static void GetKerningInfo(CCRawList<char> charset)
    {
        GetKerningInfoSkia(charset);
    }

    private float GetFontHeight()
    {
        return GetFontHeightSkia();
    }

    private CCSize GetMeasureString(string text)
    {
        return GetMeasureStringSkia(text);
    }

    private void CreateBitmap(int width, int height)
    {
        CreateBitmapSkia(width, height);
    }

    private unsafe byte* GetBitmapData(string s, out int stride)
    {
       return GetBitmapDataSkia(s, out stride);
    }

    private KerningInfo GetKerningInfo(char ch)
    {
        return GetKerningInfoSkia(ch);
    }
#endif
    private string CreateFontSkia(string fontName, float fontSize, CCRawList<char> charset)
    {
        if (_defaultTypeface == null)
        {
            _defaultTypeface = SKTypeface.Default;
        }

        if (!_typefaceCache.TryGetValue(fontName, out var typeface))
        {
            var ext = Path.GetExtension(fontName);

            _currentTypeface = _defaultTypeface;

            if (!string.IsNullOrEmpty(ext) && ext.ToLower() == ".ttf")
            {
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var contentPath = Path.Combine(appPath, CCContentManager.SharedContentManager.RootDirectory);
                var fontPath = Path.Combine(contentPath, fontName);

                if (File.Exists(fontPath))
                {
                    try
                    {
                        typeface = SKTypeface.FromFile(fontPath);
                        _currentTypeface = typeface;
                    }
                    catch
                    {
                        _currentTypeface = _defaultTypeface;
                    }
                }
            }
            else
            {
                _currentTypeface = SKTypeface.FromFamilyName(fontName) ?? _defaultTypeface;
            }

            _typefaceCache.Add(fontName, _currentTypeface);
        }
        else
        {
            _currentTypeface = typeface;
        }

        _currentFontSize = fontSize * SkiaPixelsPerPoint;

        GetKerningInfoSkia(charset);

        CreateBitmapSkia(1, 1);

        return _currentTypeface.FamilyName;
    }

    private static void GetKerningInfoSkia(CCRawList<char> charset)
    {
        _kerningInfo.Clear();

        using var font = new SKFont(_currentTypeface, _currentFontSize);

        foreach (var ch in charset)
        {
            if (!_kerningInfo.ContainsKey(ch))
            {
                // CCLabel uses only the sum, as the glyph's advance, so all of it goes in B, as
                // on Android. It's rounded to the nearest pixel, since CCLabel rounds the sum up,
                // which would add half a pixel a glyph on average. A glyph that doesn't advance,
                // like a combining accent, needs an entry too.
                _kerningInfo[ch] = new KerningInfo
                {
                    A = 0,
                    B = MathF.Round(font.MeasureText(ch.ToString())),
                    C = 0
                };
            }
        }
    }

    // The line height, which GDI+'s Font.GetHeight gives on Windows: ascent, descent and leading.
    private float GetFontHeightSkia()
    {
        using var font = new SKFont(_currentTypeface, _currentFontSize);
        return font.Spacing;
    }

    // The bitmap GetBitmapDataSkia draws a glyph into, which CCLabel searches for the glyph's
    // pixels: wide enough for the margin, the glyph and its advance, and tall enough for the
    // glyph below the top of its line.
    private CCSize GetMeasureStringSkia(string text)
    {
        using var font = new SKFont(_currentTypeface, _currentFontSize);

        var advance = font.MeasureText(text, out var bounds);
        return new CCSize(
            SkiaLeftMargin + Math.Max(advance, bounds.Right),
            -font.Metrics.Ascent + Math.Max(font.Metrics.Descent, bounds.Bottom));
    }

    [MemberNotNull(nameof(_bitmapSkia), nameof(_canvas))]
    private void CreateBitmapSkia(int width, int height)
    {
        if (_bitmapSkia == null || _canvas == null || (_bitmapSkia.Width < width || _bitmapSkia.Height < height))
        {
            _canvas?.Dispose();
            _bitmapSkia?.Dispose();

            _bitmapSkia = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _canvas = new SKCanvas(_bitmapSkia);
        }
    }

    private unsafe byte* GetBitmapDataSkia(string s, out int stride)
    {
        var size = GetMeasureStringSkia(s);

        var w = (int)Math.Ceiling(size.Width + 2);
        var h = (int)Math.Ceiling(size.Height + 2);

        CreateBitmapSkia(w, h);

        _canvas.Clear(SKColors.Transparent);

        // Glyph state (typeface + size) lives on the SKFont below; the paint only
        // carries color and antialiasing on the modern DrawText overload.
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White
        };


        using var font = new SKFont(_currentTypeface, _currentFontSize);

        // With the top of the line at the top of the bitmap, as GDI+ draws it, so the baseline is
        // the ascent below it. CCLabel takes how far the glyph's pixels start from the top as the
        // glyph's offset from the top of its line.
        _canvas.DrawText(s, SkiaLeftMargin, -font.Metrics.Ascent, SKTextAlign.Left, font, paint);

        stride = _bitmapSkia.RowBytes;

        fixed (byte* ptr = _bitmapSkia.GetPixelSpan())
        {
            return ptr;
        }
    }

    private KerningInfo GetKerningInfoSkia(char ch)
    {
        return _kerningInfo[ch];
    }
}
#endif