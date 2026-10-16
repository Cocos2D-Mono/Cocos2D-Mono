#nullable disable
using System;

namespace Cocos2D;

public class CCMenuItemImage : CCMenuItemSprite
{
    public CCMenuItemImage() : this(null, null, null, null)
    {
    }

    public CCMenuItemImage(string normalImage)
        : this(normalImage, normalImage, normalImage, null)
    {
    }

    public CCMenuItemImage(string normalImage, string selectedImage)
        :this(normalImage, selectedImage, normalImage, null)
    {
    }

    public CCMenuItemImage(string normalImage, string selectedImage, Action<CCMenuItem> selector)
        :this(normalImage, selectedImage, normalImage, selector)
    {
    }

    public CCMenuItemImage(string normalImage, string selectedImage, string disabledImage)
        : this(normalImage, selectedImage, disabledImage, null)
    {
    }

    public CCMenuItemImage(string normalImage, string selectedImage, string disabledImage, Action<CCMenuItem> selector)
        : base(selector)
    {

        if (!string.IsNullOrEmpty(normalImage))
        {
            NormalImage = new CCSprite(normalImage);
        }

        if (!string.IsNullOrEmpty(selectedImage))
        {
            SelectedImage = new CCSprite(selectedImage);
        }

        if (!string.IsNullOrEmpty(disabledImage))
        {
            DisabledImage = new CCSprite(disabledImage);
        }
    }

    public void SetNormalSpriteFrame(CCSpriteFrame frame)
    {
        NormalImage = new CCSprite(frame);
    }

    public void SetSelectedSpriteFrame(CCSpriteFrame frame)
    {
        SelectedImage = new CCSprite(frame);
    }

    public void SetDisabledSpriteFrame(CCSpriteFrame frame)
    {
        DisabledImage = new CCSprite(frame);
    }
}