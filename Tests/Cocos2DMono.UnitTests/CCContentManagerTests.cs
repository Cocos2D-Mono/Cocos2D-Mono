using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cocos2D;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Xunit;

namespace Cocos2DMono.UnitTests;

// CCContentManager kept its state in dictionaries and lists with no lock, but AddImageAsync's worker
// loads textures through it while the game thread loads everything else (C2D-373). Two loads at once
// could corrupt a dictionary for good, and every load after that threw. The tests load assets that
// don't exist, which needs no graphics device.
public class CCContentManagerTests
{
    private const string Missing = "c2d-373-missing";
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    [Fact]
    public void TryLoad_FromTwoThreadsAtOnce_RecordsEveryLoadThatFailed()
    {
        CCContentManager content = NewContentManager();
        const int perThread = 2000;
        using var start = new ManualResetEventSlim();
        Exception? errorA = null;
        Exception? errorB = null;
        var a = new Thread(() => errorA = LoadMissing(content, "c2d-373-a-", perThread, start)) { IsBackground = true };
        var b = new Thread(() => errorB = LoadMissing(content, "c2d-373-b-", perThread, start)) { IsBackground = true };
        a.Start();
        b.Start();
        start.Set();

        Assert.True(a.Join(WaitLimit) && b.Join(WaitLimit), "The loads didn't finish.");
        Assert.Null(errorA);
        Assert.Null(errorB);
        Assert.Equal(2 * perThread, Collection(content, "_failedAssets").Count);
    }

    [Theory]
    [InlineData(nameof(CCContentManager.TryLoad))]
    [InlineData(nameof(CCContentManager.Load))]
    [InlineData(nameof(CCContentManager.GetAssetStream))]
    [InlineData(nameof(CCContentManager.Unload))]
    [InlineData("ReloadGraphicsAssets")]
    [InlineData("AssetDisposing")]
    public void UsingItsCollections_WaitsForTheLock(string method)
    {
        CCContentManager content = NewContentManager();
        Action call = method switch
        {
            nameof(CCContentManager.TryLoad) => () => content.TryLoad<Texture2D>(Missing),
            nameof(CCContentManager.Load) => () => Assert.Throws<ContentLoadException>(() => content.Load<Texture2D>(Missing)),
            nameof(CCContentManager.GetAssetStream) => () => Assert.Throws<ContentLoadException>(() => content.GetAssetStream(Missing)),
            nameof(CCContentManager.Unload) => content.Unload,
            // Protected, and private: the device reset calls the first, and disposing an asset the second.
            "ReloadGraphicsAssets" => () => Invoke(content, "ReloadGraphicsAssets"),
            "AssetDisposing" => () => Invoke(content, "AssetDisposing", new object(), EventArgs.Empty),
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

        lock (Lock(content))
        {
            other.Start();
            started.Wait();

            // The call can't finish while this thread holds the lock, unless it doesn't take it.
            Assert.False(other.Join(TimeSpan.FromMilliseconds(100)), $"{method} didn't wait for the content manager's lock.");
        }
        Assert.True(other.Join(WaitLimit), $"{method} didn't finish once the lock was free.");
        if (error != null)
        {
            ExceptionDispatchInfo.Throw(error);
        }
    }

    [Fact]
    public void Load_WhenUnloadRunsDuringTheLoad_DoesntKeepTheAsset()
    {
        using var file = new TextAsset("c2d-373-unload.txt");
        using var loading = new ManualResetEventSlim();
        using var unloaded = new ManualResetEventSlim();
        var content = new PausingContentManager(() =>
        {
            loading.Set();
            unloaded.Wait(WaitLimit);
        });

        string? text = null;
        Exception? error = null;
        var other = new Thread(() =>
        {
            try
            {
                text = content.Load<string>(file.Name);
            }
            catch (Exception exception)
            {
                error = exception;
            }
        })
        { IsBackground = true };
        other.Start();
        Assert.True(loading.Wait(WaitLimit), "The load didn't start.");

        content.Unload();
        unloaded.Set();

        Assert.True(other.Join(WaitLimit), "The load didn't finish.");
        if (error != null)
        {
            ExceptionDispatchInfo.Throw(error);
        }
        Assert.Equal(file.Text, text);
        // Unload disposed what the load had recorded by then, so the asset mustn't stay cached.
        Assert.Empty(Collection(content, "_loadedAssets"));
    }

    private static CCContentManager NewContentManager() => new CCContentManager(new GameServiceContainer());

    // Runs an action inside each load's OpenStream, which ReadAsset calls first, so that a test can
    // act while a load runs. The tests' assets have no XNB, so the load then reads the file itself.
    private sealed class PausingContentManager : CCContentManager
    {
        private readonly Action _whileLoading;

        public PausingContentManager(Action whileLoading) : base(new GameServiceContainer())
        {
            _whileLoading = whileLoading;
        }

        protected override Stream OpenStream(string assetName)
        {
            _whileLoading();
            return base.OpenStream(assetName);
        }
    }

    // A text file next to the tests, where TitleContainer looks, deleted afterwards.
    private sealed class TextAsset : IDisposable
    {
        public TextAsset(string name)
        {
            Name = name;
            Text = "Written by " + name;
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, name), Text);
        }

        public string Name { get; }

        public string Text { get; }

        public void Dispose() => File.Delete(Path.Combine(AppContext.BaseDirectory, Name));
    }

    // Tries to load assets that don't exist, each of which the content manager records as failed.
    private static Exception? LoadMissing(CCContentManager content, string prefix, int count, ManualResetEventSlim start)
    {
        start.Wait();
        try
        {
            for (int i = 0; i < count; i++)
            {
                Assert.Null(content.TryLoad<Texture2D>(prefix + i));
            }
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    // One of the content manager's private collections.
    private static ICollection Collection(CCContentManager content, string name)
    {
        FieldInfo? field = typeof(CCContentManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<ICollection>(field.GetValue(content));
    }

    // The lock that guards the content manager's collections.
    private static object Lock(CCContentManager content)
    {
        FieldInfo? field = typeof(CCContentManager).GetField("_lock", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        object? value = field.GetValue(content);
        Assert.NotNull(value);
        return value;
    }

    private static void Invoke(CCContentManager content, string name, params object[] arguments)
    {
        MethodInfo? method = typeof(CCContentManager).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        try
        {
            method.Invoke(content, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Throw(exception.InnerException);
        }
    }
}
