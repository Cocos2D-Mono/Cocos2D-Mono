using cocos2d.actions.action_intervals;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// CCLayerRGBA and CCLabelBMFont declared their own colour and opacity members instead of
// overriding CCNode's (C2D-198). A call through a CCNode reference, such as
// CCColorBlendAnimation's, ran CCNode's members, so the layer or label didn't change.
public class CCNodeColorOpacityTests
{
    [Fact]
    public void LayerColor_OpacitySetThroughACCNode_ChangesTheLayer()
    {
        var layer = new CCLayerColor(new CCColor4B(255, 255, 255, 255));
        CCNode node = layer;

        node.Opacity = 128;

        Assert.Equal(128, layer.Opacity);
    }

    [Fact]
    public void LayerColor_ColorBlendAnimation_ChangesTheLayer()
    {
        var layer = new CCLayerColor(new CCColor4B(255, 255, 255, 255));
        var blend = new CCColorBlendAnimation(1f, new CCColor3B(255, 0, 0));
        try
        {
            layer.RunAction(blend);
            blend.Update(1f);

            Assert.Equal(new CCColor3B(255, 0, 0), layer.Color);
        }
        finally
        {
            layer.StopAllActions();
        }
    }

    [Fact]
    public void LabelBMFont_ColorSetThroughACCNode_ChangesTheLabel()
    {
        var label = new FontlessLabel();
        CCNode node = label;

        node.Color = new CCColor3B(255, 0, 0);

        Assert.Equal(new CCColor3B(255, 0, 0), label.Color);
        Assert.Equal(new CCColor3B(255, 0, 0), label.DisplayedColor);
    }

    // Builds without loading a font, which the test host doesn't have.
    private sealed class FontlessLabel : CCLabelBMFont
    {
        protected override bool InitWithString(string? theString, string? fntFile, CCSize dimensions,
            CCTextAlignment hAlignment, CCVerticalTextAlignment vAlignment, CCPoint imageOffset, CCTexture2D? texture)
        {
            return true;
        }
    }
}
