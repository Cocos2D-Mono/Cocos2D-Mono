using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCRawListTests
{
    [Fact]
    public void PackToCount_OnEmptyList_ThenAdd_DoesNotThrow()
    {
        var list = new CCRawList<int>();
        list.Add(1);
        list.Add(2);
        list.Clear();        // count = 0, backing buffer retained
        list.PackToCount();  // must not leave a zero-length backing array...

        list.Add(42);        // ...or this throws IndexOutOfRange (Capacity stuck at 0)

        Assert.Equal(1, list.count);
        Assert.Equal(42, list.Elements[0]);
    }

    [Fact]
    public void Pooled_FullClear_ThenAdd_StoresTheValue()
    {
        // A full clear returned the rented buffer and left no array, so the next Add threw
        // NullReferenceException (C2D-303).
        var list = new CCRawList<int>(useArrayPool: true);
        list.Add(1);
        list.Clear(true);

        list.Add(42);

        Assert.Equal(1, list.count);
        Assert.Equal(42, list.Elements[0]);
    }

    [Fact]
    public void Pooled_AddAndGrow_StoresValuesAndFrees()
    {
        var list = new CCRawList<int>(useArrayPool: true);  // backing buffer rented from the shared pool
        for (int i = 0; i < 100; i++)
            list.Add(i);                                    // forces several pooled grows

        Assert.Equal(100, list.count);
        Assert.Equal(50, list.Elements[50]);

        list.Clear(true);                                   // returns the rented buffer to the shared pool
        Assert.Equal(0, list.count);
    }

    [Fact]
    public void Pooled_PackToCount_NeverGrowsCapacity()
    {
        var list = new CCRawList<int>(useArrayPool: true);
        for (int i = 0; i < 100; i++)
            list.Add(i);                 // grows to a large pooled bucket
        int before = list.Elements.Length;

        list.RemoveAt(50, 50);           // count: 100 -> 50, values 0..49 retained
        list.PackToCount();

        Assert.Equal(50, list.count);
        Assert.True(list.Elements.Length <= before);      // packing must never increase capacity
        Assert.True(list.Elements.Length >= list.count);  // ...but must still fit the elements
        Assert.Equal(49, list.Elements[49]);
    }

    // Buffer provenance is tracked independently of the mutable UseArrayPool flag, so flipping
    // the flag after construction stays correct (grow returns the OLD buffer by its real
    // provenance; the NEW buffer follows the flag). These exercise both flip directions through
    // a grow + Clear(true); they assert functional correctness (contents/count, no throw) -
    // the underlying pool hygiene isn't observable through the public API.
    [Fact]
    public void UseArrayPool_ToggledOffAfterPooledConstruction_GrowsAndClearsCleanly()
    {
        var list = new CCRawList<int>(useArrayPool: true);  // buffer rented from the pool
        for (int i = 0; i < 10; i++)
            list.Add(i);
        list.UseArrayPool = false;                          // flip off mid-life
        for (int i = 10; i < 40; i++)
            list.Add(i);                                    // grow: old pooled buffer returned, new is a plain array

        Assert.Equal(40, list.count);
        Assert.Equal(25, list.Elements[25]);
        list.Clear(true);
        Assert.Equal(0, list.count);
    }

    [Fact]
    public void UseArrayPool_ToggledOnAfterUnpooledConstruction_GrowsCleanly()
    {
        var list = new CCRawList<int>(useArrayPool: false); // plain array
        for (int i = 0; i < 10; i++)
            list.Add(i);
        list.UseArrayPool = true;                           // flip on: the plain array must not be returned to the pool on grow
        for (int i = 10; i < 40; i++)
            list.Add(i);

        Assert.Equal(40, list.count);
        Assert.Equal(25, list.Elements[25]);
        list.Clear(true);
        Assert.Equal(0, list.count);
    }

    // Regression tests for RemoveRange's shift condition. The old check compared
    // (index + rangeCount) - an old-index-space value - against the post-removal count,
    // so removing a middle range with 1..rangeCount surviving trailing elements skipped
    // the shift AND the tail-clear then destroyed the survivors: stale removed values
    // stayed visible while real ones were zeroed.
    private static CCRawList<int> ListOf(int n)
    {
        var list = new CCRawList<int>();
        for (int i = 0; i < n; i++)
            list.Add(i);
        return list;
    }

    [Fact]
    public void RemoveRange_MiddleRangeNearEnd_ShiftsSingleSurvivor()
    {
        var list = ListOf(10);

        list.RemoveRange(6, 3);   // remove 6,7,8 - element 9 must survive at index 6

        Assert.Equal(7, list.count);
        Assert.Equal(5, list.Elements[5]);
        Assert.Equal(9, list.Elements[6]);
    }

    [Fact]
    public void RemoveRange_SurvivorCountEqualToRangeCount_PreservesSurvivors()
    {
        var list = ListOf(10);

        list.RemoveRange(5, 3);   // remove 5,6,7 - elements 8,9 must survive at 5,6

        Assert.Equal(7, list.count);
        Assert.Equal(8, list.Elements[5]);
        Assert.Equal(9, list.Elements[6]);
    }

    [Fact]
    public void RemoveRange_ExactTail_RemovesWithoutShift()
    {
        var list = ListOf(10);

        list.RemoveRange(7, 3);   // remove the exact tail 7,8,9 - nothing shifts

        Assert.Equal(7, list.count);
        Assert.Equal(6, list.Elements[6]);
    }

    [Fact]
    public void RemoveRange_All_EmptiesList()
    {
        var list = ListOf(10);

        list.RemoveRange(0, 10);

        Assert.Equal(0, list.count);
    }
}
