#nullable disable
using System;
#if !NETFRAMEWORK
using System.Buffers;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cocos2D;

///<summary>
/// No-frills list that wraps an accessible array.
///</summary>
///<typeparam name="T">Type of elements contained by the list.</typeparam>
public class CCRawList<T> : IList<T>
{
    ///<summary>
    /// Direct access to the elements owned by the raw list.
    /// Be careful about the operations performed on this list;
    /// use the normal access methods if in doubt.
    ///</summary>
    public T[] Elements;

    public int count;

    public bool UseArrayPool;

    // When pooling is enabled, buffers are rented from the shared System.Buffers pool.
    // They must be cleared on return when T holds references, so a returned buffer does
    // not keep objects alive until it is rented again (matches List<T>'s own behavior).
    // NETFRAMEWORK (the PS5 fork) never pools — see RentBuffer/ReturnBuffer below — so this
    // field and the whole System.Buffers dependency are compiled out of that build.
#if !NETFRAMEWORK
    private static readonly bool ClearOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#endif

    // Tracks whether the current Elements buffer was actually rented from
    // ArrayPool<T>.Shared, independent of the public, mutable UseArrayPool flag. Returns
    // are keyed off this (not UseArrayPool), so toggling UseArrayPool after construction
    // can never leak a rented buffer or hand a non-rented one back to the shared pool.
    private bool _ownsPooledBuffer;

    // The PS5 fork transpiles to native C++ via BRUTE, which cannot generate
    // System.Buffers.ArrayPool<T> (its DefaultArrayPool<T> internals fail codegen, which in
    // turn breaks CCRawList<T> and every renderer type that includes it). Route all buffer
    // alloc/free through these helpers so the NETFRAMEWORK build never references ArrayPool<T>:
    // it always allocates plain arrays. Every other target keeps the pooled fast path.
    // UseArrayPool is still honored as a flag; on NETFRAMEWORK it simply maps to non-pooled
    // backing (ReturnBuffer is a no-op there), which is always safe — count stays the
    // authoritative logical length regardless of the backing array's exact size.
    private static T[] RentBuffer(int minimumLength)
    {
#if NETFRAMEWORK
        return new T[minimumLength];
#else
        return ArrayPool<T>.Shared.Rent(minimumLength);
#endif
    }

    private static void ReturnBuffer(T[] buffer)
    {
#if !NETFRAMEWORK
        ArrayPool<T>.Shared.Return(buffer, ClearOnReturn);
#endif
    }

    ///<summary>
    /// Constructs an empty list.
    ///</summary>
    public CCRawList(bool useArrayPool = false)
    {
        UseArrayPool = useArrayPool;

        if (useArrayPool)
        {
            Elements = RentBuffer(4);
            _ownsPooledBuffer = true;
        }
        else
        {
            Elements = new T[4];
        }

        Debug.Assert(Elements != null);
    }

    ///<summary>
    /// Constructs an empty list.
    ///</summary>
    ///<param name="initialCapacity">Initial capacity to allocate for the list.</param>
    ///<exception cref="ArgumentException">Thrown when the initial capacity is zero or negative.</exception>
    public CCRawList(int initialCapacity, bool useArrayPool = false)
    {
        UseArrayPool = useArrayPool;

        if (initialCapacity <= 0)
            throw new ArgumentException("Initial capacity must be positive.");

        Capacity = initialCapacity;
    }

    ///<summary>
    /// Constructs a raw list from another list.
    ///</summary>
    ///<param name="elements">List to copy.</param>
    public CCRawList(IList<T> elements, bool useArrayPool = false)
        : this(Math.Max(elements.Count, 4), useArrayPool)
    {
        elements.CopyTo(Elements, 0);
        count = elements.Count;
    }

