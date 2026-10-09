using System.Diagnostics.CodeAnalysis;

namespace Cocos2D;

public class CCAnimationFrame : ICCCopyable
{
    private float _delayUnits;
    private CCSpriteFrame _spriteFrame;
    private PlistDictionary? _userInfo;

    public CCSpriteFrame SpriteFrame
    {
        get { return _spriteFrame; }
    }

    public float DelayUnits
    {
        get { return _delayUnits; }
    }

    public PlistDictionary? UserInfo
    {
        get { return _userInfo; }
    }

    public CCAnimationFrame()
    {
        // Callers set the frame with InitWithSpriteFrame straight after construction, as
        // CCAnimation and CCAnimationCache do.
        _spriteFrame = null!;
    }

		public CCAnimationFrame Copy()
		{
			return (CCAnimationFrame)Copy(null);
		}

    public object Copy(ICCCopyable? pZone)
    {
        CCAnimationFrame pCopy;
        if (pZone != null)
        {
            //in case of being called at sub class
            pCopy = (CCAnimationFrame) (pZone);
        }
        else
        {
            pCopy = new CCAnimationFrame();
        }

        pCopy.InitWithSpriteFrame((CCSpriteFrame) _spriteFrame.Copy(), _delayUnits, _userInfo);

        return pCopy;
    }

    [MemberNotNull(nameof(_spriteFrame))]
    public bool InitWithSpriteFrame(CCSpriteFrame spriteFrame, float delayUnits, PlistDictionary? userInfo)
    {
        _spriteFrame = spriteFrame;
        _delayUnits = delayUnits;
        _userInfo = userInfo;
        return true;
    }
}