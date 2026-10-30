using System;
using Box2D.Collision.Shapes;
using Box2D.Common;
using Box2D.Dynamics;
using Box2D.Dynamics.Contacts;
using Box2D.Dynamics.Joints;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests for the nullable conversion of Box2D's contacts and joints (C2D-306).
public class Box2DContactAndJointTests
{
    [Fact]
    public void ContactConstructor_ForTwoCircles_MakesAContactThatUpdates()
    {
        // The constructor didn't make the manifold or the edges, so Init threw
        // NullReferenceException. It still always makes a circle contact (C2D-338).
        b2World world = NewWorld();
        b2Fixture a = world.CreateBody(DynamicBody(0, 0)).CreateFixture(Circle(), 1);
        b2Fixture b = world.CreateBody(DynamicBody(0.5f, 0)).CreateFixture(Circle(), 1);

        var contact = new b2Contact(a, 0, b, 0);
        contact.Update(null);

        Assert.Same(a, contact.FixtureA);
        Assert.Same(b, contact.FixtureB);
        Assert.Same(contact, contact.NodeA.Contact);
        Assert.Same(b.Body, contact.NodeA.Other);
        Assert.Same(a.Body, contact.NodeB.Other);
        Assert.True(contact.IsTouching());
    }

    [Fact]
    public void CreateJoint_FromADefinitionWithoutAJointType_ThrowsArgumentException()
    {
        // b2Joint.Create returned null for a type it can't create, after a failed assert, and
        // CreateJoint used it.
        b2World world = NewWorld();
        var def = new b2JointDef
        {
            BodyA = world.CreateBody(DynamicBody(0, 0)),
            BodyB = world.CreateBody(DynamicBody(1, 0)),
        };

        var exception = Assert.Throws<ArgumentException>(() => world.CreateJoint(def));

        Assert.Equal("def", exception.ParamName);
        Assert.Null(world.JointList);
    }

    [Fact]
    public void CreateJoint_FromADefinitionWithoutBodyB_ThrowsArgumentException()
    {
        // The joint kept the missing body, and CreateJoint threw NullReferenceException while it
        // linked the joint to its bodies, after adding it to the world's list.
        b2World world = NewWorld();
        var def = new b2DistanceJointDef { BodyA = world.CreateBody(DynamicBody(0, 0)) };

        var exception = Assert.Throws<ArgumentException>(() => world.CreateJoint(def));

        Assert.Equal("def", exception.ParamName);
        Assert.Equal(0, world.JointCount);
        Assert.Null(world.JointList);
    }

    [Fact]
    public void CreateJoint_FromAGearDefinitionWithoutJoints_ThrowsArgumentException()
    {
        // The gear joint used the definition's joints without checking that they were set.
        b2World world = NewWorld();
        var def = new b2GearJointDef
        {
            BodyA = world.CreateBody(DynamicBody(0, 0)),
            BodyB = world.CreateBody(DynamicBody(1, 0)),
        };

        var exception = Assert.Throws<ArgumentException>(() => world.CreateJoint(def));

        Assert.Equal("def", exception.ParamName);
    }

    [Fact]
    public void DistanceJoint_HoldsASwingingBallAtItsLength()
    {
        // Steps a world with a joint, so the joint solver and the island's joints run in a debug
        // build with this slice's asserts. It isn't a regression test: it passes on the old code
        // too.
        b2World world = NewWorld();
        b2Body anchor = world.CreateBody(new b2BodyDef());
        b2Body ball = world.CreateBody(DynamicBody(2, 0));
        ball.CreateFixture(Circle(), 1);
        var def = new b2DistanceJointDef();
        def.Initialize(anchor, ball, anchor.Position, ball.Position);
        world.CreateJoint(def);

        for (int i = 0; i < 120; i++)
        {
            world.Step(1 / 60f, 8, 3);
        }

        Assert.InRange((ball.Position - anchor.Position).Length, 1.95f, 2.05f);
        Assert.True(ball.Position.y < -0.5f, $"The ball didn't swing down: it's at y = {ball.Position.y}.");
    }

    private static b2World NewWorld()
    {
        return new b2World(new b2Vec2(0, -10));
    }

    private static b2BodyDef DynamicBody(float x, float y)
    {
        var def = new b2BodyDef();
        def.type = b2BodyType.b2_dynamicBody;
        def.position.Set(x, y);
        return def;
    }

    private static b2CircleShape Circle()
    {
        return new b2CircleShape { Radius = 0.5f };
    }
}
