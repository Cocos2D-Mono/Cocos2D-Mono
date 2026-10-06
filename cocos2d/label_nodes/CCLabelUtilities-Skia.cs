#if DESKTOPGL
using System;
using System.IO;
using SkiaSharp;

namespace Cocos2D;

internal static partial class CCLabelUtilities
{
    private static SKBitmap _bitmapSkia;
    private static SKCanvas _canvas;
    private static SKPaint _paint;
    private static SKFont _font;

    #if (LINUX || MACOS)
    internal static CCTexture2D CreateNativeLabel(string text, CCSize dimensions, CCTextAlignment hAlignment,
        CCVerticalTextAlignment vAlignment, string fontName,
        float fontSize, CCColor4B textColor)
    {
        return CreateNativeLabelSkia(text, dimensions, hAlignment, vAlignment, fontName, fontSize, textColor);
    }

    static void CreateBitmap(int width, int height)
    {
        CreateBitmapSkia(width, height);
    }

    static SKTypeface CreateFont(string familyName, float emSize)
    {
        return CreateFontSkia(familyName, emSize);
    }

    static Stream SaveToStream()
    {
        return SaveToStreamSkia();
    }
#endif
    internal static CCTexture2D CreateNativeLabelSkia(string text, CCSize dimensions, CCTextAlignment hAlignment,
        CCVerticalTextAlignment vAlignment, string fontName,
        float fontSize, CCColor4B textColor)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new CCTexture2D();
        }

        // fontName/fontSize are currently NOT applied on the Skia path - the static
        // Sans-serif/12 font below is what measures and draws (tracked as C2D-236).
        // The dead CreateFontSkia call that used to sit here leaked a typeface per label.

        if (dimensions.Equals(CCSize.Zero))
        {
            CreateBitmapSkia(1, 1);

            _font.MeasureText(text, out var size);

            dimensions.Width = size.Width;
            dimensions.Height = size.Height;
        }

        CreateBitmapSkia((int)dimensions.Width, (int)dimensions.Height);

        var alignment = SKTextAlign.Left;

        switch (hAlignment)
        {
            case CCTextAlignment.Center:
                alignment = SKTextAlign.Center;
                break;
            case CCTextAlignment.Right:
                alignment = SKTextAlign.Right;
                break;
        }

        var lineAlignment = 0; // Top by default

        switch (vAlignment)
        {
            case CCVerticalTextAlignment.Center:
                lineAlignment = (int)(dimensions.Height / 2);
                break;
            case CCVerticalTextAlignment.Bottom:
                lineAlignment = (int)dimensions.Height;
                break;
        }

        _paint.Color = new SKColor(textColor.R, textColor.G, textColor.B, textColor.A);

        _canvas.Clear(SKColors.Transparent);
        _canvas.DrawText(text, 0, lineAlignment, alignment, _font, _paint);

        var texture = new CCTexture2D();
        texture.InitWithStream(SaveToStreamSkia(), Microsoft.Xna.Framework.Graphics.SurfaceFormat.Bgra4444);

        return texture;
    }

    static void CreateBitmapSkia(int width, int height)
    {
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        // Dispose before reassigning: these wrap native Skia handles, and this
        // method runs twice per label (1x1 for measurement, then final size).
        _canvas?.Dispose();
        _bitmapSkia?.Dispose();
        _bitmapSkia = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmapSkia);

        // SkiaSharp 3.x splits text state out of SKPaint: the font (typeface + size)
        // draws the glyphs, the paint keeps color/antialiasing. FilterQuality is
        // dropped outright - it only ever affected bitmap sampling, never text.
        // Neither depends on the bitmap dimensions, so they initialize once.
        if (_paint == null)
        {
            _paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White
            };
            _font = new SKFont(SKTypeface.FromFamilyName("Sans-serif"), 12);
        }
    }

    static SKTypeface CreateFontSkia(string familyName, float emSize)
    {
        return SKTypeface.FromFamilyName(familyName);
    }

    static Stream SaveToStreamSkia()
    {
        var image = SKImage.FromBitmap(_bitmapSkia);
        var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;

        return stream;
    }
}
#endif