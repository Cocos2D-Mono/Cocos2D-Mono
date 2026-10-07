#nullable disable
using System;
using System.Threading.Tasks;

namespace Cocos2D;

public static class CCTask
{

    #region Threading
        /// <summary>
        /// Checks if the code is currently running on the UI thread.
        /// </summary>
        /// <returns>true if the code is currently running on the UI thread.</returns>
        public static bool IsOnUIThread()
        {
            return (true);

        }

        /// <summary>
        /// No-op on current platforms. Historically this enforced that the caller was running
        /// on the UI thread (Windows Phone); current targets have no separate UI thread.
        /// </summary>
        public static void EnsureUIThread()
        {
        }

        /// <summary>
        /// Runs the given action on the UI thread. On current platforms there is no separate
        /// UI thread, so the action runs immediately on the calling thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        public static void RunOnUiThread(Action action)
        {
            action();
        }

        /// <summary>
        /// Runs the given action on the UI thread and blocks the current thread while the action is running.
        /// If the current thread is the UI thread, the action will run immediately.
        /// </summary>
        /// <param name="action">The action to be run on the UI thread</param>
        public static void BlockOnUIThread(Action action)
        {
            action();
        }

    #endregion

    private class TaskSelector : ICCSelectorProtocol
    {
        public void Update(float dt)
        {
        }
    }

    private static ICCSelectorProtocol _taskSelector = new TaskSelector();

    /// <summary>
    /// Schedules the given action to run on the next scheduler tick (the main loop).
    /// </summary>
    /// <param name="action">The action to run.</param>
    public static void RunOnScheduler(Action action)
    {
        var scheduler = CCDirector.SharedDirector.Scheduler;
        scheduler.ScheduleSelector(f => action(), _taskSelector, 0, 0, 0, false);
    }

    /// <summary>
    /// Runs the given action asynchronously on a background <see cref="Task"/>.
    /// </summary>
    /// <param name="action">The action to run in the background.</param>
    /// <returns>The started background task.</returns>
    public static object RunAsync(Action action)
    {
        return RunAsync(action, null);
    }
		
    /// <summary>
    /// Runs the given action asynchronously on a background <see cref="Task"/>, then optionally
    /// invokes a completion callback via the scheduler (the main loop).
    /// </summary>
    /// <param name="action">The action to run in the background.</param>
    /// <param name="taskCompleted">Optional callback invoked on the scheduler after the action completes.</param>
    /// <returns>The started background task.</returns>
    public static object RunAsync(Action action, Action<object> taskCompleted)
    {
        var task = new Task(
            () =>
            {
                action();

                if (taskCompleted != null)
                {
                    var scheduler = CCDirector.SharedDirector.Scheduler;
                    scheduler.ScheduleSelector(f => taskCompleted(null), _taskSelector, 0, 0, 0, false);
                }
            }
            );

        task.Start();

        return task;
    }
}
