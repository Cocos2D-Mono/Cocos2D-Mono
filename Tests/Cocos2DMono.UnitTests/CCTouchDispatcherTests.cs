using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCTouchDispatcherTests
{
    [Fact]
    public void AddAndRemoveDelegate_BeforeInit_Works()
    {
        // The handler lists used to be created only by Init(), so a dispatcher used
        // before it threw a null reference error (C2D-270).
        var dispatcher = new CCTouchDispatcher();
        var target = new Target();

        dispatcher.AddTargetedDelegate(target, 0, true);
        Assert.NotNull(dispatcher.FindHandler(target));

        dispatcher.RemoveDelegate(target);
        Assert.Null(dispatcher.FindHandler(target));
    }

    private sealed class Target : ICCTargetedTouchDelegate
    {
        public int TouchPriority => 0;
        public bool VisibleForTouches { get; set; } = true;

        public bool TouchBegan(CCTouch pTouch) => false;
        public void TouchMoved(CCTouch pTouch) { }
        public void TouchEnded(CCTouch pTouch) { }
        public void TouchCancelled(CCTouch pTouch) { }
    }
}
