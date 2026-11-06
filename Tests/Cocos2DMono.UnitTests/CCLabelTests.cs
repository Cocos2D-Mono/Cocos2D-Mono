using System.Reflection;
using System.Runtime.ExceptionServices;
using Cocos2D;
using Xunit;

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

    // Runs CCLabel.InitializeFont on a new atlas, as a label does when it's given a font, and
    // returns the font it made and the atlas. The atlas and the font cache are shared by every
    // label, so they're put back afterwards. On Windows this draws with GDI+, and on Linux and
    // macOS with Skia.
    private static (CCBMFontConfiguration Font, int[] Atlas, int AtlasWidth) InitializeFont(
        string fontName, float fontSize, string text)
    {
        var texture = CCLabel.m_pTexture;
        var data = CCLabel.m_pData;
        var fonts = new Dictionary<string, CCBMFontConfiguration>(CCLabelBMFont.s_pConfigurations);
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
            CCLabel.m_pTexture = texture;
            CCLabel.m_pData = data;
            CCLabelBMFont.s_pConfigurations.Clear();
            foreach (var pair in fonts)
            {
                CCLabelBMFont.s_pConfigurations.Add(pair.Key, pair.Value);
            }
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
