using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cocos2D;

/// <summary>
/// Shared by the caches that drop what nothing else uses:
/// <see cref="CCTextureCache.RemoveUnusedTextures"/>,
/// <see cref="CCSpriteFrameCache.RemoveUnusedSpriteFrames"/> and
/// <see cref="Cocos2D.sprite_nodes.CCSpriteSheetCache.RemoveUnused"/>.
/// </summary>
internal static class CCCachePurge
{
    /// <summary>
    /// Empties <paramref name="cache"/> and returns a weak reference to each value it held, under
    /// the same key. The caller then collects garbage and puts back the values that survived.
    /// </summary>
    /// <remarks>
    /// This must stay a separate method that isn't inlined. A loop over the cache in the method
    /// that then calls <see cref="GC.Collect()"/> leaves its enumerator on that method's stack
    /// frame, still holding the last entry it visited, so that entry always survived.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Dictionary<string, WeakReference> TakeAsWeakReferences<T>(Dictionary<string, T> cache)
        where T : class
    {
        var weak = new Dictionary<string, WeakReference>(cache.Count);
        foreach (KeyValuePair<string, T> pair in cache)
        {
            weak.Add(pair.Key, new WeakReference(pair.Value));
        }
        cache.Clear();
        return weak;
    }
}
