using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Cocos2D;
using SkiaSharp;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for CCLabel's Skia backend, which Linux and macOS use (C2D-360). It drew each
// glyph at 68% of the size it measured and spaced the glyph at. It took the size in pixels, where
// Windows takes points. It drew small glyphs outside the area CCLabel searches for their pixels,
// so they disappeared.
//
// CCLabel's public constructors build the label's texture, which needs a graphics device. So these
// call the backend's methods directly, as the package check does, and search what it draws the way
// CCLabel.InitializeFont does. The Windows build compiles the backend in too, since CCLabel hands
// Linux and macOS to it at run time, so these run on Windows as well.
public class CCLabelSkiaTests
{
    private const string FontName = "Arial";
    private const float FontSize = 40;

    [Fact]
    public void DrawsACapitalAtTheSizeInPoints()
    {
        var skia = new Backend("Skia", FontName, FontSize, "H");
        using SKFont font = skia.FontInPixels(FontSize);

        Glyph h = skia.Draw('H');

        // Before, the size was taken in pixels and drawn at 68% of that: an H 20 pixels tall, not 38.
        Assert.InRange(h.Height, font.Metrics.CapHeight - 1, font.Metrics.CapHeight + 1);
    }

    [Theory]
    [InlineData('É')] // É: its accent was cut off.
    [InlineData('Å')] // Å
    [InlineData('g')]      // reaches below the baseline
    [InlineData('j')]      // reaches back past where it starts, too
    [InlineData('.')]      // drawn below the area searched, so it disappeared
    [InlineData(',')]
    [InlineData('-')]
    [InlineData('_')]
    public void DrawsAllOfAGlyph(char glyph)
    {
        var skia = new Backend("Skia", FontName, FontSize, glyph.ToString());
        using SKFont font = skia.FontInPixels(FontSize);
        font.MeasureText(glyph.ToString(), out SKRect bounds);

        Glyph drawn = skia.Draw(glyph);

        // The glyph's pixels cover its bounds, and at most part of a pixel more on each side.
        Assert.InRange(drawn.Width, bounds.Width - 1, bounds.Width + 2);
        Assert.InRange(drawn.Height, bounds.Height - 1, bounds.Height + 2);
    }

    [Fact]
    public void PutsGlyphsOnABaselineTheAscentBelowTheTopOfTheLine()
    {
        var skia = new Backend("Skia", FontName, FontSize, "Hx.");
        using SKFont font = skia.FontInPixels(FontSize);
        float ascent = -font.Metrics.Ascent;

        foreach (char glyph in "Hx.")
        {
            // Each sits on the baseline, so its pixels end on the row above it.
            Glyph drawn = skia.Draw(glyph);
            Assert.InRange(drawn.Bottom + 1, ascent - 1, ascent + 1);
        }
    }

    [Fact]
    public void SpacesLinesByTheFontsLineSpacing()
    {
        var skia = new Backend("Skia", FontName, FontSize, "H");
        using SKFont font = skia.FontInPixels(FontSize);

        // Before, it was the height of a capital, so the lines of a label overlapped.
        Assert.Equal(font.Spacing, skia.LineHeight, 2);
    }

    [Fact]
    public void AdvancesAGlyphByItsAdvance()
    {
        var skia = new Backend("Skia", FontName, FontSize, "Hi");
        using SKFont font = skia.FontInPixels(FontSize);

        // Before, it added a pixel on each side.
        Assert.Equal(MathF.Round(font.MeasureText("H")), skia.Advance('H'));
        Assert.Equal(MathF.Round(font.MeasureText("i")), skia.Advance('i'));
    }

    [Fact]
    public void GivesACharacterThatDoesntAdvanceAnAdvance()
    {
        // A combining acute accent, which goes over the character before it.
        const char accent = '́';
        var skia = new Backend("Skia", FontName, FontSize, accent.ToString());
        using SKFont font = skia.FontInPixels(FontSize);

        // Before, a character that doesn't advance got no entry, so CCLabel threw
        // KeyNotFoundException for it.
        Assert.Equal(MathF.Round(font.MeasureText(accent.ToString())), skia.Advance(accent));
    }

