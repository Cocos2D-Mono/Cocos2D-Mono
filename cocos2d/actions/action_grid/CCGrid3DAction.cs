#nullable disable
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Cocos2D;

public class CCGrid3DAction : CCGridAction
{
    private CCGrid3D _grid;

    protected CCGrid3DAction(float duration)
        : base(duration)
    {
    }

    protected CCGrid3DAction()
    {
    }

    public override CCGridBase Grid
    {
        get
        {
            if (m_pTarget != null && !m_pTarget.ContentSize.Equals(CCSize.Zero))
            {
                _grid = new CCGrid3D(m_sGridSize, m_pTarget.ContentSize.PointsToPixels());
            }
            else
            {
                _grid = new CCGrid3D(m_sGridSize);
            }

            return _grid;
        }
        set
        {
            Debug.Assert(value is CCGrid3D);
            _grid = (CCGrid3D) value;
        }
    }

    public CCVertex3F Vertex(CCGridSize pos)
    {
        return _grid.Vertex(pos);
    }

    public CCVertex3F OriginalVertex(CCGridSize pos)
    {
        return _grid.OriginalVertex(pos);
    }

    public void SetVertex(CCGridSize pos, ref CCVertex3F vertex)
    {
        _grid.SetVertex(pos, ref vertex);
    }
}