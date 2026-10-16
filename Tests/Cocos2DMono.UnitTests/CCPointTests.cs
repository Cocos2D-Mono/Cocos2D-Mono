using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCPointTests
{
    [Fact]
    public void Ctor_SetsXAndY()
    {
        var p = new CCPoint(3f, 4f);
        Assert.Equal(3f, p.X);
        Assert.Equal(4f, p.Y);
    }

    [Fact]
    public void Zero_IsOrigin()
    {
        Assert.Equal(0f, CCPoint.Zero.X);
        Assert.Equal(0f, CCPoint.Zero.Y);
    }

    [Fact]
    public void Equality_OperatorsAndEquals()
    {
        var a = new CCPoint(1f, 2f);
        var b = new CCPoint(1f, 2f);
        var c = new CCPoint(2f, 1f);

        Assert.True(a == b);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Addition_AddsComponents()
    {
        Assert.Equal(new CCPoint(4f, 6f), new CCPoint(1f, 2f) + new CCPoint(3f, 4f));
    }

    [Fact]
    public void Subtraction_SubtractsComponents()
    {
        Assert.Equal(new CCPoint(4f, 5f), new CCPoint(5f, 7f) - new CCPoint(1f, 2f));
    }

    [Fact]
    public void Negation_NegatesComponents()
    {
        Assert.Equal(new CCPoint(-3f, 4f), -new CCPoint(3f, -4f));
    }

    [Fact]
    public void ScalarMultiply_And_Divide()
    {
        Assert.Equal(new CCPoint(2f, 4f), new CCPoint(1f, 2f) * 2f);
        Assert.Equal(new CCPoint(1f, 2f), new CCPoint(2f, 4f) / 2f);
    }

    [Fact]
    public void Distance_IsEuclidean()
    {
        Assert.Equal(5f, CCPoint.Distance(new CCPoint(0f, 0f), new CCPoint(3f, 4f)), 5);
    }

    [Fact]
    public void Dot_Product()
    {
        // 1*3 + 2*4 = 11
        Assert.Equal(11f, CCPoint.Dot(new CCPoint(1f, 2f), new CCPoint(3f, 4f)), 5);
    }

    [Fact]
    public void Lerp_AtHalf_IsMidpoint()
    {
        Assert.Equal(new CCPoint(5f, 10f), CCPoint.Lerp(new CCPoint(0f, 0f), new CCPoint(10f, 20f), 0.5f));
    }

    [Fact]
    public void Length_And_LengthSquared()
    {
        var p = new CCPoint(3f, 4f);
        Assert.Equal(25f, p.LengthSquared, 5);
        Assert.Equal(5f, p.Length, 5);
    }

    [Fact]
    public void Equals_NullOrOtherType_ReturnsFalse()
    {
        // Equals(object) used to unbox without checking, so null or another type threw (C2D-268).
        var p = new CCPoint(3f, 4f);

        Assert.False(p.Equals(null));
        Assert.False(p.Equals("3,4"));
        Assert.True(p.Equals((object)new CCPoint(3f, 4f)));
    }
}
