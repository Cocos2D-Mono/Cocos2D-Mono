#nullable disable
using System;

namespace Cocos2D;

public class CCMenuItemSprite : CCMenuItem
{
    protected float m_fOriginalScale;
    private CCNode _disabledImage;
    private CCNode _normalImage;

    private CCNode _selectedImage;

    public CCNode NormalImage
    {
        get { return _normalImage; }
        set
        {
            if (value != null)
            {
                AddChild(value);
                value.AnchorPoint = CCPoint.Zero;
                ContentSize = value.ContentSize;
            }

            if (_normalImage != null)
            {
                if (value != null)
                {
                    value.Position = _normalImage.Position;
                }
                RemoveChild(_normalImage, true);
            }

            _normalImage = value;
            UpdateImagesVisibility();
        }
    }

    public CCNode SelectedImage
    {
        get { return _selectedImage; }
        set
        {
            if (value != null)
            {
                AddChild(value);
                value.AnchorPoint = CCPoint.Zero;
            }

            if (_selectedImage != null)
            {
                if (value != null)
                {
                    value.Position = _selectedImage.Position;
                }
                RemoveChild(_selectedImage, true);
            }

            _selectedImage = value;
            UpdateImagesVisibility();
        }
    }

    public CCNode DisabledImage
    {
        get { return _disabledImage; }
        set
        {
            if (value != null)
            {
                AddChild(value);
                value.AnchorPoint = CCPoint.Zero;
            }

            if (_disabledImage != null)
            {
                if (value != null)
                {
                    value.Position = _disabledImage.Position;
                }
                RemoveChild(_disabledImage, true);
            }

            _disabledImage = value;
            UpdateImagesVisibility();
        }
    }

    public override bool Enabled
    {
        get { return base.Enabled; }
        set
        {
            base.Enabled = value;
            UpdateImagesVisibility();
        }
    }

    public override CCColor3B Color
    {
        get { return _realColor; }
        set
        {
            _displayedColor = _realColor = value;
            if (NormalImage is CCSprite)
            {
                (NormalImage as CCSprite).Color = value;
            }

            if (SelectedImage is CCSprite)
            {
                (SelectedImage as CCSprite).Color = value;
            }

            if (DisabledImage is CCSprite)
            {
                (DisabledImage as CCSprite).Color = value;
            }

            if (_cascadeColorEnabled)
            {
                var parentColor = CCTypes.CCWhite;
                var parent = m_pParent as ICCRGBAProtocol;
                if (parent != null && parent.CascadeColorEnabled)
                {
                    parentColor = parent.DisplayedColor;
                }

                UpdateDisplayedColor(parentColor);
            }
        }
    }

    public override byte Opacity
    {
        get { return _realOpacity; }
        set
        {
            _displayedOpacity = _realOpacity = value;
            if (NormalImage is CCSprite)
            {
                (NormalImage as CCSprite).Opacity = value;
            }

            if (SelectedImage is CCSprite)
            {
                (SelectedImage as CCSprite).Opacity = value;
            }

            if (DisabledImage is CCSprite)
            {
                (DisabledImage as CCSprite).Opacity = value;
            }

            if (_cascadeOpacityEnabled)
            {
                byte parentOpacity = 255;
                var pParent = m_pParent as ICCRGBAProtocol;
                if (pParent != null && pParent.CascadeOpacityEnabled)
                {
                    parentOpacity = pParent.DisplayedOpacity;
                }
                UpdateDisplayedOpacity(parentOpacity);
            }
        }
    }

    public CCMenuItemSprite()
        : this(null, null, null, null)
    {
        ZoomBehaviorOnTouch = false;
    }

    public CCMenuItemSprite(Action<CCMenuItem> selector)
        : base(selector)
    {
    }

    public CCMenuItemSprite(string normalSprite, Action<CCMenuItem> selector)
        : this(new CCSprite(normalSprite), new CCSprite(normalSprite), new CCSprite(normalSprite), selector)
    {
    }

