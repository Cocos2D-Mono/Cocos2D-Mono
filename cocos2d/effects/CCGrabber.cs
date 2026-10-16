using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public class CCGrabber
{
    // Null when the previous target was the back buffer.
    private RenderTarget2D? _oldRenderTarget;

    public void Grab(CCTexture2D pTexture)
    {
        CCDrawManager.CreateRenderTarget(pTexture, RenderTargetUsage.DiscardContents);
    }

    public void BeforeRender(CCTexture2D pTexture)
    {
        _oldRenderTarget = CCDrawManager.GetRenderTarget();
        CCDrawManager.SetRenderTarget(pTexture);
        CCDrawManager.Clear(Color.Transparent);
    }

    public void AfterRender(CCTexture2D pTexture)
    {
        CCDrawManager.SetRenderTarget(_oldRenderTarget);
    }
}