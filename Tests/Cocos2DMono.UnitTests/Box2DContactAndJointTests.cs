using System;
using Box2D.Collision.Shapes;
using Box2D.Common;
using Box2D.Dynamics;
using Box2D.Dynamics.Contacts;
using Box2D.Dynamics.Joints;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests for Box2D's contacts and joints: from their nullable conversion (C2D-306), and from
// C2D-338's fixes to contacts between edges and chains.
public class Box2DContactAndJointTests
{
    [Fact]
    public void ContactConstructor_ForTwoCircles_MakesAContactThatUpdates()
    {
        // The constructor didn't make the manifold or the edges, so Init threw
        // NullReferenceException.
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

    // Regression tests from C2D-338. Box2D has no contact between two edges, an edge and a chain,
    // or two chains: they don't collide with each other. Its factory returns null for them, but
    // b2Contact.Create made a circle contact. So the first step after two of them met threw
    // InvalidCastException, casting an edge to a circle.
    [Theory]
    [InlineData(b2ShapeType.e_edge, b2ShapeType.e_edge)]
    [InlineData(b2ShapeType.e_edge, b2ShapeType.e_chain)]
    [InlineData(b2ShapeType.e_chain, b2ShapeType.e_edge)]
    [InlineData(b2ShapeType.e_chain, b2ShapeType.e_chain)]
    public void Step_AnEdgeOrChainFallingOntoAnEdgeOrChain_FallsThroughIt(b2ShapeType falling, b2ShapeType ground)
    {
        b2World world = NewWorld();
        world.CreateBody(new b2BodyDef()).CreateFixture(Line(ground, 5), 0);
        b2Body body = world.CreateBody(DynamicBody(0, 1));
        body.CreateFixture(Line(falling, 1), 1);

        for (int i = 0; i < 60; i++)
        {
            world.Step(1 / 60f, 8, 3);
        }

        // A second's fall takes it 5 m down, through the ground, with no contact on the way.
        Assert.True(body.Position.y < -1, $"The body stopped at y = {body.Position.y}.");
        Assert.Equal(0, world.ContactCount);
    }

    [Theory]
    [InlineData(b2ShapeType.e_edge, b2ShapeType.e_edge)]
    [InlineData(b2ShapeType.e_edge, b2ShapeType.e_chain)]
    [InlineData(b2ShapeType.e_chain, b2ShapeType.e_chain)]
    public void Create_ForEdgesAndChains_ReturnsNull(b2ShapeType first, b2ShapeType second)
    {
        b2World world = NewWorld();
        b2Fixture a = world.CreateBody(DynamicBody(0, 0)).CreateFixture(Shape(first), 1);
        b2Fixture b = world.CreateBody(DynamicBody(0, 0)).CreateFixture(Shape(second), 1);

        Assert.Null(b2Contact.Create(a, 0, b, 0));
    }

    // The constructor never set the contact's type, so it always made a circle contact. One made
    // for any other shapes threw InvalidCastException when it updated.
    [Theory]
    [InlineData(b2ShapeType.e_polygon, b2ShapeType.e_circle)]
    [InlineData(b2ShapeType.e_circle, b2ShapeType.e_polygon)]
    [InlineData(b2ShapeType.e_polygon, b2ShapeType.e_polygon)]
    [InlineData(b2ShapeType.e_edge, b2ShapeType.e_circle)]
    [InlineData(b2ShapeType.e_polygon, b2ShapeType.e_edge)]
    [InlineData(b2ShapeType.e_chain, b2ShapeType.e_circle)]
    [InlineData(b2ShapeType.e_polygon, b2ShapeType.e_chain)]
    public void ContactConstructor_ForShapesThatCollide_MakesTheirContact(b2ShapeType first, b2ShapeType second)
    {
        b2World world = NewWorld();
        b2Fixture a = world.CreateBody(DynamicBody(0, 0)).CreateFixture(Shape(first), 1);
        b2Fixture b = world.CreateBody(DynamicBody(0, 0)).CreateFixture(Shape(second), 1);

        var contact = new b2Contact(a, 0, b, 0);
        contact.Update(null);

        // As in a contact the world makes, an edge or a chain is fixture A, and so is a polygon
        // with a circle.
        var (expectedA, expectedB) = Rank(first) <= Rank(second) ? (a, b) : (b, a);
        Assert.Same(expectedA, contact.FixtureA);
        Assert.Same(expectedB, contact.FixtureB);
        Assert.True(contact.IsTouching());
    }

    [Theory]
    [InlineData(b2ShapeType.e_edge, b2ShapeType.e_edge)]
    [InlineData(b2ShapeType.e_chain, b2ShapeType.e_edge)]
    [InlineData(b2ShapeType.e_chain, b2ShapeType.e_chain)]
    public void ContactConstructor_ForEdgesAndChains_ThrowsArgumentException(b2ShapeType first, b2ShapeType second)
    {
        b2World world = NewWorld();
        b2Fixture a = world.CreateBody(DynamicBody(0, 0)).CreateFixture(Shape(first), 1);
        b2Fixture b = world.CreateBody(DynamicBody(0, 0)).CreateFixture(Shape(second), 1);

        var exception = Assert.Throws<ArgumentException>(() => new b2Contact(a, 0, b, 0));

        Assert.Equal("fB", exception.ParamName);
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

    // A shape of the given type around its body's origin: a circle or a box a metre across, or a
    // line 2 m long.
    private static b2Shape Shape(b2ShapeType type)
    {
        switch (type)
        {
            case b2ShapeType.e_circle:
                return Circle();
            case b2ShapeType.e_polygon:
                var box = new b2PolygonShape();
                box.SetAsBox(0.5f, 0.5f);
                return box;
            default:
                return Line(type, 1);
        }
    }

    // A level edge, or a chain of two edges, through its body's origin, reaching as far each way.
    private static b2Shape Line(b2ShapeType type, float reach)
    {
        if (type == b2ShapeType.e_edge)
        {
            var edge = new b2EdgeShape();
            edge.Set(new b2Vec2(-reach, 0), new b2Vec2(reach, 0));
            return edge;
        }

        var chain = new b2ChainShape();
        chain.CreateChain([new b2Vec2(-reach, 0), new b2Vec2(0, 0), new b2Vec2(reach, 0)], 3);
        return chain;
    }

    // The order Box2D's contacts take shapes in: an edge or a chain, then a polygon, then a circle.
    private static int Rank(b2ShapeType type)
    {
        return type switch
        {
            b2ShapeType.e_edge or b2ShapeType.e_chain => 0,
            b2ShapeType.e_polygon => 1,
            _ => 2,
        };
    }
}
