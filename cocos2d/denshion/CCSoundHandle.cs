#nullable disable
using System;
using Microsoft.Xna.Framework.Audio;

namespace CocosDenshion;

/// <summary>
/// A handle to an individual sound effect instance, providing per-instance
/// control over volume, pan, pitch, and playback state.
/// Obtained from CCSimpleAudioEngine.PlayEffectHandled().
/// The caller owns this handle and must call Dispose() when finished
/// to release the underlying SoundEffectInstance.
/// </summary>
public class CCSoundHandle : IDisposable
{
    private SoundEffectInstance _instance;
    private readonly int _soundId;
    private bool _disposed;

    internal CCSoundHandle(SoundEffectInstance instance, int soundId)
    {
        _instance = instance;
        _soundId = soundId;
    }

    /// <summary>
    /// The sound ID (hash of the file path) for this effect.
    /// </summary>
    public int SoundId
    {
        get { return _soundId; }
    }

    /// <summary>
    /// Whether this handle's sound instance is currently playing.
    /// </summary>
    public bool IsPlaying
    {
        get { return _instance != null && !_instance.IsDisposed && _instance.State == SoundState.Playing; }
    }

    /// <summary>
    /// Whether this handle's sound instance is paused.
    /// </summary>
    public bool IsPaused
    {
        get { return _instance != null && !_instance.IsDisposed && _instance.State == SoundState.Paused; }
    }

    /// <summary>
    /// Whether this handle has been disposed or its instance is no longer valid.
    /// </summary>
    public bool IsDisposed
    {
        get { return _disposed || _instance == null || _instance.IsDisposed; }
    }

    /// <summary>
    /// Gets or sets the volume for this sound instance (0.0 to 1.0).
    /// </summary>
    public float Volume
    {
        get
        {
            if (IsDisposed) return 0f;
            return _instance.Volume;
        }
        set
        {
            if (IsDisposed) return;
            _instance.Volume = Math.Max(0f, Math.Min(1f, value));
        }
    }

    /// <summary>
    /// Gets or sets the pan for this sound instance (-1.0 left to 1.0 right).
    /// </summary>
    public float Pan
    {
        get
        {
            if (IsDisposed) return 0f;
            return _instance.Pan;
        }
        set
        {
            if (IsDisposed) return;
            _instance.Pan = Math.Max(-1f, Math.Min(1f, value));
        }
    }

    /// <summary>
    /// Gets or sets the pitch adjustment for this sound instance (-1.0 to 1.0).
    /// </summary>
    public float Pitch
    {
        get
        {
            if (IsDisposed) return 0f;
            return _instance.Pitch;
        }
        set
        {
            if (IsDisposed) return;
            _instance.Pitch = Math.Max(-1f, Math.Min(1f, value));
        }
    }

    /// <summary>
    /// Gets or sets whether this sound instance loops.
    /// </summary>
    public bool IsLooped
    {
        get
        {
            if (IsDisposed) return false;
            return _instance.IsLooped;
        }
        set
        {
            if (IsDisposed) return;
            _instance.IsLooped = value;
        }
    }

    /// <summary>
    /// Stops playback of this sound instance.
    /// </summary>
    public void Stop()
    {
        if (IsDisposed) return;
        _instance.Stop();
    }

    /// <summary>
    /// Pauses playback of this sound instance.
    /// </summary>
    public void Pause()
    {
        if (IsDisposed) return;
        if (_instance.State == SoundState.Playing)
            _instance.Pause();
    }

    /// <summary>
    /// Resumes playback of this paused sound instance.
    /// </summary>
    public void Resume()
    {
        if (IsDisposed) return;
        if (_instance.State == SoundState.Paused)
            _instance.Play();
    }

    /// <summary>
    /// Disposes the underlying SoundEffectInstance, freeing resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_instance != null && !_instance.IsDisposed)
        {
            _instance.Stop();
            _instance.Dispose();
        }
        _instance = null;
    }
}
