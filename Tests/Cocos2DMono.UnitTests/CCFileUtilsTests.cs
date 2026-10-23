using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the content-loading classes (C2D-303).
public class CCFileUtilsTests
{
    [Fact]
    public void FullPathFromRelativeFile_RelativeToAnEmptyPath_IsTheFileWithoutItsExtension()
    {
        // Path.GetDirectoryName returns null for an empty path, and combining it threw
        // ArgumentNullException.
        Assert.Equal("a", CCFileUtils.FullPathFromRelativeFile("a.png", ""));
    }
}
