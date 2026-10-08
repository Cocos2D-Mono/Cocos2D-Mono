using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCRectTests
{
    [Fact]
    public void Edges_ComputedFromOriginAndSize()
    {
        var r = new CCRect(10f, 20f, 30f, 40f);
        Assert.Equal(10f, r.MinX);
        Assert.Equal(20f, r.MinY);
        Assert.Equal(40f, r.MaxX); // 10 + 30
        Assert.Equal(60f, r.MaxY); // 20 + 40
        Assert.Equal(25f, r.MidX); // 10 + 30/2
        Assert.Equal(40f, r.MidY); // 20 + 40/2
    }

    [Fact]
    public void ContainsPoint_IsInclusiveOfEdges()
    {
        var r = new CCRect(0f, 0f, 10f, 10f);
        Assert.True(r.ContainsPoint(new CCPoint(5f, 5f)));
        Assert.True(r.ContainsPoint(0f, 0f));    // lower-left edge
        Assert.True(r.ContainsPoint(10f, 10f));  // upper-right edge
        Assert.False(r.ContainsPoint(new CCPoint(11f, 5f)));
        Assert.False(r.ContainsPoint(new CCPoint(-1f, 5f)));
    }

    [Fact]
    public void IntersectsRect_OverlapAndDisjoint()
    {
        var a = new CCRect(0f, 0f, 10f, 10f);
        Assert.True(a.IntersectsRect(new CCRect(5f, 5f, 10f, 10f)));
        Assert.False(a.IntersectsRect(new CCRect(100f, 100f, 5f, 5f)));
    }

    [Fact]
    public void Union_EnclosesBothRects()
    {
        var u = new CCRect(0f, 0f, 10f, 10f).Union(new CCRect(20f, 20f, 10f, 10f));
        Assert.Equal(0f, u.MinX);
        Assert.Equal(0f, u.MinY);
        Assert.Equal(30f, u.MaxX);
        Assert.Equal(30f, u.MaxY);
    }

    [Fact]
    public void Intersection_OfOverlappingRects()
    {
        var i = new CCRect(0f, 0f, 10f, 10f).Intersection(new CCRect(5f, 5f, 10f, 10f));
        Assert.Equal(5f, i.MinX);
        Assert.Equal(5f, i.MinY);
        Assert.Equal(10f, i.MaxX);
        Assert.Equal(10f, i.MaxY);
    }

    [Fact]
    public void Intersection_OfDisjointRects_IsZero()
    {
        var i = new CCRect(0f, 0f, 10f, 10f).Intersection(new CCRect(100f, 100f, 5f, 5f));
        Assert.True(i == CCRect.Zero);
    }

    [Fact]
    public void Equals_NullOrOtherType_ReturnsFalse()
    {
        // Equals(object) used to unbox without checking, so null or another type threw (C2D-268).
        var r = new CCRect(10f, 20f, 30f, 40f);

        Assert.False(r.Equals(null));
        Assert.False(r.Equals("10,20,30,40"));
        Assert.True(r.Equals((object)new CCRect(10f, 20f, 30f, 40f)));
    }
}