    public CCMenuItemSprite(string normalSprite, string selectedSprite, Action<CCMenuItem> selector)
        :this(new CCSprite(normalSprite), new CCSprite(selectedSprite), new CCSprite(normalSprite), selector)
    {
    }

    public CCMenuItemSprite(CCTexture2D normalSprite, CCTexture2D selectedSprite, Action<CCMenuItem> selector)
        : this(new CCSprite(normalSprite), new CCSprite(selectedSprite), new CCSprite(normalSprite), selector)
    {
    }

    public CCMenuItemSprite(CCNode normalSprite, CCNode selectedSprite)
        :this(normalSprite, selectedSprite, normalSprite, null)
    {
    }

		public CCMenuItemSprite(CCNode normalSprite, CCNode selectedSprite, Action<CCMenuItem> selector)
        :this(normalSprite, selectedSprite, normalSprite, selector)
    {
    }

    public CCMenuItemSprite(CCNode normalSprite, CCNode selectedSprite, CCNode disabledSprite, Action<CCMenuItem> selector)
    {
        InitWithTarget(selector);

        NormalImage = normalSprite;
        SelectedImage = selectedSprite;
        DisabledImage = disabledSprite;

        if (_normalImage != null)
        {
            ContentSize = _normalImage.ContentSize;
        }

        CascadeColorEnabled = true;
        CascadeOpacityEnabled = true;
    }

    /// <summary>
    /// Set this to true if you want to zoom-in/out on the button image like the CCMenuItemLabel works.
    /// </summary>
    public bool ZoomBehaviorOnTouch { get; set; }

    public override void Selected()
    {
        base.Selected();

        if (_normalImage != null)
        {
            if (_disabledImage != null)
            {
                _disabledImage.Visible = false;
            }

            if (_selectedImage != null)
            {
                _normalImage.Visible = false;
                _selectedImage.Visible = true;
            }
            else
            {
                _normalImage.Visible = true;
                if (ZoomBehaviorOnTouch)
                {
                    CCAction action = GetAction(unchecked((int)kZoomActionTag));
                    if (action != null)
                    {
                        StopAction(action);
                    }
                    else
                    {
                        m_fOriginalScale = Scale;
                    }

                    CCAction zoomAction = new CCScaleTo(0.1f, m_fOriginalScale * 1.2f);
                    zoomAction.Tag = unchecked((int)kZoomActionTag);
                    RunAction(zoomAction);
                }
            }
        }
    }

    public override void Unselected()
    {
        base.Unselected();
        if (_normalImage != null)
        {
            _normalImage.Visible = true;

            if (_selectedImage != null)
            {
                _selectedImage.Visible = false;
            }
            if (ZoomBehaviorOnTouch)
            {
                StopAction(unchecked((int)kZoomActionTag));
                CCAction zoomAction = new CCScaleTo(0.1f, m_fOriginalScale);
                zoomAction.Tag = unchecked((int)kZoomActionTag);
                RunAction(zoomAction);
            }

            if (_disabledImage != null)
            {
                _disabledImage.Visible = false;
            }
        }
    }

    public override void Activate()
    {
        if (m_bIsEnabled)
        {
            if (ZoomBehaviorOnTouch)
            {
                StopAllActions();
                Scale = m_fOriginalScale;
            }
            base.Activate();
        }
    }

    // Helper 
    private void UpdateImagesVisibility()
    {
        if (m_bIsEnabled)
        {
            if (_normalImage != null) _normalImage.Visible = true;
            if (_selectedImage != null) _selectedImage.Visible = false;
            if (_disabledImage != null) _disabledImage.Visible = false;
        }
        else
        {
            if (_disabledImage != null)
            {
                if (_normalImage != null) _normalImage.Visible = false;
                if (_selectedImage != null) _selectedImage.Visible = false;
                if (_disabledImage != null) _disabledImage.Visible = true;
            }
            else
            {
                if (_normalImage != null) _normalImage.Visible = true;
                if (_selectedImage != null) _selectedImage.Visible = false;
                if (_disabledImage != null) _disabledImage.Visible = false;
            }
        }
    }
}