using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for scroll views that didn't clip their content (C2D-353).
public class CCScrollViewTests
{
    private sealed class OneCell : ICCTableViewDataSource
    {
        public CCSize TableCellSizeForIndex(CCTableView table, int idx) => new CCSize(50, 20);
        public CCTableViewCell TableCellAtIndex(CCTableView table, int idx) => table.DequeueCell() ?? new CCTableViewCell();
        public int NumberOfCellsInTableView(CCTableView table) => 1;
    }

    // Initialising a scroll view set the field behind ClippingToBounds, which skipped the setter
    // that turns clipping on. The property read true while the view drew outside itself.
    [Fact]
    public void ANewScrollView_ClipsToItsView()
    {
        var view = new CCScrollView(new CCSize(100, 100));

        Assert.True(view.ClippingToBounds);
        Assert.Equal(CCClipMode.Bounds, view.ChildClippingMode);
    }

    [Fact]
    public void AScrollViewMadeWithoutASize_ClipsToItsView()
    {
        Assert.Equal(CCClipMode.Bounds, new CCScrollView().ChildClippingMode);
    }

    [Fact]
    public void ANewTableView_ClipsToItsView()
    {
        var table = new CCTableView(new OneCell(), new CCSize(50, 60));

        Assert.True(table.ClippingToBounds);
        Assert.Equal(CCClipMode.Bounds, table.ChildClippingMode);
    }

    [Fact]
    public void ClippingToBounds_False_TurnsClippingOff()
    {
        var view = new CCScrollView(new CCSize(100, 100)) { ClippingToBounds = false };

        Assert.False(view.ClippingToBounds);
        Assert.Equal(CCClipMode.None, view.ChildClippingMode);
    }
}
