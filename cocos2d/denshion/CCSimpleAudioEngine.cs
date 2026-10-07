#nullable disable
using System;
using System.Collections.Generic;
using Cocos2D;
using Microsoft.Xna.Framework.Audio;

namespace CocosDenshion;

public class CCSimpleAudioEngine
{
    /// <summary>
    /// The list of sounds that are configured for looping. These need to be stopped when the game pauses.
    /// </summary>
    private Dictionary<int, int> _LoopedSounds = new Dictionary<int, int>();

    private static Dictionary<int, CCEffectPlayer> s_List = new Dictionary<int, CCEffectPlayer>();
    private static CCMusicPlayer s_Music = new CCMusicPlayer();
    private static CCSimpleAudioEngine _Instance;
    private static bool _NoAudioHardware = false;

    // Throttling: tracks last play time per effect for minInterval support
    private Dictionary<int, float> _lastPlayTime = new Dictionary<int, float>();
    private static float _globalTime = 0f;


    /// <summary>
    /// The shared sound effect list. The key is the hashcode of the file path.
    /// </summary>
    public static Dictionary<int, CCEffectPlayer> SharedList
    {
        get
        {
            return (s_List);
        }
    }

    /// <summary>
    /// The shared music player.
    /// </summary>
    private static CCMusicPlayer SharedMusic
    {
        get 
        { 
            return(s_Music); 
        }
    }

    /// <summary>
    /// The singleton instance of this class. This property never throws.
    /// </summary>
    public static CCSimpleAudioEngine SharedEngine
    {
        get
        {
            if (_Instance == null)
            {
                try
                {
                    _Instance = new CCSimpleAudioEngine();
                }
                catch (Exception ex)
                {
                    CCLog.Log("Failed to initialize audio engine: {0}", ex.Message);
                    _NoAudioHardware = true;
                    _Instance = new CCSimpleAudioEngine();
                }
            }
            return _Instance;
        }
    }

    public float BackgroundMusicVolume
    {
        get { return SharedMusic.Volume; }
        set { SharedMusic.Volume = value; }
    }

    /**
    @brief The volume of the effects max value is 1.0,the min value is 0.0
    */

    public float EffectsVolume
    {
        get { return CCEffectPlayer.Volume; }
        set { CCEffectPlayer.Volume = value; }
    }

    public static string FullPath(string szPath)
    {
        // todo: return self now
        return szPath;
    }

    /**
    @brief Release the shared Engine object
    @warning It must be called before the application exit, or a memroy leak will be casued.
    */

    public void End ()
    {
        SharedMusic.Close ();

        lock (SharedList) {
            foreach (var kvp in SharedList) {
                kvp.Value.Close ();
            }

            SharedList.Clear ();
        }
    }

    /// <summary>
    /// Restore the media player's state to how it was prior to the game launch. You need to do this when the game terminates
    /// if you run music that clobbers the music that was playing before the game launched.
    /// </summary>
    public void RestoreMediaState()
    {
        // CCTask.RunAsync(CocosDenshion.CCSimpleAudioEngine.SharedEngine.RestoreMediaState); 
        SharedMusic.RestoreMediaState();
    }

    /// <summary>
    /// Save the media player's current playback state.
    /// </summary>
    public void SaveMediaState()
    {
        // CCTask.RunAsync(CocosDenshion.CCSimpleAudioEngine.SharedEngine.SaveMediaState); 
        SharedMusic.SaveMediaState();
    }

    /**
     @brief Preload background music
     @param pszFilePath The path of the background music file,or the FileName of T_SoundResInfo
     */

    public void PreloadBackgroundMusic(string pszFilePath)
    {
        if (_NoAudioHardware) return;
        SharedMusic.Open(FullPath(pszFilePath), pszFilePath.GetHashCode());
    }

    /**
    @brief Play background music
    @param pszFilePath The path of the background music file,or the FileName of T_SoundResInfo
    @param bLoop Whether the background music loop or not
    */

    public void PlayBackgroundMusic(string pszFilePath, bool bLoop)
    {
        if (null == pszFilePath)
        {
            return;
        }
        if (_NoAudioHardware)
        {
            return;
        }
        SharedMusic.Open(FullPath(pszFilePath), pszFilePath.GetHashCode());
        SharedMusic.Play(bLoop);
    }

    /**
    @brief Play background music
    @param pszFilePath The path of the background music file,or the FileName of T_SoundResInfo
    */

