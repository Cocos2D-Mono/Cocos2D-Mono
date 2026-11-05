using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
    public void Tick_WhenAHandlerRethrows_StillLetsTheTargetBeCollected()
    {
        // The scheduler passes what a timer throws to OnUnhandledException. A handler that
        // rethrows skipped the tick's clean-up, so the array kept the target.
        WeakReference target = RunAThrowingTimerWithARethrowingHandler();

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

    // The tests below come from C2D-361. The tick and UnscheduleAll grew their static arrays by
    // doubling them once, so more than twice as many timer targets as an array had slots made
    // CopyTo throw ArgumentException. The arrays only grow, so each test takes its number of
    // targets from the array's current length.

    [Fact]
    public void Tick_WithMoreThanTwiceAsManyTimerTargetsAsSlots_RunsEveryTimer()
    {
        CCDirector director = CCDirector.SharedDirector;
        CCNode[] nodes = NewNodes(2 * ScratchSlots("s_pTmpSelectorArray") + 1);
        int runs = 0;
        try
        {
            foreach (CCNode node in nodes)
            {
                director.Scheduler.ScheduleSelector(_ => runs++, node, 0f, CCScheduler.kCCRepeatForever, 0f, paused: false);
            }

            // A timer's first tick only starts it, so the second tick runs it.
            director.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));
            director.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));

            Assert.Equal(nodes.Length, runs);
        }
        finally
        {
            foreach (CCNode node in nodes)
            {
                director.Scheduler.UnscheduleAllForTarget(node);
            }
        }
    }

    [Fact]
    public void UnscheduleAll_WithMoreThanTwiceAsManyTimerTargetsAsSlots_UnschedulesThemAll()
    {
        var scheduler = new CCScheduler();
        CCNode[] nodes = NewNodes(2 * ScratchSlots("s_pTmpHashSelectorArray") + 1);
        foreach (CCNode node in nodes)
        {
            scheduler.ScheduleSelector(_ => { }, node, 1f, CCScheduler.kCCRepeatForever, 0f, paused: true);
        }
        Assert.All(nodes, node => Assert.True(scheduler.IsTargetPaused(node)));

        scheduler.UnscheduleAll();

        // Once removed, a target is unknown to the scheduler, which reports it as not paused.
        Assert.All(nodes, node => Assert.False(scheduler.IsTargetPaused(node)));
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

    // A timer's first tick only starts it, so the second tick fires it and throws. The handler
    // and the node are removed again afterwards.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunAThrowingTimerWithARethrowingHandler()
    {
        CCDirector director = CCDirector.SharedDirector;
        var node = new CCNode();
        Action<Exception> rethrow = error => ExceptionDispatchInfo.Throw(error);
        director.Scheduler.OnUnhandledException += rethrow;
        try
        {
            director.Scheduler.ScheduleSelector(_ => throw new InvalidOperationException("Thrown by the test."), node, 0f, CCScheduler.kCCRepeatForever, 0f, paused: false);
            director.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));
            Assert.Throws<InvalidOperationException>(() => director.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16))));
        }
        finally
        {
            director.Scheduler.OnUnhandledException -= rethrow;
            director.Scheduler.UnscheduleAllForTarget(node);
        }
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

    private static CCNode[] NewNodes(int count) =>
        Enumerable.Range(0, count).Select(_ => new CCNode()).ToArray();

    // The number of slots in one of the scheduler's static scratch arrays, which are private.
    private static int ScratchSlots(string field)
    {
        FieldInfo? info = typeof(CCScheduler).GetField(field, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(info);
        return ((Array)info.GetValue(null)!).Length;
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
