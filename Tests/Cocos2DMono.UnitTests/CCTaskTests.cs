using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cocos2D;
using Microsoft.Xna.Framework;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests from C2D-366. CCTask.RunOnScheduler, and the completion callback CCTask.RunAsync
// takes, scheduled a timer from whatever thread called them, usually a background one. The
// scheduler isn't thread-safe: the game thread's update copies, reads and prunes its timers without
// a lock, so a timer added during an update could damage the table or be lost. Both go through the
// scheduler's queue now, which the game thread runs at the end of its next update.
public class CCTaskTests
{
    private static readonly GameTime OneFrame = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));

    [Fact]
    public void RunOnScheduler_FromAnotherThread_RunsTheActionOnTheGameThreadAfterTheNextUpdate()
    {
        CCDirector director = CCDirector.SharedDirector;
        int gameThread = Environment.CurrentManagedThreadId;
        int timers = Timers(director.Scheduler);
        var calls = new List<int>();

        RunOnAnotherThread(() => CCTask.RunOnScheduler(() => calls.Add(Environment.CurrentManagedThreadId)));

        Assert.True(Timers(director.Scheduler) == timers, "The other thread scheduled a timer.");
        Assert.Empty(calls);

        director.Update(OneFrame);

        Assert.Equal(gameThread, Assert.Single(calls));
    }

    [Fact]
    public void RunAsync_RunsTheCallbackOnTheGameThreadAfterTheNextUpdate()
    {
        CCDirector director = CCDirector.SharedDirector;
        int gameThread = Environment.CurrentManagedThreadId;
        int timers = Timers(director.Scheduler);
        var calls = new List<int>();

        var task = (Task)CCTask.RunAsync(() => { }, _ => calls.Add(Environment.CurrentManagedThreadId));
        Assert.True(SpinWait.SpinUntil(() => task.IsCompleted, TimeSpan.FromSeconds(10)), "The background task didn't finish.");

        Assert.True(Timers(director.Scheduler) == timers, "The background task scheduled a timer.");
        Assert.Empty(calls);

        director.Update(OneFrame);

        Assert.Equal(gameThread, Assert.Single(calls));
    }

    [Fact]
    public void RunOnScheduler_DuringAnUpdate_RunsTheActionAtTheEndOfThatUpdate()
    {
        CCDirector director = CCDirector.SharedDirector;
        int calls = 0;
        var target = new UpdateTarget(() => CCTask.RunOnScheduler(() => calls++));
        director.Scheduler.ScheduleUpdateForTarget(target, 0, false);
        try
        {
            // Before, the action's timer started on the next update and ran on the one after.
            director.Update(OneFrame);

            Assert.Equal(1, calls);
        }
        finally
        {
            director.Scheduler.UnscheduleUpdateForTarget(target);
        }
    }

    // Runs its action on its first update.
    private sealed class UpdateTarget(Action action) : ICCSelectorProtocol
    {
        private Action? _action = action;

        public void Update(float dt)
        {
            Action? action = _action;
            _action = null;
            action?.Invoke();
        }
    }

    // On a thread of its own, not a task: xUnit doesn't allow a test to block on a task.
    private static void RunOnAnotherThread(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                error = e;
            }
        });
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The thread didn't finish.");
        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    // How many timers the scheduler has, for all its targets. Its table of them is private: each
    // target's entry holds a list of its timers.
    private static int Timers(CCScheduler scheduler)
    {
        FieldInfo? table = typeof(CCScheduler).GetField("_hashForTimers", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(table);
        int timers = 0;
        foreach (object entry in Assert.IsAssignableFrom<IDictionary>(table.GetValue(scheduler)).Values)
        {
            FieldInfo? list = entry.GetType().GetField("Timers");
            Assert.NotNull(list);
            timers += Assert.IsAssignableFrom<IList>(list.GetValue(entry)).Count;
        }
        return timers;
    }
}
