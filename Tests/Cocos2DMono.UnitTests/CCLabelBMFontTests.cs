using System;
using System.IO;
using System.Text;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the labels' nullable conversion (C2D-292). A label without a font threw
// NullReferenceException laying out text once it had a height, a font that failed to load
// was cached as null, so loading it again under that name kept failing, and FntFile didn't
// check that a font names a texture.
public class CCLabelBMFontTests
{
    private const string CharLine = "char id=65 x=0 y=0 width=8 height=8 xoffset=0 yoffset=0 xadvance=8 page=0 chnl=0";

    [Fact]
    public void SetString_OnALabelWithoutAFont_DoesNotThrow()
    {
        var label = new FontlessLabel();
        label.Dimensions = new CCSize(0, 20);

        var exception = Record.Exception(() => label.SetString("abc", true));

        Assert.Null(exception);
    }

    [Fact]
    public void FntFile_AFontThatNamesNoTexture_ThrowsWithoutChangingTheLabel()
    {
        // A font file without a page line names no texture. Caching it under its name lets
        // the setter find it without loading content, which the test host doesn't have.
        const string name = "c2d-292-no-texture.fnt";
        CCLabelBMFont.s_pConfigurations[name] = new CCBMFontConfiguration(CharLine + "\n", name);
        try
        {
            var label = new FontlessLabel();

            var exception = Assert.Throws<ArgumentException>(() => label.FntFile = name);

            Assert.Contains(name, exception.Message);
            Assert.Null(label.FntFile);
        }
        finally
        {
            CCLabelBMFont.s_pConfigurations.Remove(name);
        }
    }

    [Fact]
    public void FNTConfigLoadFile_AfterAFailedLoad_LoadsTheFontNextTime()
    {
        // The cache is shared, so use a name no other test loads.
        const string name = "c2d-292-reload.fnt";
        try
        {
            Assert.Null(CCLabelBMFont.FNTConfigLoadFile(name, ToStream("")));

            var configuration = CCLabelBMFont.FNTConfigLoadFile(name, ToStream(CharLine + "\n"));

            Assert.NotNull(configuration);
            Assert.Contains(65, configuration.CharacterSet);
        }
        finally
        {
            CCLabelBMFont.s_pConfigurations.Remove(name);
        }
    }

    private static Stream ToStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    // Stands in for a label made with the parameterless constructor, which has no font. It
    // keeps the layout settings and skips the texture atlas, which needs a graphics device.
    private sealed class FontlessLabel : CCLabelBMFont
    {
        protected override bool InitWithString(string? theString, string? fntFile, CCSize dimensions,
            CCTextAlignment hAlignment, CCVerticalTextAlignment vAlignment, CCPoint imageOffset, CCTexture2D? texture)
        {
            m_tDimensions = dimensions;
            m_pHAlignment = hAlignment;
            m_pVAlignment = vAlignment;
            return true;
        }
    }
}
