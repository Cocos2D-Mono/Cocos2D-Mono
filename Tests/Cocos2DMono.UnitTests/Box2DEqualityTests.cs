using Box2D.Collision;
using Box2D.Common;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for Box2D's nullable conversion (C2D-306). The Equals(object) overrides cast
// their argument to the struct's type, so null threw NullReferenceException and a value of another
// type threw InvalidCastException.
public class Box2DEqualityTests
{
    [Fact]
    public void Vec2_EqualsObject_IsFalseForNullOrAnotherType()
    {
        var v = new b2Vec2(1, 2);

        Assert.False(v.Equals(null));
        Assert.False(v.Equals("1, 2"));
        Assert.True(v.Equals((object) new b2Vec2(1, 2)));
    }

    [Fact]
    public void AABB_EqualsObject_IsFalseForNullOrAnotherType()
    {
        var box = new b2AABB { LowerBound = new b2Vec2(0, 0), UpperBound = new b2Vec2(1, 1) };

        Assert.False(box.Equals(null));
        Assert.False(box.Equals(new b2Vec2(0, 0)));
        Assert.True(box.Equals((object) new b2AABB { LowerBound = new b2Vec2(0, 0), UpperBound = new b2Vec2(1, 1) }));
    }

    [Fact]
    public void ContactFeature_EqualsObject_IsFalseForNullOrAnotherType()
    {
        var feature = new b2ContactFeature { indexA = 1, indexB = 2 };

        Assert.False(feature.Equals(null));
        Assert.False(feature.Equals(1));
        Assert.True(feature.Equals((object) new b2ContactFeature { indexA = 1, indexB = 2 }));
    }
}