    ///<summary>
    /// Gets or sets the current size allocated for the list.
    ///</summary>
    public int Capacity
    {
        get { return Elements.Length; }
        set
        {
            T[] newArray;
            bool newArrayIsPooled = UseArrayPool;

            if (newArrayIsPooled)
            {
                // Rent rounds the request up to a pooled bucket size; count (not
                // Elements.Length) remains the authoritative logical length.
                newArray = RentBuffer(value);
            }
            else
            {
                newArray = new T[value];
            }

            if (Elements != null && count > 0)
            {
                Array.Copy(Elements, newArray, count);
            }

            // Return the OLD buffer based on its real provenance, not the current flag.
            if (_ownsPooledBuffer && Elements != null)
            {
                ReturnBuffer(Elements);
            }

            Elements = newArray;
            _ownsPooledBuffer = newArrayIsPooled;

            Debug.Assert(Elements != null);
        }
    }

    #region IList<T> Members

    /// <summary>
    /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
    /// </summary>
    /// <returns>
    /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
    /// </returns>
    public int Count
    {
        get { return count; }
        set
        {
            if (Elements.Length < value)
            {
                Capacity = value;
            }
            count = value;
        }
    }

    /// <summary>
    /// Removes an element from the list.
    /// </summary>
    /// <param name="index">Index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        if (index >= count)
        {
            throw new ArgumentOutOfRangeException("index");
        }
        count--;
        if (index < count)
        {
            Array.Copy(Elements, index + 1, Elements, index, count - index);
        }

