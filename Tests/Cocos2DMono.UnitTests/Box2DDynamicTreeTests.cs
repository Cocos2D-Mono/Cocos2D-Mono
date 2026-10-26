using Box2D.Collision;
using Box2D.Common;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests for the nullable conversion of Box2D's dynamic tree (C2D-306).
public class Box2DDynamicTreeTests
{
    [Fact]
    public void GetUserData_ForAProxyWithNullUserData_ReturnsNull()
    {
        // A tree of a nullable type can store null as a proxy's user data. GetUserData asserted
        // that the user data wasn't null, which failed in a debug build.
        var tree = new b2DynamicTree<object?>();
        var aabb = new b2AABB { LowerBound = new b2Vec2(0, 0), UpperBound = new b2Vec2(1, 1) };
        object? userData = null;

        int proxyId = tree.CreateProxy(ref aabb, ref userData);

        Assert.Null(tree.GetUserData(proxyId));
    }

    [Fact]
    public void GetUserData_ForAProxy_ReturnsItsUserData()
    {
        var tree = new b2DynamicTree<string>();
        var aabb = new b2AABB { LowerBound = new b2Vec2(0, 0), UpperBound = new b2Vec2(1, 1) };
        string userData = "proxy";

        int proxyId = tree.CreateProxy(ref aabb, ref userData);

        Assert.Equal("proxy", tree.GetUserData(proxyId));
    }
}
