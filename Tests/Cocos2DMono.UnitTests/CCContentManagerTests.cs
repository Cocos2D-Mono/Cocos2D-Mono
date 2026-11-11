using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
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
    [InlineData("AddSearchPath")]
    [InlineData("RemoveSearchPath")]
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
            // Internal, for CCParticleSystem, which adds its plist's folder while it loads.
            "AddSearchPath" => () => Invoke(content, "AddSearchPath", "c2d-373-folder"),
            "RemoveSearchPath" => () => Invoke(content, "RemoveSearchPath", "c2d-373-folder"),
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
        var content = new TestContentManager
        {
            WhileLoading = () =>
            {
                loading.Set();
                unloaded.Wait(WaitLimit);
            },
        };

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

    [Fact]
    public void Load_WhenTwoThreadsLoadTheSameAssetAtOnce_GivesBothTheOneItKeeps()
    {
        using var file = new TextAsset("c2d-373-same.txt");
        using var bothLoading = new Barrier(2);
        var content = new TestContentManager { WhileLoading = () => WaitForTheOtherLoad(bothLoading) };

        (string mine, string? theirs) = LoadOnTwoThreads(content, () => content.Load<string>(file.Name));

        Assert.Same(mine, theirs);
        Assert.Same(mine, content.Load<string>(file.Name));
    }

    [Fact]
    public void Load_WhenTwoThreadsLoadTheSameAssetAtOnce_DisposesTheOneItDoesntKeep()
    {
        DisposableAsset.Created.Clear();
        using var bothLoading = new Barrier(2);
        var content = new TestContentManager
        {
            Xnb = DisposableAssetXnb(),
            WhileLoading = () => WaitForTheOtherLoad(bothLoading),
        };

        (DisposableAsset mine, DisposableAsset? theirs) = LoadOnTwoThreads(content, () => content.Load<DisposableAsset>("c2d-373-disposable"));

        Assert.Same(mine, theirs);
        Assert.False(mine.IsDisposed);
        Assert.Equal(2, DisposableAsset.Created.Count);
        DisposableAsset dropped = Assert.Single(DisposableAsset.Created, asset => asset != mine);
        Assert.True(dropped.IsDisposed, "The asset that the content manager didn't keep wasn't disposed.");
    }

    [Fact]
    public void Load_WhenTwoThreadsLoadTheSameAssetAtOnce_DisposesEverythingTheOtherLoadCreated()
    {
        DisposableAsset.Created.Clear();
        using var bothLoading = new Barrier(2);
        var content = new TestContentManager
        {
            Xnb = Xnb(new[] { typeof(ContainerAssetReader), typeof(DisposableAssetReader) }, 1, 2),
            WhileLoading = () => WaitForTheOtherLoad(bothLoading),
        };

        (ContainerAsset mine, ContainerAsset? theirs) = LoadOnTwoThreads(content, () => content.Load<ContainerAsset>("c2d-373-container"));

        Assert.Same(mine, theirs);
        Assert.NotNull(mine.Inner);
        Assert.False(mine.Inner.IsDisposed);
        Assert.Equal(2, DisposableAsset.Created.Count);
        DisposableAsset dropped = Assert.Single(DisposableAsset.Created, asset => asset != mine.Inner);
        // The container isn't disposable, so only the record of what the load created finds this.
        Assert.True(dropped.IsDisposed, "What the other load created wasn't disposed.");
    }

    [Fact]
    public void Unload_DisposesWhatALoadCreated()
    {
        var content = new TestContentManager { Xnb = DisposableAssetXnb() };

        DisposableAsset asset = content.Load<DisposableAsset>("c2d-373-recorded");

        // Recorded by the content manager, under its lock, not in MonoGame's list, which has none.
        Assert.Contains(asset, Collection(content, "_disposables").Cast<object>());
        content.Unload();
        Assert.True(asset.IsDisposed);
    }

    private static CCContentManager NewContentManager() => new CCContentManager(new GameServiceContainer());

    // Holds each load until the other thread's load of the same asset has also missed the cache.
    private static void WaitForTheOtherLoad(Barrier bothLoading) =>
        Assert.True(bothLoading.SignalAndWait(WaitLimit), "The other thread didn't load the asset too.");

    // Runs the same load on another thread and on this one, and returns both results.
    private static (T Mine, T? Theirs) LoadOnTwoThreads<T>(CCContentManager content, Func<T> load) where T : class
    {
        T? theirs = null;
        Exception? error = null;
        var other = new Thread(() =>
        {
            try
            {
                theirs = load();
            }
            catch (Exception exception)
            {
                error = exception;
            }
        })
        { IsBackground = true };
        other.Start();

        T mine = load();

        Assert.True(other.Join(WaitLimit), "The other thread's load didn't finish.");
        if (error != null)
        {
            ExceptionDispatchInfo.Throw(error);
        }
        return (mine, theirs);
    }

    // An uncompressed XNB file for DesktopGL that holds one DisposableAsset.
    private static byte[] DisposableAssetXnb() => Xnb(new[] { typeof(DisposableAssetReader) }, 1);

    // An uncompressed XNB file for DesktopGL with the given type readers. Each object is the 1-based
    // index of the reader that reads it, in the order they're read: the asset first, then what its
    // reader reads.
    private static byte[] Xnb(Type[] readers, params int[] objects)
    {
        using var body = new MemoryStream();
        using (var writer = new BinaryWriter(body, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write7BitEncodedInt(readers.Length);
            foreach (Type reader in readers)
            {
                writer.Write(reader.FullName + ", " + reader.Assembly.GetName().Name);
                writer.Write(0);
            }
            writer.Write7BitEncodedInt(0);
            foreach (int reader in objects)
            {
                writer.Write7BitEncodedInt(reader);
            }
        }

        using var xnb = new MemoryStream();
        using (var writer = new BinaryWriter(xnb))
        {
            writer.Write("XNBd"u8);
            writer.Write((byte)5);
            writer.Write((byte)0);
            writer.Write(10 + (int)body.Length);
            writer.Write(body.ToArray());
        }
        return xnb.ToArray();
    }

    // Runs an action inside each load's OpenStream, which ReadAsset calls first, so that a test can
    // act while a load runs. With no XNB, the load then looks for the file itself, as for a text file.
    private sealed class TestContentManager : CCContentManager
    {
        public TestContentManager() : base(new GameServiceContainer())
        {
        }

        public Action WhileLoading { get; init; } = () => { };

        public byte[]? Xnb { get; init; }

        protected override Stream OpenStream(string assetName)
        {
            WhileLoading();
            return Xnb != null ? new MemoryStream(Xnb) : base.OpenStream(assetName);
        }
    }

    // An asset that's disposable but needs no graphics device. The tests in a class run one at a
    // time, so they can share the list of the ones the reader created.
    public sealed class DisposableAsset : IDisposable
    {
        public static readonly List<DisposableAsset> Created = new List<DisposableAsset>();

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    // An asset that isn't disposable but holds one that is, as a SpriteFont holds its texture.
    public sealed class ContainerAsset
    {
        public DisposableAsset? Inner { get; init; }
    }

    public sealed class ContainerAssetReader : ContentTypeReader<ContainerAsset>
    {
        protected override ContainerAsset Read(ContentReader input, ContainerAsset existingInstance) =>
            new ContainerAsset { Inner = input.ReadObject<DisposableAsset>() };
    }

    // MonoGame finds this by the name in the XNB, and creates it through its public constructor.
    public sealed class DisposableAssetReader : ContentTypeReader<DisposableAsset>
    {
        protected override DisposableAsset Read(ContentReader input, DisposableAsset existingInstance)
        {
            var asset = new DisposableAsset();
            lock (DisposableAsset.Created)
            {
                DisposableAsset.Created.Add(asset);
            }
            return asset;
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
