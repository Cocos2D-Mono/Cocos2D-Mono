using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using Cocos2D;
using Xunit;
using Xunit.Sdk;

namespace Cocos2DMono.UnitTests;

public class CCLabelTests
{
    // Regression test from CCLabel's nullable conversion (C2D-293). A label drawn before any label
    // had text or a font threw NullReferenceException uploading the shared atlas, which didn't
    // exist yet.
    [Fact]
    public void Draw_BeforeAnyLabelHasAFont_DoesNotThrow()
    {
        var texture = CCLabel.m_pTexture;
        var data = CCLabel.m_pData;
        CCLabel.m_pTexture = null;
        CCLabel.m_pData = null;
        try
        {
            var label = new FontlessLabel();

            var exception = Record.Exception(() => label.Draw());

            Assert.Null(exception);
        }
        finally
        {
            CCLabel.m_pTexture = texture;
            CCLabel.m_pData = data;
        }
    }

    // Regression tests from C2D-368. CCLabel gave a glyph with no pixels, like a space, a 1x1
    // region of its atlas, and filled it from a buffer that still held the previous glyph's
    // pixels. So the space drew that glyph's top-left pixel as a dot, just below the line. In
    // Arial on Windows, the pixel an "l" left behind was 45% opaque.
    [Theory]
    [InlineData('l')]
    [InlineData('█')] // A full block leaves its pixel too.
    [InlineData('O')]      // Leaves a transparent pixel, but the space still took a region.
    public void InitializeFont_GivesASpaceNoRoomInTheAtlas(char before)
    {
        var (font, atlas, atlasWidth) = InitializeFont("Arial", 40, $"{before} ");
        Dictionary<int, CCBMFontConfiguration.CCBMFontDef> glyphs = FontDefs(font);
        CCBMFontConfiguration.CCBMFontDef space = glyphs[' '];

        // As in a .fnt file, a space's rect is empty, so it draws nothing.
        CCRect rect = space.rect;
        Assert.True(rect.Size.Width == 0 && rect.Size.Height == 0,
            $"The space has a {rect.Size.Width}x{rect.Size.Height} region, holding 0x{atlas[(int)rect.Origin.Y * atlasWidth + (int)rect.Origin.X]:X8}.");

        // It still advances, and the glyph before it still has its pixels.
        Assert.True(space.xAdvance > 0);
        Assert.True(glyphs[before].rect.Size.Width > 0);
    }

    // Regression tests from C2D-369. CCLabel packed its glyphs into the atlas edge to edge, and
    // the atlas samples linearly. A glyph drawn between pixels, as CCLabelBMFont draws any glyph
    // of odd width, takes in the texels just outside its rect, so it showed a faint line from
    // the glyph next to it. Linear sampling reads at most half a texel past the rect, so a ring
    // of transparent texels around each glyph is all it needs.
    [Theory]
    [InlineData("lI|█")]
    // More glyphs than fit in one row of the atlas, so some sit below others.
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%&")]
    public void InitializeFont_LeavesATransparentTexelAroundEachGlyph(string text)
    {
        var (font, atlas, atlasWidth) = InitializeFont("Arial", 40, text);
        Dictionary<int, CCBMFontConfiguration.CCBMFontDef> glyphs = FontDefs(font);

        foreach (char c in text.Distinct())
        {
            CCRect rect = glyphs[c].rect;
            int left = (int)Math.Floor(rect.Origin.X);
            int top = (int)rect.Origin.Y;
            int right = left + (int)Math.Round(rect.Size.Width);
            int bottom = top + (int)rect.Size.Height;

            for (int y = top - 1; y <= bottom; y++)
            {
                for (int x = left - 1; x <= right; x++)
                {
                    bool inGlyph = x >= left && x < right && y >= top && y < bottom;
                    int texel = atlas[y * atlasWidth + x];
                    Assert.True(inGlyph || texel == 0,
                        $"'{c}' at ({left}, {top}), {right - left}x{bottom - top}, has 0x{texel:X8} beside it, at ({x}, {y}).");
                }
            }
        }
    }

