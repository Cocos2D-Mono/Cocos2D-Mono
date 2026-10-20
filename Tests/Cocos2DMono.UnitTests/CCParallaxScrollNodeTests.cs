using System;
using System.Collections.Generic;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the misc nodes' nullable conversion (C2D-299). A parallax scroll node made from a
// sprite sheet whose first frame has no texture passed null to its sprite batch node, which
// threw NullReferenceException.
public class CCParallaxScrollNodeTests
{
    [Fact]
    public void Constructor_ASheetWhoseFirstFrameHasNoTexture_ThrowsArgumentException()
    {
        var sheet = new CCSpriteSheet(new Dictionary<string, CCSpriteFrame> { ["blank"] = new CCSpriteFrame() });

        var exception = Assert.Throws<ArgumentException>(() => new CCParallaxScrollNode(sheet));

        Assert.Equal("sheet", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNoSheet_HasNoBatchNode()
    {
        var node = new CCParallaxScrollNode();

        Assert.Null(node.BatchNode);
    }
}
