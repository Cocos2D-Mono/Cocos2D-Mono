using System.Collections.Generic;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// The structs in ccTypes.cs that define == also override Equals and GetHashCode to agree with it
// (C2D-198). A HashSet uses both overrides.
public class CCTypesEqualityTests
{
    [Fact]
    public void Color4B_InASet_IsFoundByAnEqualColor()
    {
        var set = new HashSet<CCColor4B> { new CCColor4B(1, 2, 3, 4) };

        Assert.Contains(new CCColor4B(1, 2, 3, 4), set);
        Assert.DoesNotContain(new CCColor4B(1, 2, 3, 5), set);
    }

    [Fact]
    public void Color4F_InASet_IsFoundByAnEqualColor()
    {
        // 0 and -0 are equal under ==, so they must hash alike.
        var set = new HashSet<CCColor4F> { new CCColor4F(0f, 0.5f, 1f, 1f) };

        Assert.Contains(new CCColor4F(-0f, 0.5f, 1f, 1f), set);
        Assert.DoesNotContain(new CCColor4F(0f, 0.5f, 1f, 0.5f), set);
    }

    [Fact]
    public void Color4F_WithNaN_EqualsItselfButNotUnderTheOperator()
    {
        // Equals is reflexive, as float.Equals is, so such a color can still be found in a set.
        // == follows IEEE 754, where NaN equals nothing.
        var color = new CCColor4F(float.NaN, 0f, 0f, 1f);
        var copy = color;

        Assert.True(color.Equals((object)copy));
        Assert.False(color == copy);
    }

    [Fact]
    public void PointI_InASet_IsFoundByAnEqualPoint()
    {
        var set = new HashSet<CCPointI> { new CCPointI(1, 2) };

        Assert.Contains(new CCPointI(1, 2), set);
        Assert.DoesNotContain(new CCPointI(2, 1), set);
    }

    [Fact]
    public void BoundingBoxI_InASet_IsFoundByAnEqualBox()
    {
        var set = new HashSet<CCBoundingBoxI> { new CCBoundingBoxI(1, 2, 3, 4) };

        Assert.Contains(new CCBoundingBoxI(1, 2, 3, 4), set);
        Assert.DoesNotContain(new CCBoundingBoxI(1, 2, 3, 5), set);
    }
}
