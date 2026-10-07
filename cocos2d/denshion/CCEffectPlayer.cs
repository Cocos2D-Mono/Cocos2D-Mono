using System;
using Cocos2D;
using Microsoft.Xna.Framework.Audio;

namespace CocosDenshion;

public class CCEffectPlayer
{
    public static ulong s_mciError;
    // Null until an effect is opened, and again after Close.
    private SoundEffect? _effect;
    private SoundEffectInstance? _sfxInstance;
    private int _soundId;

    public CCEffectPlayer()
    {
        _soundId = 0;
    }

    public static float Volume
    {
        get { return SoundEffect.MasterVolume; }
        set
        {
            if (value >= 0.0f && value <= 1.0f)
            {
                SoundEffect.MasterVolume = value;
            }
        }
    }

    ~CCEffectPlayer()
    {
        Close();
    }

    public void Open(string pFileName, int uId)
    {
        if (string.IsNullOrEmpty(pFileName))
        {
            return;
        }

        Close();

        try
        {
            _effect = CCContentManager.SharedContentManager.Load<SoundEffect>(pFileName);
        }
        catch (Exception)
        {
            string srcfile = pFileName;
            if (srcfile.IndexOf('.') > -1)
            {
                srcfile = srcfile.Substring(0, srcfile.LastIndexOf('.'));
                _effect = CCContentManager.SharedContentManager.Load<SoundEffect>(srcfile);
            }
        }
        // Do not get an instance here b/c it is very slow. 
        //_sfxInstance = _effect.CreateInstance();
        _soundId = uId;
    }

    public void Play(bool bLoop)
    {
        if (null == _effect)
        {
            return;
        }
        if (bLoop)
        {
            // If looping, then get an instance of this sound effect so that it can be
            // stopped.
            _sfxInstance = _effect.CreateInstance();
            _sfxInstance.IsLooped = true;
        }
        if (_sfxInstance != null)
        {
            _sfxInstance.Play();
        }
        else
        {
            _effect.Play();
        }
    }

    public void Play()
    {
        Play(false);
    }

    /// <summary>
    /// Plays the sound effect with per-instance volume control.
    /// </summary>
    /// <param name="bLoop">Whether to loop the sound.</param>
    /// <param name="volume">Volume from 0.0 to 1.0.</param>
    public void Play(bool bLoop, float volume)
    {
        if (null == _effect)
        {
            return;
        }

        // For non-looping sounds without instance control, use the lightweight static Play
        if (!bLoop)
        {
            _effect.Play(Math.Max(0f, Math.Min(1f, volume)), 0f, 0f);
            return;
        }

        // Dispose previous instance to prevent resource leaks
        if (_sfxInstance != null && !_sfxInstance.IsDisposed)
        {
            _sfxInstance.Stop();
            _sfxInstance.Dispose();
        }

        _sfxInstance = _effect.CreateInstance();
        _sfxInstance.IsLooped = bLoop;
        _sfxInstance.Volume = Math.Max(0f, Math.Min(1f, volume));
        _sfxInstance.Play();
    }

    /// <summary>
    /// Creates a new SoundEffectInstance for this effect.
    /// Returns null if no effect is loaded.
    /// </summary>
    internal SoundEffectInstance? CreateInstance()
    {
        if (_effect == null) return null;
        return _effect.CreateInstance();
    }

    public void Close()
    {
        Stop();

        _effect = null;
    }

    public void Pause()
    {
        if (_sfxInstance != null && !_sfxInstance.IsDisposed && _sfxInstance.State == SoundState.Playing)
        {
            _sfxInstance.Pause();
        }
//            CCLog.Log("Pause is invalid for sound effect");
    }

    public void Resume()
    {
        if (_sfxInstance != null && !_sfxInstance.IsDisposed && _sfxInstance.State == SoundState.Paused)
        {
            _sfxInstance.Play();
        }
//            CCLog.Log("Resume is invalid for sound effect");
    }

    public void Stop()
    {
        if (_sfxInstance != null && !_sfxInstance.IsDisposed && _sfxInstance.State == SoundState.Playing)
        {
            _sfxInstance.Stop();
        }
//            CCLog.Log("Stop is invalid for sound effect");
    }

    public void Rewind()
    {
        CCLog.Log("Rewind is invalid for sound effect");
    }

    public bool IsPlaying()
    {
        if (_sfxInstance != null)
        {
            return (_sfxInstance.State == SoundState.Playing);
        }
//            CCLog.Log("IsPlaying is invalid for sound effect");
        return false;
    }

    public int SoundID
    {
        get { return _soundId; }
    }

    // the volume is gloabal, it will affect other effects' volume
}