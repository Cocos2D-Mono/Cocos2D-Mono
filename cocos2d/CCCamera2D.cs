#nullable disable
using System;

namespace Cocos2D;

/// <summary>
/// A 2D-focused camera with scroll, shake, follow, zoom, and viewport culling.
/// Unlike CCCamera (which is 3D/gluLookAt-based), CCCamera2D operates in 2D space
/// and applies its transform by modifying a target node's position and scale.
///
/// Usage:
///   var camera = new CCCamera2D(viewportWidth, viewportHeight);
///   camera.Follow(playerNode, smoothing: 0.1f);
///   // In update loop:
///   camera.Update(dt);
///   camera.ApplyTo(gameLayer);
/// </summary>
public class CCCamera2D
{
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private Random _shakeRandom = new Random();

    /// <summary>
    /// The camera's X scroll position in world coordinates.
    /// </summary>
    public float ScrollX { get; set; }

    /// <summary>
    /// The camera's Y scroll position in world coordinates.
    /// </summary>
    public float ScrollY { get; set; }

    /// <summary>
    /// Zoom level. 1.0 = normal, 2.0 = 2x zoom in, 0.5 = zoom out.
    /// </summary>
    public float Zoom { get; set; } = 1f;

    /// <summary>
    /// Speed for auto-scrolling (units per second). Set to 0 to disable.
    /// </summary>
    public float AutoScrollSpeedX { get; set; }

    /// <summary>
    /// Speed for auto-scrolling (units per second). Set to 0 to disable.
    /// </summary>
    public float AutoScrollSpeedY { get; set; }

    /// <summary>
    /// The node the camera is following, if any.
    /// </summary>
    public CCNode FollowTarget { get; private set; }

    /// <summary>
    /// Smoothing factor for follow mode (0 = instant, 1 = very slow). Typical value: 0.1.
    /// </summary>
    public float FollowSmoothing { get; set; } = 0.1f;

    /// <summary>
    /// The current shake offset applied to the camera position.
    /// </summary>
    public CCPoint ShakeOffset { get; private set; }

    /// <summary>
    /// Whether the camera is currently shaking.
    /// </summary>
    public bool IsShaking => _shakeTimer > 0f;

    /// <summary>
    /// The viewport width used for culling calculations.
    /// </summary>
    public float ViewportWidth { get; set; }

    /// <summary>
    /// The viewport height used for culling calculations.
    /// </summary>
    public float ViewportHeight { get; set; }

    /// <summary>
    /// Creates a new 2D camera.
    /// </summary>
    /// <param name="viewportWidth">The width of the visible area.</param>
    /// <param name="viewportHeight">The height of the visible area.</param>
    public CCCamera2D(float viewportWidth, float viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    /// <summary>
    /// Creates a new 2D camera with default viewport size (set later via ViewportWidth/Height).
    /// </summary>
    public CCCamera2D()
    {
    }

    /// <summary>
    /// Sets the camera to follow a target node.
    /// </summary>
    /// <param name="target">The node to follow.</param>
    /// <param name="smoothing">Smoothing factor (0 = instant snap, higher = smoother/slower).</param>
    public void Follow(CCNode target, float smoothing = 0.1f)
    {
        FollowTarget = target;
        FollowSmoothing = smoothing;
    }

    /// <summary>
    /// Stops following the current target.
    /// </summary>
    public void Unfollow()
    {
        FollowTarget = null;
    }

    /// <summary>
    /// Triggers a screen shake effect.
    /// </summary>
    /// <param name="intensity">Maximum pixel offset of the shake.</param>
    /// <param name="duration">Duration in seconds.</param>
    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimer = duration;
    }

    /// <summary>
    /// Updates camera state. Call this once per frame.
    /// </summary>
    public void Update(float dt)
    {
        // Auto-scroll
        ScrollX += AutoScrollSpeedX * dt;
        ScrollY += AutoScrollSpeedY * dt;

        // Follow target
        if (FollowTarget != null)
        {
            float targetX = FollowTarget.PositionX - ViewportWidth * 0.5f / Zoom;
            float targetY = FollowTarget.PositionY - ViewportHeight * 0.5f / Zoom;

            if (FollowSmoothing <= 0f)
            {
                ScrollX = targetX;
                ScrollY = targetY;
            }
            else
            {
                float lerpFactor = 1f - (float)Math.Pow(FollowSmoothing, dt * 60f);
                ScrollX += (targetX - ScrollX) * lerpFactor;
                ScrollY += (targetY - ScrollY) * lerpFactor;
            }
        }

        // Screen shake
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= dt;
            float decay = _shakeTimer / _shakeDuration;
            float currentIntensity = _shakeIntensity * decay;

            float offsetX = ((float)_shakeRandom.NextDouble() * 2f - 1f) * currentIntensity;
            float offsetY = ((float)_shakeRandom.NextDouble() * 2f - 1f) * currentIntensity;
            ShakeOffset = new CCPoint(offsetX, offsetY);
        }
        else
        {
            ShakeOffset = CCPoint.Zero;
        }
    }

    /// <summary>
    /// Checks whether a world-space bounding rectangle is visible within the camera's viewport.
    /// Useful for culling off-screen entities.
    /// </summary>
    public bool IsVisible(CCRect worldBounds)
    {
        float vw = ViewportWidth / Zoom;
        float vh = ViewportHeight / Zoom;

        return worldBounds.MaxX >= ScrollX &&
               worldBounds.MinX <= ScrollX + vw &&
               worldBounds.MaxY >= ScrollY &&
               worldBounds.MinY <= ScrollY + vh;
    }

    /// <summary>
    /// Applies the camera transform to a target node (typically a CCLayer).
    /// Sets the node's position to offset all children by the camera's scroll + shake.
    /// </summary>
    public void ApplyTo(CCNode layer)
    {
        float x = -ScrollX + ShakeOffset.X;
        float y = -ScrollY + ShakeOffset.Y;
        layer.Position = new CCPoint(x, y);

        layer.ScaleX = Zoom;
        layer.ScaleY = Zoom;
    }
}
