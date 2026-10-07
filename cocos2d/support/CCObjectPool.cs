#nullable disable
using System;
using System.Collections.Generic;

namespace Cocos2D;

/// <summary>
/// Interface for objects that can be managed by CCObjectPool.
/// Implement this on CCNode subclasses to define activate/reset behavior.
/// </summary>
public interface ICCPoolable
{
    /// <summary>
    /// Called when the object is retrieved from the pool.
    /// Use this to initialize or re-initialize the object for reuse.
    /// </summary>
    void OnActivate();

    /// <summary>
    /// Called when the object is returned to the pool.
    /// Use this to clean up state and prepare for future reuse.
    /// </summary>
    void OnReset();
}

/// <summary>
/// Generic object pool for CCNode subclasses. Avoids garbage collection pressure
/// by reusing node instances instead of creating and destroying them.
///
/// Usage:
///   var pool = new CCObjectPool&lt;Bullet&gt;(parentNode, initialSize: 20);
///   var bullet = pool.Get();  // retrieves from pool or creates new
///   pool.Return(bullet);      // returns to pool (hides, does not remove from scene)
/// </summary>
public class CCObjectPool<T> where T : CCNode, new()
{
    private readonly List<T> _pool;
    private readonly List<T> _active;
    private readonly CCNode _parent;

    /// <summary>
    /// Creates a new object pool.
    /// </summary>
    /// <param name="parent">Optional parent node. When set, new objects are automatically added as children.</param>
    /// <param name="initialSize">Number of objects to pre-allocate.</param>
    public CCObjectPool(CCNode parent = null, int initialSize = 0)
    {
        _pool = new List<T>(Math.Max(initialSize, 8));
        _active = new List<T>(Math.Max(initialSize, 8));
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateInstance();
            obj.Visible = false;
            _pool.Add(obj);
        }
    }

    /// <summary>
    /// Number of currently active (in-use) objects.
    /// </summary>
    public int ActiveCount => _active.Count;

    /// <summary>
    /// Number of objects available in the pool.
    /// </summary>
    public int AvailableCount => _pool.Count;

    /// <summary>
    /// Retrieves an object from the pool, or creates a new one if the pool is empty.
    /// The object is made visible and OnActivate is called if it implements ICCPoolable.
    /// </summary>
    public T Get()
    {
        T obj;
        if (_pool.Count > 0)
        {
            int lastIndex = _pool.Count - 1;
            obj = _pool[lastIndex];
            _pool.RemoveAt(lastIndex);
        }
        else
        {
            obj = CreateInstance();
        }

        obj.Visible = true;
        _active.Add(obj);

        if (obj is ICCPoolable poolable)
        {
            poolable.OnActivate();
        }

        return obj;
    }

    /// <summary>
    /// Returns an object to the pool. The object is hidden (not removed from scene graph)
    /// and OnReset is called if it implements ICCPoolable.
    /// </summary>
    public void Return(T obj)
    {
        if (obj is ICCPoolable poolable)
        {
            poolable.OnReset();
        }

        obj.Visible = false;
        _active.Remove(obj);
        _pool.Add(obj);
    }

    /// <summary>
    /// Returns all active objects to the pool.
    /// </summary>
    public void ReturnAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var obj = _active[i];

            if (obj is ICCPoolable poolable)
            {
                poolable.OnReset();
            }

            obj.Visible = false;
            _pool.Add(obj);
        }
        _active.Clear();
    }

    /// <summary>
    /// Executes an action on each active object. Safe to call Return() inside the action.
    /// </summary>
    public void ForEachActive(Action<T> action)
    {
        // Iterate backwards to allow safe removal during iteration
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (i < _active.Count)
            {
                action(_active[i]);
            }
        }
    }

    private T CreateInstance()
    {
        var obj = new T();
        if (_parent != null)
        {
            _parent.AddChild(obj);
        }
        return obj;
    }
}
