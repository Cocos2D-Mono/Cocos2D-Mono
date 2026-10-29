using Cocos2D;

namespace tests.Extensions;

internal class MenuTestLayer : BaseLayer
{
#pragma warning disable CS0649 // BaseLayer.OnAssignCCBMemberVariable sets it by name from the CCB file.
    public CCLabelBMFont mMenuItemStatusLabelBMFont;
#pragma warning restore CS0649

    public void onMenuItemAClicked(object pSender)
    {
        mMenuItemStatusLabelBMFont.Text = ("Menu Item A clicked.");
    }

    public void onMenuItemBClicked(object pSender)
    {
        mMenuItemStatusLabelBMFont.Text = ("Menu Item B clicked.");
    }

    public void onMenuItemCClicked(object pSender)
    {
        mMenuItemStatusLabelBMFont.Text = ("Menu Item C clicked.");
    }
}