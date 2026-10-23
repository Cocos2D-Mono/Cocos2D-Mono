using Cocos2D;

namespace tests;

public class GzipTestScene : TestScene
{
    public override void runThisTest()
    {
        AddChild(new GzipTest());
        CCDirector.SharedDirector.ReplaceScene(this);
    }

    // The test has one page, so there's nothing to move to.
    protected override void NextTestCase() { }
    protected override void PreviousTestCase() { }
    protected override void RestTestCase() { }
}
