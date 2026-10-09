using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests from the labels' nullable conversion (C2D-292). The parser threw
// NullReferenceException on a font file that doesn't end with a newline, and font data it
// couldn't read left the character set null.
public class CCBMFontConfigurationTests
{
    private const string CharLine = "char id=65 x=0 y=0 width=8 height=8 xoffset=0 yoffset=0 xadvance=8 page=0 chnl=0";

    [Fact]
    public void Constructor_DataWithoutATrailingNewline_ReadsTheLastLine()
    {
        var configuration = new CCBMFontConfiguration(CharLine, "font.fnt");

        Assert.Contains(65, configuration.CharacterSet);
    }

    [Fact]
    public void Constructor_EmptyData_LeavesAnEmptyCharacterSet()
    {
        var configuration = new CCBMFontConfiguration("", "font.fnt");

        Assert.Empty(configuration.CharacterSet);
    }
}
