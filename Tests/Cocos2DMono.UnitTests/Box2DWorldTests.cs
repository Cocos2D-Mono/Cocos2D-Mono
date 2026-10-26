using System;
using System.Threading.Tasks;
using Box2D.Collision;
using Box2D.Collision.Shapes;
using Box2D.Common;
using Box2D.Dynamics;
using Box2D.Dynamics.Contacts;
using Box2D.Dynamics.Joints;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of Box2D's Dynamics core (C2D-306).
public class Box2DWorldTests
{
    [Fact]
    public void DestroyFixture_ForAFixtureAfterTheSecondInTheList_Returns()
    {
        // The loop that looks for the fixture never moved on to the next one, so it spun forever
        // unless the fixture was the first or second in the body's list. Bodies list their
        // fixtures newest first, so the first fixture made is the last.
        b2Body body = NewWorld().CreateBody(DynamicBody(0, 0));
        b2Fixture oldest = body.CreateFixture(Circle(), 1);
        body.CreateFixture(Circle(), 1);
        body.CreateFixture(Circle(), 1);

        bool returned = Task.Run(() => body.DestroyFixture(oldest)).Wait(TimeSpan.FromSeconds(5));

        Assert.True(returned, "DestroyFixture didn't return within 5 seconds.");
        Assert.Equal(2, body.FixtureCount);
        for (b2Fixture? f = body.FixtureList; f != null; f = f.Next)
        {
            Assert.NotSame(oldest, f);
        }
    }

    [Fact]
    public void CreateMethods_WhileTheWorldIsStepping_ThrowInvalidOperationException()
    {
        // They returned null while the world was locked, which callers used as a body, fixture or
        // joint.
        b2World world = NewWorld();
        b2Body ground = world.CreateBody(new b2BodyDef());
        var box = new b2PolygonShape();
        box.SetAsBox(10, 0.5f);
        ground.CreateFixture(box, 0);
        b2Body ball = world.CreateBody(DynamicBody(0, 1));
        ball.CreateFixture(Circle(), 1);
        var listener = new CreatingListener(world, ground, ball);
        world.SetContactListener(listener);

        for (int i = 0; i < 60 && listener.Errors == null; i++)
        {
            world.Step(1 / 60f, 8, 3);
        }

        Assert.NotNull(listener.Errors);
        Assert.All(listener.Errors, e => Assert.IsType<InvalidOperationException>(e));
    }

    [Fact]
    public void Step_WhenACallbackThrows_UnlocksTheWorld()
    {
        // The world stayed locked when an exception escaped from a callback, so creating a body
        // threw until a later Step finished.
        b2World world = NewWorld();
        world.CreateBody(DynamicBody(0, 0)).CreateFixture(Circle(), 1);
        world.CreateBody(DynamicBody(0.5f, 0)).CreateFixture(Circle(), 1);
        world.SetContactListener(new ThrowingListener(world));

        Assert.Throws<InvalidOperationException>(() => world.Step(1 / 60f, 8, 3));

        Assert.False(world.IsLocked);
        world.CreateBody(new b2BodyDef());
        Assert.Equal(3, world.BodyCount);
    }

    [Fact]
    public void Step_WhileTheWorldIsStepping_ThrowsInvalidOperationException()
    {
        // A nested Step ran a whole step in the middle of the outer one, and unlocked the world
        // when it finished, so the rest of the outer step ran unlocked.
        b2World world = NewWorld();
        world.CreateBody(DynamicBody(0, 0)).CreateFixture(Circle(), 1);
        world.CreateBody(DynamicBody(0.5f, 0)).CreateFixture(Circle(), 1);
        var listener = new SteppingListener(world);
        world.SetContactListener(listener);

        world.Step(1 / 60f, 8, 3);

        Assert.IsType<InvalidOperationException>(listener.Error);
        Assert.True(listener.LockedAfterward, "The nested Step unlocked the world.");
        Assert.False(world.IsLocked);
    }

