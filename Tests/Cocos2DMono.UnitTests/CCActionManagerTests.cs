using System.Reflection;
using System.Runtime.CompilerServices;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCActionManagerTests
{
    // The tests below come from C2D-358. CCActionManager copied its targets into a static array for
    // each walk and never cleared it, so a node stayed reachable after its actions were removed, and
    // with it its parent and scene.

    [Fact]
    public void Update_LetsATargetBeCollectedOnceItsActionsAreRemoved()
    {
        WeakReference target = RunForOneUpdateThenRemove(new CCActionManager());

        Collect();

        Assert.False(target.IsAlive);
    }

    [Fact]
    public void RemoveAllActions_LetsTheTargetsBeCollected()
    {
        WeakReference target = AddThenRemoveAllActions(new CCActionManager());

        Collect();

        Assert.False(target.IsAlive);
    }

    [Fact]
    public void AManagerDroppedWithATarget_DoesNotKeepTheTargetAlive()
    {
        // Until C2D-362, managers had a finalizer. Its RemoveAllActions copied the dropped
        // manager's targets into the static array, which kept them alive after the manager was
        // gone. The weak reference tracks resurrection so that it would see that: a short one is
        // cleared before finalizers run.
        WeakReference target = DropAManagerThatHasATarget();

        Collect();

        Assert.False(target.IsAlive);
    }

    [Fact]
    public void Update_WhenAnActionThrows_StillLetsTheTargetBeCollected()
    {
        // The director's scheduler catches what an action throws, and the game goes on. Update
        // skipped its clean-up then, so the array kept the target. So did the manager, which still
        // took the target for the one being updated, and only marked it when its actions were
        // removed.
        WeakReference target = RunAThrowingActionThenRemove(new CCActionManager());

        Collect();

        Assert.False(target.IsAlive);
    }

    [Fact]
    public void Update_WhenAnActionRemovesAllActions_FinishesWithoutThrowing()
    {
        // The first target's action calls RemoveAllActions during Update. Both walk the same
        // static array, and RemoveAllActions clears it, so Update finds an empty slot for the
        // second target.
        var manager = new CCActionManager();
        var first = new CCNode();
        var second = new CCNode();
        manager.AddAction(new CCCallFunc(() => manager.RemoveAllActions()), first, false);
        manager.AddAction(new CCDelayTime(1f), second, false);

        Exception? error = Record.Exception(() => manager.Update(0.1f));

        Assert.Null(error);
        Assert.Equal(0, manager.NumberOfRunningActionsInTarget(first));
        Assert.Equal(0, manager.NumberOfRunningActionsInTarget(second));
    }

    // From C2D-361. RemoveAllActions grew the static array by doubling it once, so more than twice
    // as many targets as the array had slots made CopyTo throw ArgumentException. The array only
    // grows, so the test takes its number of targets from the array's current length.
    [Fact]
    public void RemoveAllActions_WithMoreThanTwiceAsManyTargetsAsSlots_RemovesThemAll()
    {
        var manager = new CCActionManager();
        CCNode[] nodes = Enumerable.Range(0, 2 * ScratchSlots() + 1).Select(_ => new CCNode()).ToArray();
        foreach (CCNode node in nodes)
        {
            manager.AddAction(new CCDelayTime(1f), node, false);
        }

        manager.RemoveAllActions();

        Assert.All(nodes, node => Assert.Equal(0, manager.NumberOfRunningActionsInTarget(node)));
    }

    // From C2D-362. A manager's finalizer called RemoveAllActions, which copies into the static
    // array that Update walks and then empties it. So when the GC finalized a dropped manager
    // during another manager's walk, the walk found empty slots and skipped those targets.
    [Fact]
    public void Update_WhenADroppedManagerIsFinalizedDuringTheWalk_StepsEveryTarget()
    {
        // The finalizer thread runs alongside the game thread. To run it at a known point in the
        // walk, the first target's action drops a manager whose two targets fill the walk's two
        // slots, then waits for the GC to finalize it.
        var manager = new CCActionManager();
        int steps = 0;
        bool dropped = false;
        Action step = () =>
        {
            steps++;
            if (!dropped)
            {
                dropped = true;
                DropAManagerWithTargets(2);
                Collect();
            }
        };
        manager.AddAction(new CCCallFunc(step), new CCNode(), false);
        manager.AddAction(new CCCallFunc(step), new CCNode(), false);

        manager.Update(0.1f);

        Assert.Equal(2, steps);
    }

    // Not inlined, so no local in a test refers to the node.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunForOneUpdateThenRemove(CCActionManager manager)
    {
        var node = new CCNode();
        manager.AddAction(new CCDelayTime(1f), node, false);
        manager.Update(0.1f);
        manager.RemoveAllActionsFromTarget(node);
        return new WeakReference(node);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunAThrowingActionThenRemove(CCActionManager manager)
    {
        var node = new CCNode();
        manager.AddAction(new CCCallFunc(() => throw new InvalidOperationException("Thrown by the test.")), node, false);
        Assert.Throws<InvalidOperationException>(() => manager.Update(0.1f));
        manager.RemoveAllActionsFromTarget(node);
        return new WeakReference(node);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddThenRemoveAllActions(CCActionManager manager)
    {
        var node = new CCNode();
        manager.AddAction(new CCDelayTime(1f), node, false);
        manager.RemoveAllActions();
        return new WeakReference(node);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DropAManagerThatHasATarget()
    {
        var manager = new CCActionManager();
        var node = new CCNode();
        manager.AddAction(new CCDelayTime(1f), node, false);
        return new WeakReference(node, trackResurrection: true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropAManagerWithTargets(int count)
    {
        var manager = new CCActionManager();
        for (int i = 0; i < count; i++)
        {
            manager.AddAction(new CCDelayTime(1f), new CCNode(), false);
        }
    }

    // The number of slots in the managers' static scratch array, which is private.
    private static int ScratchSlots()
    {
        FieldInfo? info = typeof(CCActionManager).GetField("_tmpKeysArray", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(info);
        return ((Array)info.GetValue(null)!).Length;
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
