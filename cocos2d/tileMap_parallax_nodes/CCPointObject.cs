

namespace Cocos2D;

public class CCPointObject
{
    // Set by CCParallaxNode.AddChild; null until then.
    public CCNode? Child;
    public CCPoint Offset;
    public CCPoint Ratio;

    public CCPointObject (CCPoint ratio, CCPoint offset)
    {
        InitWithCcPoint(ratio, offset);
    }

    public bool InitWithCcPoint(CCPoint ratio, CCPoint offset)
    {
        Ratio = ratio;
        Offset = offset;
        Child = null;
        return true;
    }
}