    [Fact]
    public void DrawShapeAndDrawJoint_WithoutADebugDraw_DoNothing()
    {
        // They drew with the world's debug draw without checking that it had one, as
        // DrawDebugData does.
        b2World world = NewWorld();
        b2Body a = world.CreateBody(DynamicBody(0, 0));
        b2Fixture fixture = a.CreateFixture(Circle(), 1);
        b2Body b = world.CreateBody(DynamicBody(2, 0));
        b.CreateFixture(Circle(), 1);
        var def = new b2DistanceJointDef();
        def.Initialize(a, b, a.Position, b.Position);
        b2Joint joint = world.CreateJoint(def);
        b2Transform xf = a.Transform;

        world.DrawShape(fixture, ref xf, new b2Color(1, 1, 1));
        world.DrawJoint(joint);
    }

    [Fact]
    public void CompareTo_Null_IsPositive()
    {
        // It read the other body's type without checking for null.
        b2Body body = NewWorld().CreateBody(new b2BodyDef());

        Assert.True(body.CompareTo(null) > 0);
    }

    [Fact]
    public void CreateFixture_FromADefinitionWithoutAShape_ThrowsArgumentException()
    {
        // The fixture cloned the definition's missing shape, which threw NullReferenceException.
        b2Body body = NewWorld().CreateBody(DynamicBody(0, 0));

        var exception = Assert.Throws<ArgumentException>(() => body.CreateFixture(new b2FixtureDef()));

        Assert.Equal("def", exception.ParamName);
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

    // Tries each create method from inside a contact callback, while the world is locked.
    private sealed class CreatingListener : b2ContactListener
    {
        private readonly b2World _world;
        private readonly b2Body _ground;
        private readonly b2Body _ball;

        public CreatingListener(b2World world, b2Body ground, b2Body ball)
        {
            _world = world;
            _ground = ground;
            _ball = ball;
        }

        public Exception?[]? Errors { get; private set; }

        public override void BeginContact(b2Contact contact)
        {
            var jointDef = new b2DistanceJointDef();
            jointDef.Initialize(_ground, _ball, _ground.Position, _ball.Position);
            Errors = new[]
            {
                Record.Exception(() => _world.CreateBody(new b2BodyDef())),
                Record.Exception(() => _ball.CreateFixture(Circle(), 1)),
                Record.Exception(() => _world.CreateJoint(jointDef)),
            };
        }

        public override void PreSolve(b2Contact contact, b2Manifold oldManifold) { }
        public override void PostSolve(b2Contact contact, ref b2ContactImpulse impulse) { }
    }

    // Steps the world from inside a contact callback, and checks the lock after the nested call.
    private sealed class SteppingListener : b2ContactListener
    {
        private readonly b2World _world;

        public SteppingListener(b2World world)
        {
            _world = world;
        }

        public Exception? Error { get; private set; }
        public bool LockedAfterward { get; private set; }

        public override void BeginContact(b2Contact contact)
        {
            Error = Record.Exception(() => _world.Step(1 / 60f, 8, 3));
            LockedAfterward = _world.IsLocked;
        }

        public override void PreSolve(b2Contact contact, b2Manifold oldManifold) { }
        public override void PostSolve(b2Contact contact, ref b2ContactImpulse impulse) { }
    }

    // Creates a body from inside a contact callback, and lets the exception escape from Step.
    private sealed class ThrowingListener : b2ContactListener
    {
        private readonly b2World _world;

        public ThrowingListener(b2World world)
        {
            _world = world;
        }

        public override void BeginContact(b2Contact contact)
        {
            _world.CreateBody(new b2BodyDef());
        }

        public override void PreSolve(b2Contact contact, b2Manifold oldManifold) { }
        public override void PostSolve(b2Contact contact, ref b2ContactImpulse impulse) { }
    }
}
