using System.IO;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// First headless coverage of CCUserDefault, enabled by the ICCUserDefaultStorage seam:
// an in-memory backend replaces the file/isolated-storage default, so the tests never
// touch the disk. Storage and the singleton are static - the suite runs serialized
// (see AssemblyInfo), and each test restores both in a finally.
public class CCUserDefaultTests
{
    /// <summary>In-memory ICCUserDefaultStorage: OpenWrite captures bytes on dispose.</summary>
    private sealed class MemoryUserDefaultStorage : ICCUserDefaultStorage
    {
        private byte[]? data;

        public bool Exists() { return data != null; }

        public Stream OpenRead() { return new MemoryStream(data ?? throw new FileNotFoundException(), writable: false); }

        public Stream OpenWrite() { return new CaptureStream(this); }

        private sealed class CaptureStream : MemoryStream
        {
            private readonly MemoryUserDefaultStorage owner;
            public CaptureStream(MemoryUserDefaultStorage owner) { this.owner = owner; }
            protected override void Dispose(bool disposing)
            {
                if (disposing) owner.data = ToArray();
                base.Dispose(disposing);
            }
        }
    }

    private static void WithMemoryStorage(System.Action body)
    {
        var previous = CCUserDefault.Storage;
        CCUserDefault.PurgeSharedUserDefault();
        CCUserDefault.Storage = new MemoryUserDefaultStorage();
        try
        {
            body();
        }
        finally
        {
            CCUserDefault.Storage = previous;
            CCUserDefault.PurgeSharedUserDefault();
        }
    }

    [Fact]
    public void FirstAccess_CreatesEmptyStore_DefaultsReturned()
    {
        WithMemoryStorage(() =>
        {
            var defaults = CCUserDefault.SharedUserDefault;
            Assert.False(defaults.GetBoolForKey("missing", false));
            Assert.Equal(42, defaults.GetIntegerForKey("missing", 42));
            Assert.Equal("fallback", defaults.GetStringForKey("missing", "fallback"));
        });
    }

    [Fact]
    public void Values_RoundTrip_Through_Flush_And_Reload()
    {
        WithMemoryStorage(() =>
        {
            var defaults = CCUserDefault.SharedUserDefault;
            defaults.SetBoolForKey("sound", true);
            defaults.SetIntegerForKey("highscore", 9001);
            defaults.SetDoubleForKey("volume", 0.75);
            defaults.SetStringForKey("player", "frutz");
            defaults.Flush();

            // Drop the singleton; the next access re-parses from the (memory) store.
            CCUserDefault.PurgeSharedUserDefault();
            var reloaded = CCUserDefault.SharedUserDefault;

            Assert.True(reloaded.GetBoolForKey("sound", false));
            Assert.Equal(9001, reloaded.GetIntegerForKey("highscore", 0));
            Assert.Equal(0.75, reloaded.GetDoubleForKey("volume", 0), 6);
            Assert.Equal("frutz", reloaded.GetStringForKey("player", null));
        });
    }
}
