using System.Reflection;
using System.Runtime.CompilerServices;
using Cocos2D;
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

    public void Dispose()
    {
        // The cache is shared, so take out what the test put in.
        CCTextureCache cache = CCTextureCache.SharedTextureCache;
        foreach (string key in UnusedKeys)
        {
            cache.RemoveTextureForKey(key);
        }
        cache.RemoveTextureForKey(InUseKey);
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
}
