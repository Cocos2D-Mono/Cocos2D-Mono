using Cocos2D;

namespace cocos2d.misc_nodes;

public class CCSwappableNode : CCNode
{
    private CCPoint _currentTargetPosition;
    public virtual CCPoint CurrentTargetPosition
    {
        get
        {
            if (_currentTargetPosition != default(CCPoint))
            {
                return _currentTargetPosition;
            }
            else
            {
                return Position;
            }
        }
        set
        {
            _currentTargetPosition = value;
        }
    }

    public CCSwappableNode()
    {
    }
}