    public void PlayBackgroundMusic(string pszFilePath)
    {
        if (_NoAudioHardware) return;
        PlayBackgroundMusic(pszFilePath, false);
    }

    /**
    @brief Stop playing background music
    @param bReleaseData If release the background music data or not.As default value is false
    */

    public void StopBackgroundMusic(bool bReleaseData)
    {
        if (_NoAudioHardware) return;
        if (bReleaseData)
        {
            SharedMusic.Close();
        }
        else
        {
            SharedMusic.Stop();
        }
    }

    /**
    @brief Stop playing background music
    */

    public void StopBackgroundMusic()
    {
        if (_NoAudioHardware) return;
        StopBackgroundMusic(false);
    }

    /**
    @brief Pause playing background music
    */

    public void PauseBackgroundMusic()
    {
        if (_NoAudioHardware) return;
        SharedMusic.Pause();
    }

    /**
    @brief Resume playing background music
    */

    public void ResumeBackgroundMusic()
    {
        if (_NoAudioHardware) return;
        SharedMusic.Resume();
    }

    /**
    @brief Rewind playing background music
    */

    public void RewindBackgroundMusic()
    {
        if (_NoAudioHardware) return;
        SharedMusic.Rewind();
    }

    public bool WillPlayBackgroundMusic()
    {
        return false;
    }

    /**
    @brief Whether the background music is playing
    @return If is playing return true,or return false
    */

    public bool IsBackgroundMusicPlaying()
    {
        if (_NoAudioHardware) return (false);
        return SharedMusic.IsPlaying();
    }

    public void PauseEffect(int fxid) 
    {
        if (_NoAudioHardware) return;
        try
        {
            if (SharedList.ContainsKey(fxid))
            {
                SharedList[fxid].Pause();
            }
        }
        catch (NoAudioHardwareException ex)
        {
            CCLog.Log("NoAudioHardware! while playing a SoundEffect: {0}", fxid);
            CCLog.Log(ex.ToString());
            _NoAudioHardware = true;
        }
        catch (Exception ex)
        {
            CCLog.Log("Unexpected exception while playing a SoundEffect: {0}", fxid);
            CCLog.Log(ex.ToString());
        }
    }

    public void PauseAllEffects()
    {
        if (_NoAudioHardware) return;
        List<CCEffectPlayer> l = new List<CCEffectPlayer>();

        try
        {
            foreach (CCEffectPlayer p in l)
            {
                p.Pause();
            }
        }
        catch (Exception ex)
        {
            CCLog.Log("Unexpected exception while pausing all effects.");
            CCLog.Log(ex.ToString());
        }
    }

    public void ResumeAllEffects()
    {
        if (_NoAudioHardware) return;
        List<CCEffectPlayer> l = new List<CCEffectPlayer>();

        try
        {
            foreach (CCEffectPlayer p in l)
            {
                p.Resume();
            }
        }
        catch (Exception ex)
        {
            CCLog.Log("Unexpected exception while resuming all effects.");
            CCLog.Log(ex.ToString());
        }
    }

    public void StopAllEffects()
    {
        if (_NoAudioHardware) return;
        List<CCEffectPlayer> l = new List<CCEffectPlayer>();

        lock (SharedList)
        {
            try
            {
                l.AddRange(SharedList.Values);
                SharedList.Clear();
            }
            catch (Exception ex)
            {
                CCLog.Log("Unexpected exception while stopping all effects.");
                CCLog.Log(ex.ToString());
            }
        }
        foreach (CCEffectPlayer p in l)
        {
            p.Stop();
        }

    }

    public int PlayEffect(int fxid)
    {
        if (_NoAudioHardware) return(-1);
        PlayEffect(fxid, false);
        return (fxid);
    }

    public int PlayEffect(int fxid, bool bLoop)
    {
        if (_NoAudioHardware) return(-1);
        lock (SharedList)
        {
            try
            {
                if (SharedList.ContainsKey(fxid))
                {
                    SharedList[fxid].Play(bLoop);
                    if (bLoop)
                    {
                        _LoopedSounds[fxid] = fxid;
                    }
                }
            }
            catch (Exception ex)
            {
                CCLog.Log("Unexpected exception while playing a SoundEffect: {0}", fxid);
                CCLog.Log(ex.ToString());
            }
        }

        return fxid;
    }

