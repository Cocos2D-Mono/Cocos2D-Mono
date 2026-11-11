using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public class CCContentManager : ContentManager
{
    // Set by Initialize, which CCApplication.LoadContent and CCGameView call before any content
    // loads.
    public static CCContentManager SharedContentManager = null!;

    [MemberNotNull(nameof(SharedContentManager))]
    internal static void Initialize(IServiceProvider serviceProvider, string rootDirectory)
    {
        SharedContentManager = new CCContentManager(serviceProvider, rootDirectory);
#if IOS
        InitializeContentTypeReaders();
#endif
    }

#if IOS
    private static bool s_readersInited;

    private static void InitializeContentTypeReaders()
    {
        // Please read the following discussions for the reasons of this.
        // http://monogame.codeplex.com/discussions/393775
        // http://monogame.codeplex.com/discussions/396792
        // 
        // https://github.com/mono/MonoGame/pull/726
        //
        // Also search Google for -> ContentTypeReaderManager.AddTypeCreator

        if (s_readersInited)
        {
            return;
        }

        // .FNT Reader
        //ContentTypeReaderManager.AddTypeCreator(
        //    "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[Cocos2D.CCBMFontConfiguration+CCBMFontDef, cocos2d-xna, Version=2.2.4.0, Culture=neutral, PublicKeyToken=null]]",
        //    () => new DictionaryReader<Int32, CCBMFontConfiguration.CCBMFontDef>()

        //    );

        //ContentTypeReaderManager.AddTypeCreator(
        //    "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[Cocos2D.CCBMFontConfiguration+CCKerningHashElement, cocos2d-xna, Version=2.2.4.0, Culture=neutral, PublicKeyToken=null]]",
        //    () => new DictionaryReader<Int32, CCBMFontConfiguration.CCKerningHashElement>()

        //    );
        ContentTypeReaderManager.AddTypeCreator(
            "Microsoft.Xna.Framework.Content.ReflectiveReader`1[[Cocos2D.CCRect, cocos2d-mono, Version=2.3.7.0, Culture=neutral, PublicKeyToken=null]]",
            () => new CCRectReader()

            );

        ContentTypeReaderManager.AddTypeCreator(
            "Microsoft.Xna.Framework.Content.ReflectiveReader`1[[Cocos2D.CCPoint, cocos2d-mono, Version=2.3.7.0, Culture=neutral, PublicKeyToken=null]]",
            () => new CCPointReader()

            );
        ContentTypeReaderManager.AddTypeCreator(
            "Microsoft.Xna.Framework.Content.ReflectiveReader`1[[Cocos2D.CCSize, cocos2d-mono, Version=2.3.7.0, Culture=neutral, PublicKeyToken=null]]",
            () => new CCSizeReader()

            );

        ContentTypeReaderManager.AddTypeCreator(
            "Microsoft.Xna.Framework.Content.ReflectiveReader`1[[Cocos2D.CCBMFontConfiguration+CCKerningHashElement, cocos2d-mono, Version=2.3.7.0, Culture=neutral, PublicKeyToken=null]]",
            () => new KerningHashElementReader()

            );

        ContentTypeReaderManager.AddTypeCreator(
            "Microsoft.Xna.Framework.Content.ReflectiveReader`1[[Cocos2D.CCBMFontConfiguration+CCBMFontPadding, cocos2d-mono, Version=2.3.7.0, Culture=neutral, PublicKeyToken=null]]",
            () => new CCBMFontPaddingtReader()

            );

        s_readersInited = true;
    }
#endif

    private class AssetEntry
    {
        public readonly string AssetFileName;
        public readonly bool WeakReference;
        public readonly bool UseContentReader;
        private object? _asset;

        public AssetEntry(object? asset, string assetFileName, bool weakReference, bool useContentReader)
        {
            AssetFileName = assetFileName;
            WeakReference = weakReference;
            UseContentReader = useContentReader;
            Asset = asset;
        }

        public object? Asset
        {
            set
            {
                if (WeakReference)
                {
                    _asset = new WeakReference(value);
                }
                else
                {
                    _asset = value;
                }
            }
            get
            {
                if (WeakReference)
                {
                    if (_asset is WeakReference reference && reference.IsAlive)
                    {
                        return reference.Target;
                    }
                    return null;
                }
                else
                {
                    return _asset;
                }
            }
        }
    }

    private Dictionary<string, AssetEntry> _loadedAssets;
    
    private Dictionary<string, string> _assetLookupDict = new Dictionary<string, string>();
    private List<string> _searchPaths = new List<string>();
    private List<string> _searchResolutionsOrder = new List<string>(); 

    private Dictionary<Tuple<string, Type>, string?> _failedAssets = new Dictionary<Tuple<string, Type>, string?>();

    // Guards the collections above and _disposables: AddImageAsync's worker loads through the shared
    // content manager while the game thread loads too. It's never held while an asset loads or is
    // disposed. On OpenGL, a thread other than the game thread waits for the game thread to create a
    // texture, so the game thread mustn't wait for this lock while the worker loads.
    private readonly object _lock = new object();

    // What this content manager's loads created that Unload disposes. MonoGame records them in a list
    // of its own, which has no lock, unless ReadAsset is given a callback to record them.
    private readonly List<IDisposable> _disposables = new List<IDisposable>();
    private readonly Action<IDisposable> _recordDisposable;

    // How many times Unload has run, so that a load can tell whether one ran while it loaded.
    private int _unloads;

    public CCContentManager(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _loadedAssets = new Dictionary<string, AssetEntry>();
        _recordDisposable = RecordDisposable;
    }

    public CCContentManager(IServiceProvider serviceProvider, string rootDirectory) : base(serviceProvider, rootDirectory)
    {
        _loadedAssets = new Dictionary<string, AssetEntry>();
        _recordDisposable = RecordDisposable;
    }

    public T? TryLoad<T>(string assetName)
    {
        return TryLoad<T>(assetName, false);
    }

    public T? TryLoad<T>(string assetName, bool weakReference)
    {
        var assetKey = Tuple.Create(assetName, typeof(T));
        lock (_lock)
        {
            if (_failedAssets.ContainsKey(assetKey))
            {
                return default(T);
            }
        }

        try
        {
            return Load<T>(assetName, weakReference);
        }
        catch (Exception)
        {
            lock (_lock)
            {
                _failedAssets[assetKey] = null;
            }

            return default(T);
        }
    }

    public override T Load<T>(string assetName)
    {
        var assetKey = Tuple.Create(assetName, typeof(T));
        lock (_lock)
        {
            if (_failedAssets.ContainsKey(assetKey))
            {
                throw new ContentLoadException("Failed to load the asset file from " + assetName);
            }
        }

        try
        {
            return Load<T>(assetName, false);
        }
        catch (Exception)
        {
            lock (_lock)
            {
                _failedAssets[assetKey] = null;
            }

            throw;
        }
    }

    public T Load<T>(string assetName, bool weakReference)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            throw new ArgumentNullException("assetName");
        }

        // Check for a previously loaded asset first
        AssetEntry? entry;
        object? loaded = null;
        lock (_lock)
        {
            if (_loadedAssets.TryGetValue(assetName, out entry))
            {
                loaded = entry.Asset;
            }
        }
        if (entry != null)
        {
            if (loaded is T asset)
            {
                return asset;
            }
            else
            {
                return InternalLoad<T>(assetName, entry.AssetFileName, weakReference);
            }
        }

        var (realName, searchPaths, resolutionsOrder) = GetSearch(assetName);

        foreach (var searchPath in searchPaths)
        {
            foreach (string resolutionOrder in resolutionsOrder)
            {
                var path = Path.Combine(Path.Combine(searchPath, resolutionOrder), realName);

                try
                {
                    //TODO: for platforms with access to the file system, first check for the existence of the file 
                    return InternalLoad<T>(assetName, path, weakReference);
                }
                catch (ContentLoadException ex)
                {
                    // try other path
                    Console.WriteLine(ex.Message);
                }
            }
        }

        throw new ContentLoadException("Failed to load the asset file from " + assetName);
    }

    private string GetRealName(string assetName)
    {
        if (_assetLookupDict.ContainsKey(assetName))
        {
            return _assetLookupDict[assetName];
        }
        return assetName;
    }

    // An asset's real name and copies of the paths to look for it in, so that they can be tried
    // outside the lock.
    private (string RealName, string[] SearchPaths, string[] ResolutionsOrder) GetSearch(string assetName)
    {
        lock (_lock)
        {
            CheckDefaultPath(_searchPaths);
            CheckDefaultPath(_searchResolutionsOrder);

            return (GetRealName(assetName), _searchPaths.ToArray(), _searchResolutionsOrder.ToArray());
        }
    }

    // ReadAsset calls this for each disposable object it creates, on the thread that loads.
    private void RecordDisposable(IDisposable disposable)
    {
        lock (_lock)
        {
            if (!_disposables.Contains(disposable))
            {
                _disposables.Add(disposable);
            }
        }
    }

    public override void Unload()
    {
        IDisposable[] disposables;
        lock (_lock)
        {
            disposables = _disposables.ToArray();
            _disposables.Clear();
            _loadedAssets.Clear();
            _unloads++;
        }

        foreach (IDisposable disposable in disposables)
        {
            disposable.Dispose();
        }

        // Disposes what MonoGame recorded itself, as when the device reset reloaded assets.
        base.Unload();
    }

    private T InternalLoad<T>(string assetName, string path, bool weakReference)
    {
        int unloads;
        lock (_lock)
        {
            unloads = _unloads;
        }

        T result;

        bool useContentReader = true;

        try
        {
            //Special types
            if (typeof (T) == typeof (string))
            {
                //TODO: No need CCContent, use TileContainer
                var content = ReadAsset<CCContent>(path, _recordDisposable);
                result = (T) (object) (content.Content
                    ?? throw new ContentLoadException("The asset " + assetName + " has no content."));

                useContentReader = false;
            }
            else
            {
                // Load the asset.
                result = ReadAsset<T>(path, _recordDisposable);
            }
        }
        catch (ContentLoadException)
        {
            try
            {
                string assetPath = Path.Combine(RootDirectory, path);

                if (typeof(T) == typeof(string))
                {
                    using (var streamContent = TitleContainer.OpenStream(assetPath))
                    {
                        using (StreamReader reader = new StreamReader(streamContent, Encoding.UTF8))
                        {
                            result = (T)(object) reader.ReadToEnd();
                        }
                    }
                    useContentReader = false;
                }
                else if (typeof(T) == typeof(PlistDocument))
                {
                    try
                    {
                        using (var streamContent = TitleContainer.OpenStream(assetPath))
                        {
                            result = (T) (object) new PlistDocument(streamContent);
                        }
                    }
                    catch (Exception)
                    {
                        //TODO: Remove This
                        var content = ReadAsset<CCContent>(path, _recordDisposable);
                        result = (T) (object) new PlistDocument(content.Content
                            ?? throw new ContentLoadException("The asset " + assetName + " has no content."));
                    }

                    useContentReader = false;
                }
#if XNA
                else if (typeof (T) == typeof (Texture2D) && Path.HasExtension(path))
                {
                    var service =
                        (IGraphicsDeviceService?) ServiceProvider.GetService(typeof (IGraphicsDeviceService))
                        ?? throw new InvalidOperationException("Loading a texture needs a graphics device service.");

                    using (var streamContent = TitleContainer.OpenStream(assetPath))
                    {
                        result = (T) (object) Texture2D.FromStream(service.GraphicsDevice, streamContent);
                    }
                    useContentReader = false;
                }
#endif
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {

                throw new ContentLoadException("Failed to load the asset file from " + assetName);
            }
        }

        var assetEntry = new AssetEntry(result, path, weakReference, useContentReader);

        IDisposable? duplicate = null;
        lock (_lock)
        {
            if (_loadedAssets.TryGetValue(assetName, out AssetEntry? stored) && stored.Asset is T kept &&
                !ReferenceEquals(kept, result))
            {
                // Another thread stored the same asset while this one loaded it. The one stored
                // first is kept, so that every caller gets the one in the cache.
                duplicate = result as IDisposable;
                if (duplicate != null)
                {
                    _disposables.Remove(duplicate);
                }
                result = kept;
            }
            else if (unloads == _unloads)
            {
                _loadedAssets[assetName] = assetEntry;

                if (result is GraphicsResource resource)
                {
                    resource.Disposing += AssetDisposing;
                }
            }
            // Otherwise an Unload while this loaded disposed what the load had created by then, so
            // the asset isn't kept. The next load of it loads it again.
        }

        // Outside the lock, which is never held while an asset is disposed.
        duplicate?.Dispose();

        return result;
    }

    // Runs on whichever thread disposes the asset.
    private void AssetDisposing(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            foreach (var loadedAsset in _loadedAssets)
            {
                if (loadedAsset.Value.Asset == sender)
                {
                    _loadedAssets.Remove(loadedAsset.Key);
                    return;
                }
            }
        }
    }

