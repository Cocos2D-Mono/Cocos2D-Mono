using System.Runtime.CompilerServices;
using Cocos2D;
using Microsoft.Xna.Framework;
using Xunit;

namespace Cocos2DMono.UnitTests;

// CCScheduler.update(dt) (the per-frame tick) is internal, so firing is exercised through the
// public CCTimer.Update(dt) - the scheduler's actual timing engine - while registration and
// pause state are exercised through CCScheduler's public API. Each test uses a local scheduler,
// so there is no shared-singleton state to clean up.
public class CCSchedulerTests
{
    private static CCTimer NewTimer(Action<float> selector, float interval, float delay) =>
        new CCTimer(new CCScheduler(), new CCNode(), selector, interval, CCScheduler.kCCRepeatForever, delay);

    [Fact]
    public void Timer_FirstTick_OnlyInitializes()
    {
        int count = 0;
        var timer = NewTimer(_ => count++, interval: 1f, delay: 0f);

        timer.Update(5f);   // even a large dt: the first tick just initializes, no fire
        Assert.Equal(0, count);
    }

    [Fact]
    public void Timer_DoesNotFireBeforeInterval_ThenFires()
    {
        int count = 0;
        var timer = NewTimer(_ => count++, interval: 1f, delay: 0f);

        timer.Update(0f);     // initialize
        timer.Update(0.5f);   // elapsed 0.5 < 1 -> no fire
        Assert.Equal(0, count);

        timer.Update(0.5f);   // elapsed 1.0 >= 1 -> fire
        Assert.Equal(1, count);
    }

    [Fact]
    public void Timer_FiresOncePerInterval()
    {
        int count = 0;
        var timer = NewTimer(_ => count++, interval: 1f, delay: 0f);

        timer.Update(0f);     // initialize
        timer.Update(1f);     // fire (1)
        timer.Update(1f);     // fire (2)
        timer.Update(1f);     // fire (3)
        Assert.Equal(3, count);
    }

    [Fact]
    public void Timer_WithDelay_DefersFirstFire()
    {
        int count = 0;
        var timer = NewTimer(_ => count++, interval: 1f, delay: 2f);

        timer.Update(0f);     // initialize
        timer.Update(1f);     // elapsed 1.0 < delay 2 -> no fire
        Assert.Equal(0, count);

        timer.Update(1.5f);   // elapsed 2.5 >= delay 2 -> first fire
        Assert.Equal(1, count);
    }

    [Fact]
    public void ScheduleSelector_DefaultsToNotPaused()
    {
        var scheduler = new CCScheduler();
        var target = new CCNode();

        scheduler.ScheduleSelector(_ => { }, target, 1f, CCScheduler.kCCRepeatForever, 0f, paused: false);

        Assert.False(scheduler.IsTargetPaused(target));
    }

    [Fact]
    public void PauseTarget_And_ResumeTarget_ToggleState()
    {
        var scheduler = new CCScheduler();
        var target = new CCNode();
        scheduler.ScheduleSelector(_ => { }, target, 1f, CCScheduler.kCCRepeatForever, 0f, paused: false);

        scheduler.PauseTarget(target);
        Assert.True(scheduler.IsTargetPaused(target));

        scheduler.ResumeTarget(target);
        Assert.False(scheduler.IsTargetPaused(target));
    }

    [Fact]
    public void UnscheduleAllForTarget_RemovesTarget()
    {
        var scheduler = new CCScheduler();
        var target = new CCNode();
        scheduler.ScheduleSelector(_ => { }, target, 1f, CCScheduler.kCCRepeatForever, 0f, paused: false);
        scheduler.PauseTarget(target);
        Assert.True(scheduler.IsTargetPaused(target));   // registered (and paused)

        scheduler.UnscheduleAllForTarget(target);

        // Once removed, the target is unknown to the scheduler -> default (not paused).
        Assert.False(scheduler.IsTargetPaused(target));
    }

    // From the root files' nullable conversion (C2D-302). A timer made with the parameterless
    // constructor has no scheduler, and updating it threw NullReferenceException when it tried
    // to unschedule itself after its one run.
    [Fact]
    public void Timer_FromTheParameterlessConstructor_UpdatesWithoutThrowing()
    {
        var timer = new CCTimer();

        Exception? error = Record.Exception(() =>
        {
            timer.Update(0.1f);   // initialize
            timer.Update(0.1f);   // its one run is due, then it unschedules itself
        });

        Assert.Null(error);
    }

    // The tests below come from C2D-358. The tick and UnscheduleAll copied the timers' targets into
    // static arrays and never cleared them, so a target stayed reachable after it was unscheduled,
    // and with it whatever it referred to, such as a departed scene.

    [Fact]
    public void Tick_LetsAnUnscheduledTargetBeCollected()
    {
        WeakReference target = ScheduleForOneTickThenUnschedule();

        Collect();

        Assert.False(target.IsAlive);
    }

    [Fact]
    public void UnscheduleAll_LetsTheTargetsBeCollected()
    {
        WeakReference target = ScheduleThenUnscheduleAll(new CCScheduler());

        Collect();

        Assert.False(target.IsAlive);
    }

    // The tick is internal, so this runs one through the shared director, which leaves nothing
    // scheduled afterwards. Not inlined, so no local in the test refers to the node.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ScheduleForOneTickThenUnschedule()
    {
        CCDirector director = CCDirector.SharedDirector;
        var node = new CCNode();
        director.Scheduler.ScheduleSelector(_ => { }, node, 0f, CCScheduler.kCCRepeatForever, 0f, paused: false);
        director.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));
        director.Scheduler.UnscheduleAllForTarget(node);
        return new WeakReference(node);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ScheduleThenUnscheduleAll(CCScheduler scheduler)
    {
        var node = new CCNode();
        scheduler.ScheduleSelector(_ => { }, node, 1f, CCScheduler.kCCRepeatForever, 0f, paused: false);
        scheduler.UnscheduleAll();
        return new WeakReference(node);
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
