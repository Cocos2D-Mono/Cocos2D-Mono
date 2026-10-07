#nullable disable
using System;
using Microsoft.Xna.Framework;

namespace Cocos2D;

/// <summary>
/// A lightweight, struct-based particle emitter that renders via CCDrawNode.
/// Designed for high-performance scenarios (bullet hell, pixel explosions) where
/// the full CCParticleSystem is too heavy.
///
/// No plist files, no textured quads — just DrawDot/DrawFilledCircle calls
/// against a pre-allocated particle array. Zero GC pressure during gameplay.
///
/// Usage:
///   var emitter = new CCParticleEmitterLight(128);
///   emitter.OnUpdateParticle = (ref particle, dt) => {
///       particle.Velocity *= 1f - 0.4f * dt;  // drag
///       particle.Position += particle.Velocity * dt;
///   };
///   parentNode.AddChild(emitter);
///   emitter.Emit(position, 20, velocity, spread, colors);
/// </summary>
public class CCParticleEmitterLight : CCDrawNode
{
    /// <summary>
    /// A single particle. Stored as a struct for zero GC pressure.
    /// </summary>
    public struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public CCColor4F Color;
        public float Life;
        public float MaxLife;
        public float Size;
        public bool IsActive;
    }

    /// <summary>
    /// Delegate for custom particle update logic.
    /// </summary>
    public delegate void ParticleUpdateDelegate(ref Particle particle, float dt);

    private Particle[] _particles;
    private int _activeCount;
    private Random _random = new Random();

    /// <summary>
    /// Custom update callback. Called for each active particle every frame.
    /// If null, default behavior is applied (linear velocity, linear alpha fade).
    /// </summary>
    public ParticleUpdateDelegate OnUpdateParticle { get; set; }

    /// <summary>
    /// Default drag applied to particle velocity each frame (0 = no drag, 1 = full stop).
    /// Only used when OnUpdateParticle is null.
    /// </summary>
    public float DefaultDrag { get; set; } = 0.3f;

    /// <summary>
    /// Whether particles should use quadratic alpha fade (more natural) vs linear.
    /// Only used when OnUpdateParticle is null.
    /// </summary>
    public bool QuadraticFade { get; set; } = true;

    /// <summary>
    /// Whether this emitter is currently active (has living particles).
    /// </summary>
    public bool IsActive => _activeCount > 0;

    /// <summary>
    /// Number of currently active particles.
    /// </summary>
    public int ActiveParticleCount => _activeCount;

    /// <summary>
    /// Creates a lightweight particle emitter with a fixed maximum particle count.
    /// </summary>
    /// <param name="maxParticles">Maximum number of simultaneous particles.</param>
    public CCParticleEmitterLight(int maxParticles = 128)
    {
        _particles = new Particle[maxParticles];
        _activeCount = 0;
    }

    /// <summary>
    /// Emits particles from a position with the given parameters.
    /// </summary>
    /// <param name="position">World position to emit from.</param>
    /// <param name="count">Number of particles to emit.</param>
    /// <param name="speed">Base speed of emitted particles.</param>
    /// <param name="spreadAngle">Spread angle in radians (MathHelper.TwoPi for full circle).</param>
    /// <param name="baseAngle">Center angle of emission cone in radians.</param>
    /// <param name="color">Particle color.</param>
    /// <param name="life">Particle lifetime in seconds.</param>
    /// <param name="size">Particle render size (radius for DrawDot).</param>
    /// <param name="speedVariance">Random variance applied to speed (0-1 range).</param>
    /// <param name="lifeVariance">Random variance applied to lifetime (0-1 range).</param>
    public void Emit(CCPoint position, int count, float speed, float spreadAngle,
                     float baseAngle, CCColor4F color, float life = 1f, float size = 1f,
                     float speedVariance = 0.3f, float lifeVariance = 0.2f)
    {
        for (int i = 0; i < count; i++)
        {
            int slot = FindInactiveSlot();
            if (slot < 0) break;

            float angle = baseAngle + ((float)_random.NextDouble() - 0.5f) * spreadAngle;
            float spd = speed * (1f - speedVariance + (float)_random.NextDouble() * speedVariance * 2f);
            float lt = life * (1f - lifeVariance + (float)_random.NextDouble() * lifeVariance * 2f);

            ref var p = ref _particles[slot];
            p.Position = new Vector2(position.X, position.Y);
            p.Velocity = new Vector2((float)Math.Cos(angle) * spd, (float)Math.Sin(angle) * spd);
            p.Color = color;
            p.Life = lt;
            p.MaxLife = lt;
            p.Size = size;
            p.IsActive = true;
            _activeCount++;
        }
    }

    /// <summary>
    /// Emits particles with per-particle colors sampled from an array.
    /// Useful for explosions that match a sprite's pixel colors.
    /// </summary>
    public void Emit(CCPoint position, int count, float speed, float spreadAngle,
                     float baseAngle, CCColor4F[] colors, float life = 1f, float size = 1f,
                     float speedVariance = 0.3f, float lifeVariance = 0.2f)
    {
        for (int i = 0; i < count; i++)
        {
            int slot = FindInactiveSlot();
            if (slot < 0) break;

            float angle = baseAngle + ((float)_random.NextDouble() - 0.5f) * spreadAngle;
            float spd = speed * (1f - speedVariance + (float)_random.NextDouble() * speedVariance * 2f);
            float lt = life * (1f - lifeVariance + (float)_random.NextDouble() * lifeVariance * 2f);
            var color = colors[_random.Next(colors.Length)];

            ref var p = ref _particles[slot];
            p.Position = new Vector2(position.X, position.Y);
            p.Velocity = new Vector2((float)Math.Cos(angle) * spd, (float)Math.Sin(angle) * spd);
            p.Color = color;
            p.Life = lt;
            p.MaxLife = lt;
            p.Size = size;
            p.IsActive = true;
            _activeCount++;
        }
    }

    /// <summary>
    /// Updates all active particles and redraws. Call this once per frame.
    /// </summary>
    public void UpdateParticles(float dt)
    {
        Clear();

        if (_activeCount == 0) return;

        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            if (!p.IsActive) continue;

            p.Life -= dt;
            if (p.Life <= 0f)
            {
                p.IsActive = false;
                _activeCount--;
                continue;
            }

            if (OnUpdateParticle != null)
            {
                OnUpdateParticle(ref p, dt);
            }
            else
            {
                // Default behavior: velocity with drag, alpha fade
                p.Velocity *= 1f - DefaultDrag * dt;
                p.Position += p.Velocity * dt;
            }

            // Calculate alpha based on remaining life
            float t = p.Life / p.MaxLife;
            float alpha = QuadraticFade ? t * t : t;

            var drawColor = new CCColor4F(p.Color.R, p.Color.G, p.Color.B, p.Color.A * alpha);
            DrawDot(new CCPoint(p.Position.X, p.Position.Y), p.Size, drawColor);
        }
    }

    /// <summary>
    /// Stops all active particles immediately.
    /// </summary>
    public void StopAll()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            _particles[i].IsActive = false;
        }
        _activeCount = 0;
        Clear();
    }

    private int FindInactiveSlot()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            if (!_particles[i].IsActive) return i;
        }
        return -1;
    }
}
