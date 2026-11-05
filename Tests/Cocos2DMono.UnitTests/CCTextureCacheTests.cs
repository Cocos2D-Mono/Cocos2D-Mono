using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Cocos2D;
using Microsoft.Xna.Framework;
using Xunit;

namespace Cocos2DMono.UnitTests;

// RemoveUnusedTextures kept the last texture its loop visited, whether anything used it or not: the
// loop's enumerator was still on the stack when it collected (C2D-358). The textures are created
// without loading an image, which needs no graphics device. Adding one to the cache does need one,
// so the tests put them straight into its dictionary.
public class CCTextureCacheTests : IDisposable
{
    private static readonly string[] UnusedKeys = { "c2d-358-a", "c2d-358-b", "c2d-358-c" };
    private const string InUseKey = "c2d-358-in-use";
    private const string AsyncKey = "c2d-363-async";
    private const string LockKey = "c2d-363-lock";

    private static readonly GameTime OneFrame = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));

    [Fact]
    public void RemoveUnusedTextures_RemovesEveryTextureNothingUses()
    {
        CCTextureCache cache = CCTextureCache.SharedTextureCache;
        AddUnusedTextures(cache);

        cache.RemoveUnusedTextures();

        Assert.All(UnusedKeys, key => Assert.False(cache.Contains(key)));
    }

    [Fact]
    public void RemoveUnusedTextures_KeepsATextureStillInUse()
    {
        CCTextureCache cache = CCTextureCache.SharedTextureCache;
        var inUse = new CCTexture2D();
        Textures(cache).Add(InUseKey, inUse);
        AddUnusedTextures(cache);

        cache.RemoveUnusedTextures();

        Assert.Same(inUse, cache.TextureForKey(InUseKey));
    }

    // The tests below come from C2D-363. AddImageAsync's worker scheduled each load's callback from
    // its own thread, but the scheduler isn't thread-safe: the game thread's tick walks its timers
    // without a lock. And only AddImage took the cache's lock, so the cache's other methods raced
    // the worker's writes to the dictionary. The loads below find their texture in the cache, so
    // the worker loads no image.

    [Fact]
    public void AddImageAsync_LeavesTheCallbackToTheGameThreadsNextTick()
    {
        CCTextureCache cache = CCTextureCache.SharedTextureCache;
        CCDirector director = CCDirector.SharedDirector;
        var texture = new CCTexture2D();
        Textures(cache).Add(AsyncKey, texture);
        int gameThread = Environment.CurrentManagedThreadId;
        var calls = new List<(CCTexture2D? Texture, int Thread)>();

        cache.AddImageAsync(AsyncKey + ".png", loaded => calls.Add((loaded, Environment.CurrentManagedThreadId)));
        WaitForTheWorker(cache);

        Assert.False(HasTimers(director.Scheduler, cache), "The worker scheduled the callback from its own thread.");
        Assert.Empty(calls);

        director.Update(OneFrame);

        (CCTexture2D? Texture, int Thread) call = Assert.Single(calls);
        Assert.Same(texture, call.Texture);
        Assert.Equal(gameThread, call.Thread);
    }

    [Fact]
    public void AddImageAsync_WhenAHandlerRethrowsWhatACallbackThrew_RunsTheNextCallbackOnTheNextTick()
    {
        CCTextureCache cache = CCTextureCache.SharedTextureCache;
        CCDirector director = CCDirector.SharedDirector;
        Textures(cache).Add(AsyncKey, new CCTexture2D());
        int secondCalls = 0;
        Action<Exception> rethrow = error => ExceptionDispatchInfo.Throw(error);
        director.Scheduler.OnUnhandledException += rethrow;
        try
        {
            cache.AddImageAsync(AsyncKey + ".png", _ => throw new InvalidOperationException("Thrown by the test."));
            cache.AddImageAsync(AsyncKey + ".png", _ => secondCalls++);
            WaitForTheWorker(cache);

            Assert.Throws<InvalidOperationException>(() => director.Update(OneFrame));
            Assert.Equal(0, secondCalls);

            director.Update(OneFrame);
            Assert.Equal(1, secondCalls);
        }
        finally
        {
            director.Scheduler.OnUnhandledException -= rethrow;
        }
    }

    [Theory]
    [InlineData(nameof(CCTextureCache.Contains))]
    [InlineData(nameof(CCTextureCache.TextureForKey))]
    [InlineData(nameof(CCTextureCache.RemoveTexture))]
    [InlineData(nameof(CCTextureCache.RemoveTextureForKey))]
    [InlineData(nameof(CCTextureCache.RemoveAllTextures))]
    [InlineData(nameof(CCTextureCache.RemoveUnusedTextures))]
    [InlineData(nameof(CCTextureCache.UnloadContent))]
    [InlineData(nameof(CCTextureCache.DumpCachedTextureInfo))]
    [InlineData(nameof(CCTextureCache.Dispose))]
    public void UsingTheDictionary_WaitsForTheCachesLock(string method)
    {
        // A cache of the test's own, so the shared one is left alone.
        CCTextureCache cache = NewCache();
        var texture = new CCTexture2D();
        Textures(cache).Add(LockKey, texture);
        Action call = method switch
        {
            nameof(CCTextureCache.Contains) => () => cache.Contains(LockKey),
            nameof(CCTextureCache.TextureForKey) => () => cache.TextureForKey(LockKey),
            nameof(CCTextureCache.RemoveTexture) => () => cache.RemoveTexture(texture),
            nameof(CCTextureCache.RemoveTextureForKey) => () => cache.RemoveTextureForKey(LockKey),
            nameof(CCTextureCache.RemoveAllTextures) => cache.RemoveAllTextures,
            nameof(CCTextureCache.RemoveUnusedTextures) => cache.RemoveUnusedTextures,
            nameof(CCTextureCache.UnloadContent) => cache.UnloadContent,
            nameof(CCTextureCache.DumpCachedTextureInfo) => cache.DumpCachedTextureInfo,
            nameof(CCTextureCache.Dispose) => cache.Dispose,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        };

        using var started = new ManualResetEventSlim();
        Exception? error = null;
        var other = new Thread(() =>
        {
            started.Set();
            try
            {
                call();
            }
            catch (Exception exception)
            {
                error = exception;
            }
        })
        { IsBackground = true };

        lock (CacheLock(cache))
        {
            other.Start();
            started.Wait();

            // The call can't finish while this thread holds the lock, unless it doesn't take it.
            Assert.False(other.Join(TimeSpan.FromMilliseconds(100)), $"{method} didn't wait for the cache's lock.");
        }
        Assert.True(other.Join(TimeSpan.FromSeconds(10)), $"{method} didn't finish once the lock was free.");
        if (error != null)
        {
            ExceptionDispatchInfo.Throw(error);
        }
    }

    public void Dispose()
    {
        // The cache is shared, so take out what the test put in.
        CCTextureCache cache = CCTextureCache.SharedTextureCache;
        foreach (string key in UnusedKeys)
        {
            cache.RemoveTextureForKey(key);
        }
        cache.RemoveTextureForKey(InUseKey);
        cache.RemoveTextureForKey(AsyncKey);
    }

    // Not inlined, so no local in the test refers to the textures.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddUnusedTextures(CCTextureCache cache)
    {
        foreach (string key in UnusedKeys)
        {
            Textures(cache).Add(key, new CCTexture2D());
        }
    }

    // The cache's dictionary. Its constructor is private, and every public way to add a texture
    // loads an image.
    private static Dictionary<string, CCTexture2D> Textures(CCTextureCache cache)
    {
        FieldInfo? field = typeof(CCTextureCache).GetField("m_pTextures", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<Dictionary<string, CCTexture2D>>(field.GetValue(cache));
    }

    // A cache of the test's own, through the private constructor.
    private static CCTextureCache NewCache() =>
        Assert.IsType<CCTextureCache>(Activator.CreateInstance(typeof(CCTextureCache), nonPublic: true));

    // The lock that guards the cache's dictionary.
    private static object CacheLock(CCTextureCache cache)
    {
        FieldInfo? field = typeof(CCTextureCache).GetField("_dictLock", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        object? value = field.GetValue(cache);
        Assert.NotNull(value);
        return value;
    }

    // The worker clears _task once nothing is left to load, after it has passed on every callback.
    private static void WaitForTheWorker(CCTextureCache cache)
    {
        FieldInfo? task = typeof(CCTextureCache).GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(task);
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (task.GetValue(cache) != null)
        {
            Assert.True(DateTime.UtcNow < deadline, "AddImageAsync's worker didn't finish.");
            Thread.Sleep(5);
        }
    }

    // Whether the scheduler has timers for the target. Its table of them is private.
    private static bool HasTimers(CCScheduler scheduler, ICCSelectorProtocol target)
    {
        FieldInfo? field = typeof(CCScheduler).GetField("_hashForTimers", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<IDictionary>(field.GetValue(scheduler)).Contains(target);
    }
}
