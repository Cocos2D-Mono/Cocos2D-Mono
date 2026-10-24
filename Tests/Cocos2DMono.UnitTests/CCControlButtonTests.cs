using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the controls' nullable conversion (C2D-305), for title nodes that aren't
// labels.
public class CCControlButtonTests
{
    [Fact]
    public void Constructor_WithATitleNodeThatIsNotALabel_UsesItAsTheTitle()
    {
        // Initialising read the node's text as a label's, which threw NullReferenceException.
        // Laying out the button then cast it to a label, which threw InvalidCastException.
        var title = new CCNode();
        var background = new CCNode();

        var button = new CCControlButton(title, background);

        Assert.Same(title, button.TitleLabel);
        Assert.Same(background, button.BackgroundSprite);
        Assert.Equal(string.Empty, button.GetTitleForState(CCControlState.Normal));
    }

    [Fact]
    public void TitleFontMethods_ForAStateWhoseTitleNodeIsNotALabel_TreatItAsHavingNoFont()
    {
        // They cast the state's title node to a label, which threw InvalidCastException.
        var button = new CCControlButton("", "Arial", 12);
        button.SetTitleLabelForState(new CCNode(), CCControlState.Highlighted);

        Assert.Equal(string.Empty, button.GetTitleTtfForState(CCControlState.Highlighted));
        Assert.Equal(string.Empty, button.GetTitleBmFontForState(CCControlState.Highlighted));
        button.SetTitleTtfSizeForState(20, CCControlState.Highlighted);
    }
}
