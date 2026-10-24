using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression test for the scroll views' nullable conversion (C2D-305).
public class CCTableViewTests
{
    private sealed class ThreeCells : ICCTableViewDataSource
    {
        public CCSize TableCellSizeForIndex(CCTableView table, int idx) => new CCSize(50, 20);
        public CCTableViewCell TableCellAtIndex(CCTableView table, int idx) => table.DequeueCell() ?? new CCTableViewCell();
        public int NumberOfCellsInTableView(CCTableView table) => 3;
    }

    private sealed class TouchRecorder : ICCTableViewDelegate
    {
        public CCTableViewCell? Touched;
        public void TableCellTouched(CCTableView table, CCTableViewCell cell) => Touched = cell;
        public void TableCellHighlight(CCTableView table, CCTableViewCell cell) { }
        public void TableCellUnhighlight(CCTableView table, CCTableViewCell cell) { }
        public void TableCellWillRecycle(CCTableView table, CCTableViewCell cell) { }
        public void ScrollViewDidScroll(CCScrollView view) { }
        public void ScrollViewDidZoom(CCScrollView view) { }
    }

    [Fact]
    public void TouchEnded_WithoutAParent_ReportsTheTouchedCell()
    {
        // Converting the bounding box to world space read the missing parent, which threw
        // NullReferenceException.
        var table = new CCTableView(new ThreeCells(), new CCSize(50, 60));
        var recorder = new TouchRecorder();
        table.Delegate = recorder;
        // A touch 10 points up from the table's bottom edge, on the first cell.
        var touch = new CCTouch(1, 25, CCDirector.SharedDirector.WinSize.Height - 10);

        Assert.True(table.TouchBegan(touch));
        table.TouchEnded(touch);

        Assert.NotNull(recorder.Touched);
        Assert.Equal(0, recorder.Touched.Index);
    }
}