    /// <summary>
    /// Play the sound effect with the given path and optionally set it to lopo.
    /// </summary>
    /// <param name="pszFilePath">The path to the sound effect file.</param>
    /// <param name="bLoop">True if the sound effect will play continuously, and false if it will play then stop.</param>
    /// <returns></returns>
    public int PlayEffect (string pszFilePath, bool bLoop)
    {
        if (_NoAudioHardware) return(-1);
        int nId = pszFilePath.GetHashCode();

        PreloadEffect (pszFilePath);

        lock (SharedList)
        {
            try
            {
                if (SharedList.ContainsKey(nId))
                {
                    SharedList[nId].Play(bLoop);
                    if (bLoop)
                    {
                        _LoopedSounds[nId] = nId;
                    }
                }
            }
            catch (Exception ex)
            {
                CCLog.Log("Unexpected exception while playing a SoundEffect: {0}", pszFilePath);
                CCLog.Log(ex.ToString());
            }
        }

        return nId;
    }
    
    /// <summary>
    /// Plays the given sound effect without looping.
    /// </summary>
    /// <param name="pszFilePath">The path to the sound effect</param>
    /// <returns></returns>
    public int PlayEffect(string pszFilePath)
    {
        if (_NoAudioHardware) return(-1);
        return PlayEffect(pszFilePath, false);
    }

    /// <summary>
    /// Plays the sound effect at the given volume (0.0 to 1.0).
    /// </summary>
    public int PlayEffect(string pszFilePath, float volume)
    {
        if (_NoAudioHardware) return (-1);
        int nId = pszFilePath.GetHashCode();

        PreloadEffect(pszFilePath);

        lock (SharedList)
        {
            try
            {
                if (SharedList.ContainsKey(nId))
                {
                    SharedList[nId].Play(false, volume);
                }
            }
            catch (Exception ex)
            {
                CCLog.Log("Unexpected exception while playing a SoundEffect: {0}", pszFilePath);
                CCLog.Log(ex.ToString());
            }
        }

        return nId;
    }

    /// <summary>
    /// Plays the sound effect only if the minimum interval (in seconds) has elapsed
    /// since the last time this effect was played. Useful for preventing sound stacking.
    /// </summary>
    public int PlayEffect(string pszFilePath, float volume, float minInterval)
    {
        if (_NoAudioHardware) return (-1);
        int nId = pszFilePath.GetHashCode();

        lock (_lastPlayTime)
        {
            if (_lastPlayTime.TryGetValue(nId, out float lastTime))
            {
                if (_globalTime - lastTime < minInterval)
                    return nId;
            }
            _lastPlayTime[nId] = _globalTime;
        }

        return PlayEffect(pszFilePath, volume);
    }

    /// <summary>
    /// Call this each frame with the delta time to enable throttled playback via minInterval.
    /// </summary>
    public void UpdateThrottleTimers(float dt)
    {
        _globalTime += dt;
    }

    /// <summary>
    /// Plays a sound effect and returns a handle for per-instance control
    /// (volume, pan, pitch, stop, pause, resume).
    /// Returns null if audio hardware is unavailable or the effect cannot be played.
    /// The caller owns the returned handle and must call Dispose() when finished
    /// to release the underlying SoundEffectInstance.
    /// </summary>
    /// <param name="pszFilePath">Path to the sound effect file.</param>
    /// <param name="volume">Volume from 0.0 to 1.0.</param>
    /// <param name="bLoop">Whether to loop the sound.</param>
    public CCSoundHandle PlayEffectHandled(string pszFilePath, float volume = 1f, bool bLoop = false)
    {
        if (_NoAudioHardware || string.IsNullOrEmpty(pszFilePath)) return null;

        int nId = pszFilePath.GetHashCode();
        PreloadEffect(pszFilePath);

        SoundEffectInstance instance = null;
        lock (SharedList)
        {
            try
            {
                CCEffectPlayer player;
                if (SharedList.TryGetValue(nId, out player))
                {
                    instance = player.CreateInstance();
                    if (instance == null) return null;

                    instance.Volume = Math.Max(0f, Math.Min(1f, volume));
                    instance.IsLooped = bLoop;
                    instance.Play();

                    return new CCSoundHandle(instance, nId);
                }
            }
            catch (Exception ex)
            {
                if (instance != null && !instance.IsDisposed)
                {
                    instance.Dispose();
                }
                CCLog.Log("Unexpected exception while playing a SoundEffect: {0}", pszFilePath);
                CCLog.Log(ex.ToString());
            }
        }

        return null;
    }

