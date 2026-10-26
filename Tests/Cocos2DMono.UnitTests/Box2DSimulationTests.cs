using Box2D.Collision.Shapes;
using Box2D.Common;
using Box2D.Dynamics;
using Xunit;

namespace Cocos2DMono.UnitTests;

// A small simulation for Box2D's nullable conversion (C2D-306). Bodies landing on a chain shape go
// through the broad phase, the dynamic tree, the distance proxy, the contacts and the solver, whose
// asserts run in debug builds.
public class Box2DSimulationTests
{
    [Fact]
    public void BodiesDroppedOnAChain_ComeToRestOnIt()
    {
        var world = new b2World(new b2Vec2(0, -10));
        b2Body ground = world.CreateBody(new b2BodyDef());
        var chain = new b2ChainShape();
        chain.CreateChain(new[] { new b2Vec2(-20, 0), new b2Vec2(0, 0), new b2Vec2(20, 0) }, 3);
        ground.CreateFixture(chain, 0);

        b2Body ball = Drop(world, new b2CircleShape { Radius = 0.5f }, 0, 3);
        var box = new b2PolygonShape();
        box.SetAsBox(0.5f, 0.5f);
        // The crate lands on the ball and either stays on it or slides off onto the chain.
        b2Body crate = Drop(world, box, 0.2f, 5);

        for (int i = 0; i < 240; i++)
        {
            world.Step(1 / 60f, 8, 3);
        }

        Assert.InRange(ball.Position.y, 0.45f, 0.55f);
        Assert.InRange(crate.Position.y, 0.45f, 2.6f);
    }

    private static b2Body Drop(b2World world, b2Shape shape, float x, float y)
    {
        var def = new b2BodyDef();
        def.type = b2BodyType.b2_dynamicBody;
        def.position.Set(x, y);
        b2Body body = world.CreateBody(def);
        body.CreateFixture(shape, 1);
        return body;
    }
}
