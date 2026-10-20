using System;

namespace Cocos2D;

/// <summary>
/// A simple cooldown/duration timer struct. Useful for tracking cooldowns,
/// invincibility frames, combo windows, and other time-based game state.
///
/// Usage:
///   var dashCooldown = new CCCooldownTimer(0.8f);
///   // In update loop:
///   dashCooldown.Update(dt);
///   if (dashCooldown.IsReady) { Dash(); dashCooldown.Reset(); }
/// </summary>
public struct CCCooldownTimer
{
    /// <summary>
    /// The total duration of the timer in seconds.
    /// </summary>
    public float Duration;

    /// <summary>
    /// The elapsed time since the timer was last reset.
    /// </summary>
    public float Elapsed;

    /// <summary>
    /// Whether the timer has completed (elapsed >= duration).
    /// </summary>
    public bool IsReady => Elapsed >= Duration;

    /// <summary>
    /// Progress from 0.0 (just started) to 1.0 (complete).
    /// </summary>
    public float Progress => Duration > 0f ? Math.Min(Elapsed / Duration, 1f) : 1f;

    /// <summary>
    /// Remaining time in seconds.
    /// </summary>
    public float Remaining => Math.Max(Duration - Elapsed, 0f);

    /// <summary>
    /// Creates a new timer with the given duration. Starts ready (elapsed = duration).
    /// Call Reset() to start the cooldown.
    /// </summary>
    public CCCooldownTimer(float duration)
    {
        Duration = duration;
        Elapsed = duration; // starts ready
    }

    /// <summary>
    /// Creates a new timer with the given duration and initial readiness.
    /// </summary>
    public CCCooldownTimer(float duration, bool startReady)
    {
        Duration = duration;
        Elapsed = startReady ? duration : 0f;
    }

    /// <summary>
    /// Advances the timer by dt seconds.
    /// </summary>
    public void Update(float dt)
    {
        if (Elapsed < Duration)
        {
            Elapsed += dt;
        }
    }

    /// <summary>
    /// Resets the timer, starting the cooldown from zero.
    /// </summary>
    public void Reset()
    {
        Elapsed = 0f;
    }

    /// <summary>
    /// Resets the timer with a new duration.
    /// </summary>
    public void Reset(float newDuration)
    {
        Duration = newDuration;
        Elapsed = 0f;
    }

    /// <summary>
    /// If the timer is ready, resets it and returns true. Otherwise returns false.
    /// Convenient one-liner: if (timer.TryFire(dt)) { DoAction(); }
    /// </summary>
    public bool TryConsume()
    {
        if (IsReady)
        {
            Reset();
            return true;
        }
        return false;
    }
}
