using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the runtime classes (C2D-303).
public class CCFocusManagerTests
{
    private sealed class Focusable : ICCFocusable
    {
        public bool CanReceiveFocus => true;
        public bool HasFocus { get; set; }
    }

    [Fact]
    public void FocusNextItem_AfterTheOnlyFocusedItemIsRemoved_LeavesNoFocus()
    {
        // The removed item stayed current, and moving on from it took the first item of an
        // empty list, which threw NullReferenceException.
        CCFocusManager manager = CCFocusManager.Instance;
        var item = new Focusable { HasFocus = true };
        manager.Add(item);
        manager.Remove(item);

        manager.FocusNextItem();

        Assert.False(item.HasFocus);
        Assert.Null(manager.ItemWithFocus);
    }

    [Fact]
    public void FocusPreviousItem_AfterTheOnlyFocusedItemIsRemoved_LeavesNoFocus()
    {
        // The same as FocusNextItem, with the last item of the empty list.
        CCFocusManager manager = CCFocusManager.Instance;
        var item = new Focusable { HasFocus = true };
        manager.Add(item);
        manager.Remove(item);

        manager.FocusPreviousItem();

        Assert.False(item.HasFocus);
        Assert.Null(manager.ItemWithFocus);
    }
}
