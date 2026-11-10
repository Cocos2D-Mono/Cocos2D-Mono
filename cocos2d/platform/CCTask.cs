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

    /// <summary>
    /// Runs the given action on the game thread, at the end of the scheduler's next update. It's
    /// safe to call from any thread, unlike the scheduler's own methods.
    /// </summary>
    /// <remarks>
    /// Called during an update, the action runs at the end of that update. It runs even while the
    /// scheduler's targets are paused. What it throws goes to
    /// <see cref="CCScheduler.OnUnhandledException"/>.
    /// </remarks>
    /// <param name="action">The action to run.</param>
    public static void RunOnScheduler(Action action)
    {
        CCDirector.SharedDirector.Scheduler.RunOnNextTick(action);
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
    /// runs a completion callback on the game thread, as <see cref="RunOnScheduler"/> does.
    /// </summary>
    /// <param name="action">The action to run in the background.</param>
    /// <param name="taskCompleted">Optional callback, run on the game thread after the action completes.</param>
    /// <returns>The started background task.</returns>
    public static object RunAsync(Action action, Action<object?>? taskCompleted)
    {
        var task = new Task(
            () =>
            {
                action();

                if (taskCompleted != null)
                {
                    // On the background thread, so it goes through the scheduler's queue: the rest
                    // of the scheduler isn't thread-safe.
                    RunOnScheduler(() => taskCompleted(null));
                }
            }
            );

        task.Start();

        return task;
    }
}
