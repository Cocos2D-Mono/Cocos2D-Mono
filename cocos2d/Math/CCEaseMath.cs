using System;
using Microsoft.Xna.Framework;

namespace Cocos2D;

public static class CCEaseMath
{
    public static float Linear(float time)
    {
        return time;
    }

    public static float QuadIn(float time)
    {
        return time * time;
    }

    public static float QuadOut(float time)
    {
        return time * (2f - time);
    }

    public static float QuadInOut(float time)
    {
        if (time < 0.5f)
        {
            return 2f * time * time;
        }
        return -1f + (4f - 2f * time) * time;
    }

    public static float CubicIn(float time)
    {
        return time * time * time;
    }

    public static float CubicOut(float time)
    {
        time -= 1f;
        return time * time * time + 1f;
    }

    public static float CubicInOut(float time)
    {
        if (time < 0.5f)
        {
            return 4f * time * time * time;
        }
        time -= 1f;
        return 1f + 4f * time * time * time;
    }

    public static float BackIn(float time)
    {
        const float overshoot = 1.70158f;
        
        return time * time * ((overshoot + 1) * time - overshoot);
    }

    public static float BackOut(float time)
    {
        const float overshoot = 1.70158f;

        time = time - 1;
        return time * time * ((overshoot + 1) * time + overshoot) + 1;
    }

    public static float BackInOut(float time)
    {
        const float overshoot = 1.70158f * 1.525f;

        time = time * 2;
        if (time < 1)
        {
            return (time * time * ((overshoot + 1) * time - overshoot)) / 2;
        }
        else
        {
            time = time - 2;
            return (time * time * ((overshoot + 1) * time + overshoot)) / 2 + 1;
        }
    }

    public static float BounceOut(float time)
    {
        if (time < 1 / 2.75)
        {
            return 7.5625f * time * time;
        }
        else if (time < 2 / 2.75)
        {
            time -= 1.5f / 2.75f;
            return 7.5625f * time * time + 0.75f;
        }
        else if (time < 2.5 / 2.75)
        {
            time -= 2.25f / 2.75f;
            return 7.5625f * time * time + 0.9375f;
        }

        time -= 2.625f / 2.75f;
        return 7.5625f * time * time + 0.984375f;
    }

    public static float BounceIn(float time)
    {
        return 1f - BounceOut(1f - time);
    }

    public static float BounceInOut(float time)
    {
        if (time < 0.5f)
        {
            time = time * 2;
            return (1 - BounceOut(1 - time)) * 0.5f;
        }
        return BounceOut(time * 2 - 1) * 0.5f + 0.5f;
    }

    public static float SineOut(float time)
    {
        return (float) Math.Sin(time * MathHelper.PiOver2);
    }

    public static float SineIn(float time)
    {
        return -1f * (float)Math.Cos(time * MathHelper.PiOver2) + 1f;
    }

    public static float SineInOut(float time)
    {
        return -0.5f * ((float)Math.Cos((float)Math.PI * time) - 1f);
    }

    public static float ExponentialOut(float time)
    {
        return time == 1f ? 1f : (-(float)Math.Pow(2f, -10f * time / 1f) + 1f);
    }

    public static float ExponentialIn(float time)
    {
        return time == 0f ? 0f : (float)Math.Pow(2f, 10f * (time / 1f - 1f)) - 1f * 0.001f;
    }

    public static float ExponentialInOut(float time)
    {
        time /= 0.5f;
        if (time < 1)
        {
            return 0.5f * (float)Math.Pow(2f, 10f * (time - 1f));
        }
        else
        {
            return 0.5f * (-(float)Math.Pow(2f, -10f * (time - 1f)) + 2f);
        }
    }

    public static float ElasticIn(float time, float period)
    {
        if (time == 0 || time == 1)
        {
            return time;
        }
        else
        {
            float s = period / 4;
            time = time - 1;
            return -(float)(Math.Pow(2, 10 * time) * Math.Sin((time - s) * MathHelper.Pi * 2.0f / period));
        }
    }

    public static float ElasticOut(float time, float period)
    {
        if (time == 0 || time == 1)
        {
            return time;
        }
        else
        {
            float s = period / 4;
            return (float)(Math.Pow(2, -10 * time) * Math.Sin((time - s) * MathHelper.Pi * 2f / period) + 1);
        }
    }

    public static float ElasticInOut(float time, float period)
    {
        if (time == 0 || time == 1)
        {
            return time;
        }
        else
        {
            time = time * 2;
            if (period == 0)
            {
                period = 0.3f * 1.5f;
            }

            float s = period / 4;

            time = time - 1;
            if (time < 0)
            {
                return (float)(-0.5f * Math.Pow(2, 10 * time) * Math.Sin((time - s) * MathHelper.TwoPi / period));
            }
            else
            {
                return (float)(Math.Pow(2, -10 * time) * Math.Sin((time - s) * MathHelper.TwoPi / period) * 0.5f + 1);
            }
        }
    }

    /// <summary>
    /// Elastic ease-in with default period (0.3).
    /// </summary>
    public static float ElasticIn(float time)
    {
        return ElasticIn(time, 0.3f);
    }

    /// <summary>
    /// Elastic ease-out with default period (0.3).
    /// </summary>
    public static float ElasticOut(float time)
    {
        return ElasticOut(time, 0.3f);
    }

    /// <summary>
    /// Elastic ease-in-out with default period (0.3), matching CCEaseElasticInOut.
    /// </summary>
    public static float ElasticInOut(float time)
    {
        return ElasticInOut(time, 0.3f);
    }

    /// <summary>
    /// Linearly interpolates between a and b by t.
    /// Delegates to MathHelper.Lerp.
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return MathHelper.Lerp(a, b, t);
    }

    /// <summary>
    /// Exponential smoothing towards a target value. Frame-rate independent.
    /// Smoothing controls how much of the remaining distance is kept each second:
    /// lower values (e.g. 0.01) converge quickly, higher values (e.g. 0.99) converge slowly.
    /// </summary>
    /// <param name="current">Current value.</param>
    /// <param name="target">Target value.</param>
    /// <param name="smoothing">Smoothing factor, clamped to (0, 1).</param>
    /// <param name="dt">Delta time in seconds (must be >= 0).</param>
    public static float ExpSmooth(float current, float target, float smoothing, float dt)
    {
        smoothing = MathHelper.Clamp(smoothing, 0.0001f, 0.9999f);
        if (dt < 0f) dt = 0f;
        return MathHelper.Lerp(current, target, 1f - (float)Math.Pow(smoothing, dt));
    }
}

