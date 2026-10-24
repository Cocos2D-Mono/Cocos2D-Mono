using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the controls' nullable conversion (C2D-305). A control without a parent
// converted a touch to its parent's space, which threw NullReferenceException.
public class CCControlTests
{
    [Fact]
    public void IsTouchInside_WithoutAParent_TestsTheTouchInWorldSpace()
    {
        var touch = new CCTouch(1, 50, 50);
        var control = new CCControl();
        control.ContentSize = new CCSize(20, 20);

        control.Position = touch.Location - new CCPoint(10, 10);
        Assert.True(control.IsTouchInside(touch));

        control.Position = touch.Location + new CCPoint(30, 30);
        Assert.False(control.IsTouchInside(touch));
    }

    [Fact]
    public void Slider_IsTouchInside_WithoutAParent_TestsTheTouchInWorldSpace()
    {
        var touch = new CCTouch(1, 50, 50);
        var slider = new CCControlSlider(new CCSprite(), new CCSprite(), new CCSprite());
        slider.ContentSize = new CCSize(20, 20);

        // The slider's anchor point is its centre.
        slider.Position = touch.Location;
        Assert.True(slider.IsTouchInside(touch));

        slider.Position = touch.Location + new CCPoint(30, 30);
        Assert.False(slider.IsTouchInside(touch));
    }
}
