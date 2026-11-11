using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public partial class CCTextureCache : IDisposable, ICCSelectorProtocol
{
    struct AsyncStruct
    {
        public string  FileName;
        public Action<CCTexture2D?>? Action;
    };

    private List<AsyncStruct> _asyncLoadedImages = new List<AsyncStruct>();
    private Action _processingAction;
    private object? _task;

    private static CCTextureCache? s_sharedTextureCache;

    private readonly object _dictLock = new object();
    protected Dictionary<string, CCTexture2D> m_pTextures = new Dictionary<string, CCTexture2D>();

    ~CCTextureCache() 
    {
        Dispose();
    }

    private CCTextureCache()
    {
        _processingAction = new Action(
            () =>
            {
                while (true)
                {
                    AsyncStruct image;

                    lock (_asyncLoadedImages)
                    {
                        if (_asyncLoadedImages.Count == 0)
                        {
                            _task = null;
                            return;
                        }
                        image = _asyncLoadedImages[0];
                        _asyncLoadedImages.RemoveAt(0);
                    }

                    try
                    {
                        var texture = AddImage(image.FileName);
						CCLog.Log("Loaded texture: {0}", image.FileName);
                        Action<CCTexture2D?>? action = image.Action;
                        if (action != null)
                        {
                            // The scheduler isn't thread-safe, so the callback waits for the game
                            // thread's next tick instead of being scheduled from this thread.
                            CCDirector.SharedDirector.Scheduler.RunOnNextTick(() => action(texture));
                        }
                    }
                    catch (Exception ex)
                    {
						    CCLog.Log("Failed to load image {0}", image.FileName);
                        CCLog.Log(ex.ToString());
                    }
                }
            }
            );
    }

    public void Update(float dt)
    {
    }

    public static CCTextureCache SharedTextureCache
    {
        get 
        {
            if (s_sharedTextureCache == null)
            {
                s_sharedTextureCache = new CCTextureCache();
            }
            return (s_sharedTextureCache);
        }
    }

    public static void PurgeSharedTextureCache()
    {
        if (s_sharedTextureCache != null)
        {
            s_sharedTextureCache.Dispose();
            s_sharedTextureCache = null;
        }
    }

    // Every method that reads or changes m_pTextures takes _dictLock, because AddImageAsync's worker
    // adds to it from another thread.

    public void UnloadContent()
    {
        lock (_dictLock)
        {
            m_pTextures.Clear();
        }
    }

    public bool Contains(string assetFile)
    {
        lock (_dictLock)
        {
            return m_pTextures.ContainsKey(assetFile);
        }
    }

    public void AddImageAsync(string fileimage, Action<CCTexture2D?>? action)
    {
        Debug.Assert(!String.IsNullOrEmpty(fileimage), "TextureCache: fileimage MUST not be NULL");

        lock (_asyncLoadedImages)
        {
            _asyncLoadedImages.Add(new AsyncStruct() {FileName = fileimage, Action = action});

            // Under the lock: the worker clears _task under it when it finds nothing left to load,
            // so it can't finish and clear it before this sets it.
            if (_task == null)
            {
                _task = CCTask.RunAsync(_processingAction);
            }
        }
    }

    /// <summary>
    /// Create the dictionary key for this texture. Duplicate assets using different compression are not supported.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private string CreateAssetKey(string file)
    {
        var assetName = file;
        if (Path.HasExtension(assetName))
        {
            assetName = CCFileUtils.RemoveExtension(assetName);
        }
        return (assetName);
    }

    // Loads the texture for a file. It's a field so that the unit tests can supply the texture:
    // loading an image needs a graphics device.
    private Func<string, CCTexture2D?> _loadImage = LoadImage;

    private static CCTexture2D? LoadImage(string file)
    {
        var texture = new CCTexture2D();
        return texture.InitWithFile(file) ? texture : null;
    }

    public CCTexture2D? AddImage(string fileimage)
    {
        Debug.Assert(!String.IsNullOrEmpty(fileimage), "TextureCache: fileimage MUST not be NULL");

        CCTexture2D? texture = null;

        var assetName = CreateAssetKey(fileimage);

        lock (_dictLock)
        {
            m_pTextures.TryGetValue(assetName, out texture);
        }
        if (texture == null)
        {
            // The load runs outside the lock. On OpenGL, a thread other than the game thread waits
            // for the game thread to create the texture, so the game thread mustn't wait for the
            // lock while AddImageAsync's worker loads.
            CCTexture2D? loaded = _loadImage(fileimage);
            if (loaded == null)
            {
                return null;
            }

            // Another thread can add the same file meanwhile, as when the game thread adds a file
            // that AddImageAsync is loading. The texture stored first is kept, so that every caller
            // gets the one in the cache.
            lock (_dictLock)
            {
                if (!m_pTextures.TryGetValue(assetName, out texture))
                {
                    m_pTextures.Add(assetName, loaded);
                    texture = loaded;
                }
            }

            if (texture != loaded)
            {
                loaded.Dispose();
            }
        }
        else if (!texture.IsTextureDefined)
        {
            texture.Reinit();
        }
        return texture;
    }

    public CCTexture2D? AddImage(byte[] data, string assetName, SurfaceFormat format)
    {
        lock (_dictLock)
        {
            CCTexture2D? texture;

            if (!m_pTextures.TryGetValue(assetName, out texture))
            {
                texture = new CCTexture2D();
                
                if (texture.InitWithData(data, format))
                {
                    m_pTextures.Add(assetName, texture);
                }
                else
                {
                    return null;
                }
            }
            return texture;
        }
    }

    public CCTexture2D? AddRawImage<T>(T[] data, int width, int height, string assetName, SurfaceFormat format,
                                      bool premultiplied) where T : struct
    {
        return AddRawImage(data, width, height, assetName, format, premultiplied, false, new CCSize(width, height));
    }

    public CCTexture2D? AddRawImage<T>(T[] data, int width, int height, string assetName, SurfaceFormat format,
                                      bool premultiplied, bool mipMap) where T : struct
    {
        return AddRawImage(data, width, height, assetName, format, premultiplied, mipMap, new CCSize(width, height));
    }

     public CCTexture2D? AddRawImage<T>(T[] data, int width, int height, string assetName, SurfaceFormat format,
                                      bool premultiplied, bool mipMap, CCSize contentSize) where T : struct
    {
        CCTexture2D? texture;

        lock (_dictLock)
        {
            if (!m_pTextures.TryGetValue(assetName, out texture))
            {
                texture = new CCTexture2D();
                
                if (texture.InitWithRawData(data, format, width, height, premultiplied, mipMap, contentSize))
                {
                    m_pTextures.Add(assetName, texture);
                }
                else
                {
                    return null;
                }
            }
        }
        return texture;
    }

		public CCTexture2D? this[string key]
		{
			get 
			{
				return TextureForKey (key);
			}
		}

    public CCTexture2D? TextureForKey(string key)
    {
        CCTexture2D? texture = null;
        try
        {
            key = CreateAssetKey(key);
            lock (_dictLock)
            {
                m_pTextures.TryGetValue(key, out texture);
            }
        }
        catch (ArgumentNullException)
        {
            CCLog.Log("Texture of key {0} is not exist.", key);
        }

        return texture;
    }

    public void RemoveAllTextures()
    {
        lock (_dictLock)
        {
            m_pTextures.Clear();
        }
    }

    public void RemoveUnusedTextures()
    {
        // Held from the snapshot to the restore, so that the worker can't add a texture in between,
        // which the restore would then drop or collide with.
        lock (_dictLock)
        {
            if (m_pTextures.Count > 0)
            {
                var tmp = CCCachePurge.TakeAsWeakReferences(m_pTextures);
#if DEBUG
                int original = tmp.Count;
#endif

                GC.Collect();

#if DEBUG
                int culled = 0;
                long memory = 0L;
#endif
                foreach (var pair in tmp)
                {
                    if (pair.Value.Target is CCTexture2D tex)
                    {
                        m_pTextures[pair.Key] = tex;
#if DEBUG
                        memory += (long)tex.TotalBytes;
                        CCLog.Log("CCTextureCache: RemoveUnusedTextures, saving {0}", tex.ToString());
#endif
                    }
#if DEBUG
                    else {
                        culled++;
                    }
#endif
                }
#if DEBUG
                memory = memory >> 10; // KB
                memory = memory >> 10; // MB
                CCLog.Log("CCTextureCache: RemoveUnusedTextures removed {0} out of {1} textures for {2} MB.", culled, original, memory);
#endif
            }
        }
    }

    public void RemoveTexture(CCTexture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        lock (_dictLock)
        {
            string? key = null;

            foreach (var pair in m_pTextures)
            {
                if (pair.Value == texture)
                {
                    key = pair.Key;
                    break;
                }
            }

            if (key != null)
            {
                m_pTextures.Remove(key);
            }
        }
    }

    public void RemoveTextureForKey(string textureKeyName)
    {
        if (String.IsNullOrEmpty(textureKeyName))
        {
            return;
        }
        textureKeyName = CreateAssetKey(textureKeyName);
        lock (_dictLock)
        {
            m_pTextures.Remove(textureKeyName);
        }
    }

    public void DumpCachedTextureInfo()
    {
        int count = 0;
        int total = 0;

        List<KeyValuePair<string, CCTexture2D>> copy;
        lock (_dictLock)
        {
            copy = m_pTextures.ToList();
        }

        foreach (var pair in copy)
        {
            var texture = pair.Value.XNATexture;

            if (texture != null)
            {
                var bytes = texture.Width * texture.Height * (int)pair.Value.BytesPerPixelForFormat;
                CCLog.Log("{0} {1} x {2} => {3} KB.", pair.Key, texture.Width, texture.Height, bytes / 1024);
                total += bytes;
            }

            count++;
        }
        CCLog.Log("{0} textures, for {1} KB ({2:00.00} MB)", count, total / 1024, total / (1024f * 1024f));
    }

    public void Dispose()
    {
        if (m_pTextures != null)
        {
            // A copy, so the textures are disposed outside the lock.
            CCTexture2D[] textures;
            lock (_dictLock)
            {
                textures = m_pTextures.Values.ToArray();
            }
            foreach (CCTexture2D t in textures)
            {
                t.Dispose();
            }
        }
    }
}