        Array.Clear(Elements, count, 1);
    }

    public void RemoveAt(int index, int amount)
    {
        if (index + amount > count)
        {
            throw new ArgumentOutOfRangeException("index");
        }
        count -= amount;
        if (index < count)
        {
            Array.Copy(Elements, index + amount, Elements, index, count - index);
        }

        // Clear the vacated tail in one call (was a per-element Array.Clear loop).
        Array.Clear(Elements, count, amount);
    }

    /// <summary>
    /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
    public void Add(T item)
    {
        if (count == Elements.Length)
        {
            Capacity = Elements.Length * 2;
        }
        Elements[count++] = item;
    }

    public void Add(ref T item)
    {
        if (count == Elements.Length)
        {
            Capacity = Elements.Length * 2;
        }
        Elements[count++] = item;
    }

    /// <summary>
    /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
    /// </summary>
    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
    public void Clear()
    {
        //Array.Clear(Elements, 0, count);
        count = 0;
    }

    public void Clear(bool fullClear)
    {
        count = 0;
        if (fullClear)
        {
            Free();
        }
    }

    public void Free()
    {
        if (Elements != null && _ownsPooledBuffer)
        {
            ReturnBuffer(Elements);
            Elements = null;
            _ownsPooledBuffer = false;
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
    /// </summary>
    /// <returns>
    /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
    /// </returns>
    /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index == -1)
            return false;
        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
    /// </returns>
    /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
    public int IndexOf(T item)
    {
        return Array.IndexOf(Elements, item, 0, count);
    }

    /// <summary>
    /// Inserts the element at the specified index.
    /// </summary>
    /// <param name="index">Index to insert the item.</param>
    /// <param name="item">Element to insert.</param>
    public void Insert(int index, T item)
    {
        if (index < count)
        {
            if (count == Elements.Length)
            {
                Capacity = Elements.Length * 2;
            }

            Array.Copy(Elements, index, Elements, index + 1, count - index);
            Elements[index] = item;
            count++;
        }
        else
            Add(item);
    }

    /// <summary>
    /// Gets or sets the element of the list at the given index.
    /// </summary>
    /// <param name="index">Index in the list.</param>
    /// <returns>Element at the given index.</returns>
    public T this[int index]
    {
        get
        {
            //if (index < count && index >= 0)
                return Elements[index];
            //throw new IndexOutOfRangeException("Index is outside of the list's bounds.");
        }
        set
        {
            //if (index < count && index >= 0)
                Elements[index] = value;
            //else
            //    throw new IndexOutOfRangeException("Index is outside of the list's bounds.");
        }
    }

    /// <summary>
    /// Determines if an item is present in the list.
    /// </summary>
    /// <param name="item">Item to be tested.</param>
    /// <returns>Whether or not the item was contained by the list.</returns>
    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    /// <summary>
    /// Copies the list's contents to the array.
    /// </summary>
    /// <param name="array">Array to receive the list's contents.</param>
    /// <param name="arrayIndex">Index in the array to start the dump.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(Elements, 0, array, arrayIndex, count);
    }

    bool ICollection<T>.IsReadOnly
    {
        get { return false; }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion

    /// <summary>
    /// Removes an element from the list without maintaining order.
    /// </summary>
    /// <param name="index">Index of the element to remove.</param>
    public void FastRemoveAt(int index)
    {
        if (index >= count)
        {
            throw new ArgumentOutOfRangeException("index");
        }
        count--;
        if (index < count)
        {
            Elements[index] = Elements[count];
        }
        Array.Clear(Elements, count, 1);
    }

    ///<summary>
    /// Adds a range of elements to the list from another list.
    ///</summary>
    ///<param name="items">Elements to add.</param>
    public void AddRange(CCRawList<T> items)
    {
        int neededLength = count + items.count;
        if (neededLength > Elements.Length)
        {
            int newLength = Elements.Length * 2;
            if (newLength < neededLength)
                newLength = neededLength;
            Capacity = newLength;
        }
        Array.Copy(items.Elements, 0, Elements, count, items.count);
        count = neededLength;
    }

    public void AddRange(CCRawList<T> items, int offset, int c)
    {
        int neededLength = count + c;
        if (neededLength > Elements.Length)
        {
            int newLength = Elements.Length * 2;
            if (newLength < neededLength)
                newLength = neededLength;
            Capacity = newLength;
        }
        Array.Copy(items.Elements, offset, Elements, count, c);
        count = neededLength;
    }

    ///<summary>
    /// Adds a range of elements to the list from another list.
    ///</summary>
    ///<param name="items">Elements to add.</param>
    public void AddRange(List<T> items)
    {
        int neededLength = count + items.Count;
        if (neededLength > Elements.Length)
        {
            int newLength = Elements.Length * 2;
            if (newLength < neededLength)
                newLength = neededLength;
            Capacity = newLength;
        }
        items.CopyTo(0, Elements, count, items.Count);
        count = neededLength;
    }

    ///<summary>
    /// Adds a range of elements to the list from another list.
    ///</summary>
    ///<param name="items">Elements to add.</param>
    public void AddRange(IList<T> items)
    {
        int neededLength = count + items.Count;
        if (neededLength > Elements.Length)
        {
            int newLength = Elements.Length * 2;
            if (newLength < neededLength)
                newLength = neededLength;
            Capacity = newLength;
        }
        items.CopyTo(Elements, 0);
        count = neededLength;
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the collection without maintaining element order.
    /// </summary>
    /// <returns>
    /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
    /// </returns>
    /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
    public bool FastRemove(T item)
    {
        int index = IndexOf(item);
        if (index == -1)
            return false;
        FastRemoveAt(index);
        return true;
    }

    /// <summary>
    /// Copies the elements from the list into an array.
    /// </summary>
    /// <returns>An array containing the elements in the list.</returns>
    public T[] ToArray()
    {
        var toReturn = new T[count];
        Array.Copy(Elements, toReturn, count);
        return toReturn;
    }

    /// <summary>
    /// Inserts the element at the specified index without maintaining list order.
    /// </summary>
    /// <param name="index">Index to insert the item.</param>
    /// <param name="item">Element to insert.</param>
    public void FastInsert(int index, T item)
    {
        if (index < count)
        {
            if (count == Elements.Length)
            {
                Capacity = Elements.Length * 2;
            }

            //Array.Copy(Elements, index, Elements, index + 1, count - index);
            Elements[count] = Elements[index];
            Elements[index] = item;
            count++;
        }
        else
            Add(item);
    }

    ///<summary>
    /// Gets an enumerator for the list.
    ///</summary>
    ///<returns>Enumerator for the list.</returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    ///<summary>
    /// Sorts the list.
    ///</summary>
    ///<param name="comparer">Comparer to use to sort the list.</param>
    public void Sort(IComparer<T> comparer)
    {
        Array.Sort(Elements, 0, count, comparer);
    }

    public void Reverse()
    {
        Array.Reverse(Elements, 0, count);
    }

    public T First()
    {
        return Elements[0];
    }

    public T Last()
    {
        return Elements[count - 1];
    }

    public T Pop()
    {
        return Elements[--count];
    }

    public T Peek()
    {
        return Elements[count - 1];
    }

    public void Push(T item)
    {
        if (count == Elements.Length)
        {
            Capacity = Elements.Length * 2;
        }
        Elements[count++] = item;
    }

    #region Nested type: Enumerator

    ///<summary>
    /// Enumerator for the RawList.
    ///</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly CCRawList<T> _list;
        private int _index;

        ///<summary>
        /// Constructs a new enumerator.
        ///</summary>
        ///<param name="list"></param>
        public Enumerator(CCRawList<T> list)
        {
            _index = -1;
            _list = list;
        }

        #region IEnumerator<T> Members

        public T Current
        {
            get { return _list.Elements[_index]; }
        }

        public void Dispose()
        {
        }

        object IEnumerator.Current
        {
            get { return _list.Elements[_index]; }
        }

        public bool MoveNext()
        {
            return ++_index < _list.count;
        }

        public void Reset()
        {
            _index = -1;
        }

        #endregion
    }

    public void InsertRange(int index, CCRawList<T> c)
    {
        if (c == null)
        {
            throw new ArgumentNullException("c");
        }
        if (index < 0 || index > this.count)
        {
            throw new ArgumentOutOfRangeException("index");
        }
        int cnt = c.count;
        if (cnt > 0)
        {
            if (this.count + cnt > Elements.Length)
                Capacity = this.count + cnt;
            if (index < this.count)
            {
                Array.Copy(Elements, index, Elements, index + cnt, this.count - index);
            }
            T[] array = new T[cnt];
            c.CopyTo(array, 0);
            array.CopyTo(this.Elements, index);
            this.count += cnt;
        }
    }

    public virtual void RemoveRange(int index, int rangeCount)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException("index");
        }
        if (rangeCount < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }
        if (this.count - index < rangeCount)
        {
            throw new ArgumentException("index");
        }
        if (this.count > 0)
        {
            this.count -= rangeCount;
            // Trailing survivors exist iff index < the post-removal count. (The old check
            // compared index + rangeCount - old-index space - against the new count, which
            // skipped the shift when 1..rangeCount elements trailed the removed range.)
            if (index < this.count)
            {
                Array.Copy(this.Elements, index + rangeCount, this.Elements, index, this.count - index);
            }
            // Clear the vacated tail unconditionally: removing every element (new count == 0)
            // previously skipped this and left stale references pinned in the backing array.
            Array.Clear(this.Elements, this.count, rangeCount);
        }
    }

    #endregion

    public void PackToCount()
    {
        if (Elements == null || count >= Elements.Length)
        {
            return;
        }

        // Floor the length at 1 so packing an empty list never yields a zero-length
        // backing array, which Add()'s doubling growth (Length * 2) could not expand.
        var minLength = Math.Max(count, 1);

        if (UseArrayPool)
        {
            // The replacement must also be rented so the list never hands a non-rented
            // array back to ArrayPool<T>.Shared on a later grow/Free. Rent returns the
            // smallest pooled bucket that fits count; if that bucket isn't actually
            // smaller than the current buffer, packing would only churn same-sized
            // buffers, so keep the existing one. (count stays authoritative for length.)
            var packed = RentBuffer(minLength);
            if (packed.Length >= Elements.Length)
            {
                ReturnBuffer(packed);
                return;
            }
            Array.Copy(Elements, packed, count);
            if (_ownsPooledBuffer)
            {
                ReturnBuffer(Elements);
            }
            Elements = packed;
            _ownsPooledBuffer = true;
        }
        else
        {
            var packed = new T[minLength];
            Array.Copy(Elements, packed, count);
            if (_ownsPooledBuffer)
            {
                ReturnBuffer(Elements);
            }
            Elements = packed;
            _ownsPooledBuffer = false;
        }
    }

    public void IncreaseCount(int size)
    {
        var newCount = count + size;
        if (Elements.Length < newCount)
        {
            Capacity = newCount;
        }
        count = newCount;
    }
}
