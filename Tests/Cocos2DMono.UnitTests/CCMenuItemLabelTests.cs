using System;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the menus' nullable conversion (C2D-299). A label menu item cast its label to the
// color and text interfaces without checking, so disabling an item with no label threw
// NullReferenceException, and so did reading or setting the text of an item whose label has
// no text, or that has no label.
public class CCMenuItemLabelTests
{
    [Fact]
    public void Enabled_WithNoLabel_DoesNotThrow()
    {
        var item = new CCMenuItemLabel(new CCNode());
        item.Label = null;

        item.Enabled = false;

        Assert.False(item.Enabled);
    }

    [Fact]
    public void Enabled_WithAColoredLabel_ShowsTheDisabledColorWhileDisabled()
    {
        var red = new CCColor3B(255, 0, 0);
        var gray = new CCColor3B(100, 100, 100);
        var label = new CCSprite { Color = red };
        var item = new CCMenuItemLabel(label) { DisabledColor = gray };

        item.Enabled = false;
        Assert.Equal(gray, label.Color);

        item.Enabled = true;
        Assert.Equal(red, label.Color);
    }

    [Fact]
    public void Text_WithALabelThatHasNoText_Throws()
    {
        var item = new CCMenuItemLabel(new CCSprite());

        var exception = Assert.Throws<InvalidOperationException>(() => item.Text);

        Assert.Contains("CCSprite", exception.Message);
    }

    [Fact]
    public void SetString_WithNoLabel_Throws()
    {
        var item = new CCMenuItemLabel(new TextNode());
        item.Label = null;

        Assert.Throws<InvalidOperationException>(() => item.SetString("Play"));
    }

    [Fact]
    public void Text_SetsTheLabelsTextAndTakesItsSize()
    {
        var label = new TextNode { ContentSize = new CCSize(40, 10) };
        var item = new CCMenuItemLabel(label);

        item.Text = "Play";

        Assert.Equal("Play", label.Text);
        Assert.Equal("Play", item.Text);
        Assert.Equal(new CCSize(40, 10), item.ContentSize);
    }

    // A label with text that needs no graphics device.
    private sealed class TextNode : CCNode, ICCLabelProtocol
    {
        public string Text { get; set; } = "";
    }
}