    /// <summary>
    /// Stops the sound effect with the given id.
    /// </summary>
    /// <param name="nSoundId"></param>
    public void StopEffect(int nSoundId)
    {
        if (_NoAudioHardware) return;
        lock (SharedList)
        {
            if (SharedList.ContainsKey(nSoundId))
            {
                SharedList[nSoundId].Stop();
            }
        }
        lock (_LoopedSounds)
        {
            if (_LoopedSounds.ContainsKey(nSoundId))
            {
                _LoopedSounds.Remove(nSoundId);
            }
        }
    }

    /// <summary>
    /// Stops all of the sound effects that are currently playing and looping.
    /// </summary>
    public void StopAllLoopingEffects()
    {
        if (_NoAudioHardware) return;
        lock (SharedList)
        {
            if (_LoopedSounds.Count > 0)
            {
                int[] a = new int[_LoopedSounds.Keys.Count];
                _LoopedSounds.Keys.CopyTo(a, 0);
                foreach (int key in a)
                {
                    StopEffect(key);
                }
            }
        }
    }

    /**
    @brief  		preload a compressed audio file
    @details	    the compressed audio will be decode to wave, then write into an 
    internal buffer in SimpleaudioEngine
    */


    /// <summary>
    /// Load the sound effect found with the given path. The sound effect is only loaded one time and the
    /// effect is cached as an instance of EffectPlayer.
    /// </summary>
    public void PreloadEffect(string pszFilePath)
    {
        if (_NoAudioHardware) return;
        if (string.IsNullOrEmpty(pszFilePath))
        {
            return;
        }

        int nId = pszFilePath.GetHashCode();
        lock (SharedList)
        {
            if (SharedList.ContainsKey(nId))
            {
                return;
            }
        }
        try
        {
            CCEffectPlayer eff = new CCEffectPlayer();
            eff.Open(FullPath(pszFilePath), nId);
            SharedList[nId] = eff;
        }
        catch (NoAudioHardwareException ex)
        {
            _NoAudioHardware = true;
            CCLog.Log(ex.ToString());
        }
    }

    /**
    @brief  		unload the preloaded effect from internal buffer
    @param[in]		pszFilePath		The path of the effect file,or the FileName of T_SoundResInfo
    */

    public void UnloadEffect (string pszFilePath)
    {
        if (_NoAudioHardware) return;
        int nId = pszFilePath.GetHashCode();
        lock (SharedList) {
            if (SharedList.ContainsKey(nId))
            {
                SharedList.Remove(nId);
            }
        }
        lock (_LoopedSounds)
        {
            if (_LoopedSounds.ContainsKey(nId))
            {
                _LoopedSounds.Remove(nId);
            }
        }
    }

    #region Background Music Fade

    private float _fadeTargetVolume;
    private float _fadeStartVolume;
    private float _fadeDuration;
    private float _fadeElapsed;
    private bool _isFading;

    /// <summary>
    /// Whether a background music fade is currently in progress.
    /// </summary>
    public bool IsFading => _isFading;

    /// <summary>
    /// Fades the background music volume from the current level to a target level over a duration.
    /// Call UpdateFade(dt) each frame while fading.
    /// </summary>
    /// <param name="targetVolume">Target volume (0.0 to 1.0).</param>
    /// <param name="duration">Fade duration in seconds.</param>
    public void FadeBackgroundMusic(float targetVolume, float duration)
    {
        if (_NoAudioHardware) return;
        _fadeStartVolume = BackgroundMusicVolume;
        _fadeTargetVolume = Math.Max(0f, Math.Min(1f, targetVolume));
        _fadeDuration = Math.Max(0.01f, duration);
        _fadeElapsed = 0f;
        _isFading = true;
    }

    /// <summary>
    /// Updates the background music fade. Call once per frame.
    /// Also updates throttle timers. Can be called unconditionally.
    /// </summary>
    public void Update(float dt)
    {
        UpdateThrottleTimers(dt);

        if (!_isFading) return;

        _fadeElapsed += dt;
        float t = Math.Min(_fadeElapsed / _fadeDuration, 1f);
        BackgroundMusicVolume = _fadeStartVolume + (_fadeTargetVolume - _fadeStartVolume) * t;

        if (t >= 1f)
        {
            _isFading = false;
        }
    }

    #endregion
}