    // On Windows, CCLabel draws with GDI+. Skia puts each glyph where GDI+ does, at the same size,
    // and spaces lines as far apart, so a label looks the same on Linux and macOS as on Windows.
    [WindowsTheory]
    [InlineData(12)]
    [InlineData(20)]
    [InlineData(40)]
    [InlineData(72)]
    public void DrawsGlyphsWhereGdiPlusDoes(float size)
    {
        const string glyphs = "HExgjÉ.,-_W";
        var skia = new Backend("Skia", FontName, size, glyphs);
        var gdi = new Backend("", FontName, size, glyphs);

        Assert.Equal(gdi.LineHeight, skia.LineHeight, 1);

        // The two rasterize a glyph a pixel or two apart. At some sizes, GDI+ also puts the
        // baseline a few pixels lower than the ascent says.
        var mismatches = new List<string>();
        foreach (char glyph in glyphs)
        {
            Glyph s = skia.Draw(glyph);
            Glyph g = gdi.Draw(glyph);
            if (Math.Abs(s.Left - g.Left) > 2 || Math.Abs(s.Top - g.Top) > 3
                || Math.Abs(s.Width - g.Width) > 2 || Math.Abs(s.Height - g.Height) > 2)
            {
                mismatches.Add($"'{glyph}': Skia {s}, GDI+ {g}");
            }
        }
        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    // Where a glyph's pixels are in the bitmap a backend draws it into: what CCLabel takes as its
    // offset from the top left of its line, and its size in the atlas.
    private readonly record struct Glyph(int Left, int Top, int Width, int Height)
    {
        public int Bottom => Top + Height - 1;
    }

    // A label backend with a font made, reached through CCLabel's private methods. The backends
    // keep the font and the bitmap in static fields, so only one Backend of each kind works at a
    // time.
    private sealed class Backend
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        private readonly object _label;
        private readonly string _suffix;

        // With "Skia", the Skia backend's own methods. With "", the ones CCLabel calls, which pick
        // the backend for the OS: GDI+ on Windows.
        public Backend(string suffix, string fontName, float fontSize, string glyphs)
        {
            _suffix = suffix;
            _label = RuntimeHelpers.GetUninitializedObject(typeof(CCLabel));
            // CCNode's finalizer runs Cleanup, which throws on a node whose constructor never ran,
            // and ends the test run (C2D-365).
            GC.SuppressFinalize(_label);

            Call("CreateFont", [typeof(string), typeof(float), typeof(CCRawList<char>)],
                fontName, fontSize, new CCRawList<char>(glyphs.ToCharArray()));
        }

        public float LineHeight => (float)Call("GetFontHeight", [])!;

        public float Advance(char glyph)
        {
            object info = Call("GetKerningInfo", [typeof(char)], glyph)!;
            Type type = info.GetType();
            return (float)type.GetField("A")!.GetValue(info)!
                + (float)type.GetField("B")!.GetValue(info)!
                + (float)type.GetField("C")!.GetValue(info)!;
        }

        // The typeface the Skia backend chose, which is a substitute where the font isn't
        // installed, at a size in points.
        public SKFont FontInPixels(float sizeInPoints)
        {
            var typeface = (SKTypeface)typeof(CCLabel).GetField("_currentTypeface", Private)!.GetValue(null)!;
            return new SKFont(typeface, sizeInPoints * 96 / 72);
        }

        // As CCLabel.InitializeFont does it: measure the glyph, have it drawn, and search the
        // measured size, plus two pixels each way, for pixels that aren't zero.
        public unsafe Glyph Draw(char glyph)
        {
            string text = glyph.ToString();
            var size = (CCSize)Call("GetMeasureString", [typeof(string)], text)!;
            int width = (int)Math.Ceiling(size.Width + 2);
            int height = (int)Math.Ceiling(size.Height + 2);

            object?[] args = [text, null];
            var pixels = (byte*)Pointer.Unbox(Call("GetBitmapData", [typeof(string), typeof(int).MakeByRefType()], args)!);
            int stride = (int)args[1]!;

            int left = width, top = height, right = -1, bottom = -1;
            for (int y = 0; y < height; y++)
            {
                var row = (int*)(pixels + y * stride);
                for (int x = 0; x < width; x++)
                {
                    if (row[x] != 0)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }
                }
            }
            return right < 0 ? new Glyph(-1, -1, 0, 0) : new Glyph(left, top, right - left + 1, bottom - top + 1);
        }

        private object? Call(string name, Type[] parameters, params object?[] args)
        {
            MethodInfo method = typeof(CCLabel).GetMethod(name + _suffix, Private, parameters)
                ?? throw new MissingMethodException(typeof(CCLabel).FullName, name + _suffix);
            try
            {
                return method.Invoke(_label, args);
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }
    }
}

// A theory that runs only on Windows, the only OS with GDI+.
internal sealed class WindowsTheoryAttribute : TheoryAttribute
{
    public WindowsTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "GDI+ runs only on Windows.";
        }
    }
}
