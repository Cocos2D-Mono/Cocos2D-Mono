using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the menus' nullable conversion (C2D-299). A toggle made with the parameterless
// constructor had no sub-item list, so AddSubItem and Enabled threw NullReferenceException.
public class CCMenuItemToggleTests
{
    [Fact]
    public void AddSubItem_OnAToggleMadeWithNew_AddsTheItem()
    {
        var toggle = new CCMenuItemToggle();
        var item = new CCMenuItem();

        toggle.AddSubItem(item);

        Assert.Same(item, Assert.Single(toggle.SubItems));
    }

    [Fact]
    public void Enabled_OnAToggleMadeWithNew_DoesNotThrow()
    {
        var toggle = new CCMenuItemToggle();

        toggle.Enabled = false;

        Assert.False(toggle.Enabled);
    }
}
