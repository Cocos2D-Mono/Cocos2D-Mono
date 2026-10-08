using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCSizeTests
{
    [Fact]
    public void Ctor_SetsWidthAndHeight()
    {
        var s = new CCSize(640f, 480f);
        Assert.Equal(640f, s.Width);
        Assert.Equal(480f, s.Height);
    }

    [Fact]
    public void Equality_OperatorsAndHashCode()
    {
        Assert.True(new CCSize(1f, 2f) == new CCSize(1f, 2f));
        Assert.True(new CCSize(1f, 2f) != new CCSize(2f, 1f));
        Assert.Equal(new CCSize(1f, 2f).GetHashCode(), new CCSize(1f, 2f).GetHashCode());
    }

    [Fact]
    public void ScalarMultiply_And_Divide()
    {
        Assert.True(new CCSize(2f, 4f) == new CCSize(1f, 2f) * 2f);
        Assert.True(new CCSize(1f, 2f) == new CCSize(2f, 4f) / 2f);
    }

    [Fact]
    public void Center_IsHalfExtents()
    {
        Assert.Equal(new CCPoint(5f, 10f), new CCSize(10f, 20f).Center);
    }

    [Fact]
    public void Zero_IsEmpty()
    {
        Assert.True(CCSize.Zero == new CCSize(0f, 0f));
    }

    [Fact]
    public void Diagonal_IsHypotenuse()
    {
        Assert.Equal(5f, new CCSize(3f, 4f).Diagonal, 5);
    }

    [Fact]
    public void Inverted_SwapsWidthAndHeight()
    {
        var s = new CCSize(3f, 4f).Inverted;
        Assert.Equal(4f, s.Width);
        Assert.Equal(3f, s.Height);
    }

    [Fact]
    public void AsRect_IsOriginRectOfThisSize()
    {
        var r = new CCSize(10f, 20f).AsRect;
        Assert.Equal(0f, r.MinX);
        Assert.Equal(0f, r.MinY);
        Assert.Equal(10f, r.Size.Width);
        Assert.Equal(20f, r.Size.Height);
    }

    [Fact]
    public void Clamp_CapsEachDimensionToMax()
    {
        var s = new CCSize(10f, 20f).Clamp(new CCSize(5f, 25f));
        Assert.Equal(5f, s.Width);    // 10 capped to 5
        Assert.Equal(20f, s.Height);  // 20 is under 25, unchanged
    }

    [Fact]
    public void Equals_NullOrOtherType_ReturnsFalse()
    {
        // Equals(object) used to unbox without checking, so null or another type threw (C2D-268).
        var s = new CCSize(640f, 480f);

        Assert.False(s.Equals(null));
        Assert.False(s.Equals("640x480"));
        Assert.True(s.Equals((object)new CCSize(640f, 480f)));
    }
}
