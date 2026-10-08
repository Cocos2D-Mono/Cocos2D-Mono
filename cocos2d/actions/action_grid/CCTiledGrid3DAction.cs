using System.Diagnostics;

namespace Cocos2D;

public class CCTiledGrid3DAction : CCGridAction
{
    private CCTiledGrid3D? _grid;

    public CCTiledGrid3DAction()
    {
    }

    public CCTiledGrid3DAction(float duration)
        : base(duration)
    {
    }

    public CCTiledGrid3DAction(float duration, CCGridSize gridSize)
        : base(duration, gridSize)
    {
    }

    public CCQuad3 Tile(CCGridSize pos)
    {
        Debug.Assert(_grid != null, "Tile only works once the action has started");
        return _grid.Tile(pos);
    }

    public CCQuad3 OriginalTile(CCGridSize pos)
    {
        Debug.Assert(_grid != null, "OriginalTile only works once the action has started");
        return _grid.OriginalTile(pos);
    }

    public void SetTile(CCGridSize pos, ref CCQuad3 coords)
    {
        Debug.Assert(_grid != null, "SetTile only works once the action has started");
        _grid.SetTile(pos, ref coords);
    }

    public override CCGridBase Grid
    {
        get
        {
            _grid = new CCTiledGrid3D(m_sGridSize);
            return _grid;
        }
        set
        {
            Debug.Assert(value is CCTiledGrid3D);
            _grid = (CCTiledGrid3D) value;
        }
    }
}