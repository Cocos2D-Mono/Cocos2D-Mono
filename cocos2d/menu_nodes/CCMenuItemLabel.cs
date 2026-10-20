using System;
using System.Reflection.Emit;

namespace Cocos2D;

public class CCMenuItemLabel : CCMenuItem
{
    protected float m_fOriginalScale;
    protected CCNode? m_pLabel;
    protected CCColor3B m_tColorBackup;
    protected CCColor3B m_tDisabledColor;

    protected CCMenuItemLabel() {}

    public CCMenuItemLabel(CCNode label, Action<CCMenuItem>? selector)
    {
        InitWithLabel(label, selector);
    }

    public CCMenuItemLabel (CCNode label) : this(label, null)
    { }

    public CCColor3B DisabledColor
    {
        get { return m_tDisabledColor; }
        set { m_tDisabledColor = value; }
    }

    public CCNode? Label
    {
        get { return m_pLabel; }
        set
        {
            if (value != null)
            {
                AddChild(value);
                value.AnchorPoint = new CCPoint(0, 0);
                ContentSize = value.ContentSize;
            }

            if (m_pLabel != null)
            {
                RemoveChild(m_pLabel, true);
            }

            m_pLabel = value;
        }
    }

    public override bool Enabled
    {
        get { return base.Enabled; }
        set
        {
            // An item without a label has no color to change.
            if (m_bIsEnabled != value && m_pLabel is ICCRGBAProtocol coloredLabel)
            {
                if (!value)
                {
                    m_tColorBackup = coloredLabel.Color;
                    coloredLabel.Color = m_tDisabledColor;
                }
                else
                {
                    coloredLabel.Color = m_tColorBackup;
                }
            }
            base.Enabled = value;
        }
    }

    public string Text
    {
        get
        {
            if (m_pLabel is not ICCLabelProtocol textLabel)
            {
                throw NoTextLabel();
            }
            return textLabel.Text;
        }
        set { SetString(value); }
    }

    protected bool InitWithLabel(CCNode label, Action<CCMenuItem>? selector)
    {
        base.InitWithTarget(selector);
        m_fOriginalScale = 1.0f;
        m_tColorBackup = CCTypes.CCWhite;
        DisabledColor = new CCColor3B(126, 126, 126);
        Label = label;

        CascadeColorEnabled = true;
        CascadeOpacityEnabled = true;

        return true;
    }

    public void SetString(string label)
    {
        if (m_pLabel is not ICCLabelProtocol textLabel)
        {
            throw NoTextLabel();
        }
        textLabel.Text = label;
        ContentSize = m_pLabel.ContentSize;
    }

    private InvalidOperationException NoTextLabel()
    {
        return new InvalidOperationException(m_pLabel == null
            ? "The menu item has no label."
            : "The menu item's label, a " + m_pLabel.GetType().Name + ", has no text.");
    }

    public override void Activate()
    {
        if (m_bIsEnabled)
        {
            StopAllActions();
            Scale = m_fOriginalScale;
            base.Activate();
        }
    }

    public override void Selected()
    {
        // subclass to change the default action
        if (m_bIsEnabled)
        {
            base.Selected();

            CCAction? action = GetAction(unchecked((int) kZoomActionTag));
            if (action != null)
            {
                StopAction(action);
            }
            else
            {
                m_fOriginalScale = Scale;
            }

            CCAction zoomAction = new CCScaleTo(0.1f, m_fOriginalScale * 1.2f);
            zoomAction.Tag = unchecked((int) kZoomActionTag);
            RunAction(zoomAction);
        }
    }

    public override void Unselected()
    {
        // subclass to change the default action
        if (m_bIsEnabled)
        {
            base.Unselected();
            StopAction(unchecked((int) kZoomActionTag));
            CCAction zoomAction = new CCScaleTo(0.1f, m_fOriginalScale);
            zoomAction.Tag = unchecked((int) kZoomActionTag);
            RunAction(zoomAction);
        }
    }
}