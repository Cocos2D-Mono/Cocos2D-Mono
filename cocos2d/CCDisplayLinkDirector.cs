using Microsoft.Xna.Framework;

namespace Cocos2D;

public class CCDisplayLinkDirector : CCDirector
{
    private bool _invalid;

    public override double AnimationInterval
    {
        get { return base.AnimationInterval; }
        set
        {
            m_dAnimationInterval = value;

            if (!_invalid)
            {
                StopAnimation();
                StartAnimation();
            }
        }
    }

    public override void StopAnimation()
    {
        _invalid = true;
    }

    public override void StartAnimation()
    {
        _invalid = false;
        // When using CCGameView, CCApplication.SharedApplication may not exist
        // Animation interval is managed by the game loop in that case
        if (CCApplication.SharedApplication != null)
        {
            CCApplication.SharedApplication.AnimationInterval = m_dAnimationInterval;
        }
    }

    public override void MainLoop(GameTime gameTime)
    {
        if (m_bPurgeDirectorInNextLoop)
        {
            PurgeDirector();
            m_bPurgeDirectorInNextLoop = false;
        }
        else if (!_invalid)
        {
            DrawScene(gameTime);
        }
    }
}