#if MONOGAME
    protected override void ReloadGraphicsAssets()
    {
        lock (_lock)
        {
            foreach (var pair in _loadedAssets)
            {
                if (pair.Value.UseContentReader && pair.Value.Asset != null)
                {
                    LoadedAssets.Add(pair.Value.AssetFileName, pair.Value.Asset);
                }
            }
        }

        // Outside the lock, because it reloads the assets.
        base.ReloadGraphicsAssets();

        lock (_lock)
        {
            foreach (var pair in LoadedAssets)
            {
                foreach (var pair2 in _loadedAssets)
                {
                    if (pair2.Value.AssetFileName == pair.Key)
                    {
                        _loadedAssets[pair2.Key].Asset = pair.Value;
                    }
                }
            }
        }

        LoadedAssets.Clear();
    }
#else
    // CCDrawManager calls this when the device resets. MonoGame's own ReloadGraphicsAssets is
    // protected, so this public one hides it rather than overriding it.
    public new void ReloadGraphicsAssets()
    {
        lock (_lock)
        {
            foreach (var asset in _loadedAssets)
            {
                asset.Value.Asset = null;
            }
        }
    }
#endif

    public Stream GetAssetStream(string assetName)
    {
        var (realName, searchPaths, resolutionsOrder) = GetSearch(assetName);

        foreach (var searchPath in searchPaths)
        {
            foreach (string resolutionOrder in resolutionsOrder)
            {
                var path = Path.Combine(Path.Combine(RootDirectory, Path.Combine(searchPath, resolutionOrder)), realName);

                try
                {
                    //TODO: for platforms with access to the file system, first check for the existence of the file 
                    return TitleContainer.OpenStream(path);
                }
                catch (Exception)
                {
                    // try other path
                }
            }
        }

        throw new ContentLoadException("Failed to load the asset stream from " + assetName);
    }

    public virtual void LoadFilenameLookupDictionaryFromFile(string filename)
    {
        var document = Load<PlistDocument>(filename, true);
        
        SetFilenameLookupDictionary(document.GetRootDictionary(filename));
    }

    public virtual void SetFilenameLookupDictionary(PlistDictionary filenameLookupDict)
    {
        //TODO: Load lookup names from PlistDictionary
    }

    /// <summary>
    /// The resolution folders that loads look in, in order. Change the list before loading content
    /// on another thread, such as with <see cref="CCTextureCache.AddImageAsync"/>: loads read it
    /// under a lock that changing it doesn't take.
    /// </summary>
    public List<string> SearchResolutionsOrder
    {
        get { return _searchResolutionsOrder; }
    }

    /// <summary>
    /// The folders that loads look in, in order. Change the list before loading content on another
    /// thread, such as with <see cref="CCTextureCache.AddImageAsync"/>: loads read it under a lock
    /// that changing it doesn't take.
    /// </summary>
    public List<string> SearchPaths
    {
        get { return _searchPaths; }
    }

    // For the library's own changes to SearchPaths, which take the lock, unlike changes made
    // through the list. Returns whether it added the path.
    internal bool AddSearchPath(string path)
    {
        lock (_lock)
        {
            if (_searchPaths.Contains(path))
            {
                return false;
            }

            _searchPaths.Add(path);
            return true;
        }
    }

    internal void RemoveSearchPath(string path)
    {
        lock (_lock)
        {
            _searchPaths.Remove(path);
        }
    }

    private void CheckDefaultPath(List<string> paths)
    {
        for (int i = paths.Count - 1; i >= 0; i--)
        {
            if (paths[i] == "")
            {
                return;
            }
        }

        paths.Add("");
    }
}