    // Regression tests from C2D-370. On Windows, CCLabel searched only the box GDI+ lays a glyph out
    // in for the glyph's pixels: from the top of its line, and from a sixth of an em left of where
    // the glyph starts to as far right of its advance. GDI+ draws past that box, and what it drew
    // there was lost: ink above the font's ascent, like the acute on Ǻ, and combining accents that
    // GDI+ draws left or right of the box. GDI+ also hints what it draws, which can move a small
    // accent a few pixels from where the glyph's outline puts it.
    [WindowsTheory]
    // \u0300 to \u0303 are combining accents: grave, acute, circumflex and tilde.
    [InstalledFontData("Arial", 40, "Ǻ\u0301\u0300")]     // Ǻ lost its top; the accents, their left edge.
    [InstalledFontData("Times New Roman", 40, "Ǻ\u0301")]
    [InstalledFontData("Verdana", 40, "\u0301\u0300")]   // These accents are drawn wholly left of the box.
    [InstalledFontData("Segoe UI", 40, "\u0301\u0300")]  // These reach past its right edge.
    [InstalledFontData("Georgia", 20, "\u0301\u0302Ấ")]  // Hinted above the top of the line.
    [InstalledFontData("Courier New", 8, "\u0300\u0301\u0303")]
    [InstalledFontData("Gabriola", 8, "Ǻ")]
    [InstalledFontData("Arial", 12, "Ǻ\u0301")]
    // Glyphs GDI+ doesn't draw where their outline is: Trebuchet MS's lower, Segoe Script's wider.
    [InstalledFontData("Trebuchet MS", 56, "_µ(")]
    [InstalledFontData("Segoe Script", 40, "bqn")]
    // Glyphs that always fitted.
    [InstalledFontData("Arial", 40, "HgjÉ_W")]
    [SupportedOSPlatform("windows")]
    public void InitializeFont_KeepsAllOfAGlyphGdiPlusDraws(string fontName, float fontSize, string text)
    {
        var (font, _, _) = InitializeFont(fontName, fontSize, text);
        Dictionary<int, CCBMFontConfiguration.CCBMFontDef> glyphs = FontDefs(font);

        var mismatches = new List<string>();
        foreach (char c in text)
        {
            var drawn = GdiPlusInk(fontName, fontSize * CCMacros.CCContentScaleFactor(), c);
            CCBMFontConfiguration.CCBMFontDef glyph = glyphs[c];
            var kept = (Left: glyph.xOffset, Top: glyph.yOffset,
                Width: (int)glyph.rect.Size.Width, Height: (int)glyph.rect.Size.Height);
            if (kept != drawn)
            {
                mismatches.Add($"U+{(int)c:X4}: CCLabel kept {kept}, GDI+ draws {drawn}");
            }
        }
        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    // Where GDI+ draws a glyph's pixels, from where it's asked to draw the glyph, with the settings
    // CCLabel uses and room on every side, so none are cut off.
    [SupportedOSPlatform("windows")]
    private static unsafe (int Left, int Top, int Width, int Height) GdiPlusInk(string fontName, float fontSize, char glyph)
    {
        using var font = new Font(fontName, fontSize);
        int room = (int)Math.Ceiling(2 * font.GetHeight()) + 8;
        using var bitmap = new Bitmap(4 * room, 3 * room, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(Color.White))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.Clear(Color.Transparent);
            graphics.DrawString(glyph.ToString(), font, brush, room, room);
        }

        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
            for (int y = 0; y < bitmap.Height; y++)
            {
                var row = (int*)((byte*)data.Scan0 + y * data.Stride);
                for (int x = 0; x < bitmap.Width; x++)
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
            Assert.True(right >= 0, $"GDI+ draws nothing for U+{(int)glyph:X4} in {fontName}.");
            Assert.True(left > 0 && top > 0 && right < bitmap.Width - 1 && bottom < bitmap.Height - 1,
                $"U+{(int)glyph:X4} in {fontName} reaches the edge of the room given it.");
            return (left - room, top - room, right - left + 1, bottom - top + 1);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    // Runs CCLabel.InitializeFont on a new atlas, as a label does when it's given a font, and
    // returns the font it made and the atlas. Every label shares the atlas, the allocator that
    // places glyphs in it and the font cache, so they're all put back afterwards. On Windows this
    // draws with GDI+, and on Linux and macOS with Skia.
    private static (CCBMFontConfiguration Font, int[] Atlas, int AtlasWidth) InitializeFont(
        string fontName, float fontSize, string text)
    {
        var shared = new SharedLabelState();
        CCLabel.m_pTexture = null;
        CCLabel.m_pData = null;
        CCLabelBMFont.s_pConfigurations.Clear();
        try
        {
            MethodInfo initializeFont = typeof(CCLabel).GetMethod("InitializeFont", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(CCLabel).FullName, "InitializeFont");
            var font = (CCBMFontConfiguration)initializeFont.Invoke(new FontlessLabel(), [fontName, fontSize, text, false])!;
            int atlasWidth = (int)typeof(CCLabel).GetField("_width", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            return (font, CCLabel.m_pData!, atlasWidth);
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
        finally
        {
            shared.Restore();
        }
    }

    // What CCLabel.InitializeFont changes that every label shares, saved when this is made: the
    // atlas, the allocator that places glyphs in it, and the font cache.
    private sealed class SharedLabelState
    {
        private const BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly string[] AllocatorFields = ["_width", "_height", "_used"];

        private readonly CCTexture2D? _texture = CCLabel.m_pTexture;
        private readonly int[]? _data = CCLabel.m_pData;
        private readonly CCLabel.ivec3[] _nodes = Nodes.ToArray();
        private readonly int[] _allocator = Array.ConvertAll(AllocatorFields, name => (int)Field(name).GetValue(null)!);
        private readonly bool _textureDirty = (bool)Field("m_bTextureDirty").GetValue(null)!;
        private readonly Dictionary<string, CCBMFontConfiguration> _fonts = new(CCLabelBMFont.s_pConfigurations);

        private static CCRawList<CCLabel.ivec3> Nodes => (CCRawList<CCLabel.ivec3>)Field("_nodes").GetValue(null)!;

        public void Restore()
        {
            // The texture made for the new atlas, which nothing else holds.
            if (CCLabel.m_pTexture != _texture)
            {
                CCLabel.m_pTexture?.Dispose();
            }
            CCLabel.m_pTexture = _texture;
            CCLabel.m_pData = _data;

            Nodes.Clear();
            Nodes.AddRange(_nodes);
            for (int i = 0; i < AllocatorFields.Length; i++)
            {
                Field(AllocatorFields[i]).SetValue(null, _allocator[i]);
            }
            Field("m_bTextureDirty").SetValue(null, _textureDirty);

            CCLabelBMFont.s_pConfigurations.Clear();
            foreach (var pair in _fonts)
            {
                CCLabelBMFont.s_pConfigurations.Add(pair.Key, pair.Value);
            }
        }

        private static FieldInfo Field(string name)
        {
            return typeof(CCLabel).GetField(name, Private)
                ?? throw new MissingFieldException(typeof(CCLabel).FullName, name);
        }
    }

    private static Dictionary<int, CCBMFontConfiguration.CCBMFontDef> FontDefs(CCBMFontConfiguration font)
    {
        return (Dictionary<int, CCBMFontConfiguration.CCBMFontDef>)typeof(CCBMFontConfiguration)
            .GetField("m_pFontDefDictionary", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(font)!;
    }

    // Stands in for a label made with the parameterless constructor. It gives the label an
    // empty texture atlas without the vertex buffer, which needs a graphics device; with no
    // quads, drawing the batch returns straight away.
    private sealed class FontlessLabel : CCLabel
    {
        protected override bool InitWithString(string? theString, string? fntFile, CCSize dimensions,
            CCTextAlignment hAlignment, CCVerticalTextAlignment vAlignment, CCPoint imageOffset, CCTexture2D? texture)
        {
            m_pobTextureAtlas = new CCTextureAtlas
            {
                Texture = new CCTexture2D(),
                m_pQuads = new CCRawList<CCV3F_C4B_T2F_Quad>()
            };
            // The atlas starts out needing an upload; make sure it still does.
            m_bTextureDirty = true;
            return true;
        }
    }
}

// A row of data for a theory that draws with a font through GDI+, skipped where the font isn't
// installed. GDI+ draws a font it can't find with another, so the row would otherwise pass
// without testing the font it names. CI's unit tests run on Windows Server, which may lack some.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class InstalledFontDataAttribute : DataAttribute
{
    private readonly object[] _row;

    public InstalledFontDataAttribute(string fontName, float fontSize, string text)
    {
        _row = [fontName, fontSize, text];
        if (OperatingSystem.IsWindows() && !IsInstalled(fontName))
        {
            Skip = $"{fontName} isn't installed.";
        }
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        return [_row];
    }

    [SupportedOSPlatform("windows")]
    private static bool IsInstalled(string fontName)
    {
        using var font = new Font(fontName, 12);
        return string.Equals(font.Name, fontName, StringComparison.OrdinalIgnoreCase);
    }
}
