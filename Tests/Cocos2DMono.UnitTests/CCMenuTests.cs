using System;
using Cocos2D;
using Microsoft.Xna.Framework;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the menus' nullable conversion (C2D-299). Giving the focus to a menu with no
// items, and pressing a gamepad button while the menu had the focus but none of its items
// did, threw NullReferenceException. A child that isn't a menu item left a null in the
// menu's item list in release builds, which then threw when the menu entered a scene.
public class CCMenuTests
{
    [Fact]
    public void HasFocus_OnAMenuWithNoItems_DoesNotThrow()
    {
        var menu = new CCMenu();

        menu.HasFocus = true;

        Assert.True(menu.HasFocus);
        Assert.Null(menu.FocusedItem);
    }

    [Fact]
    public void HasFocus_GivesTheFirstItemTheFocus()
    {
        var first = new CCMenuItem();
        var menu = new CCMenu(first, new CCMenuItem());

        menu.HasFocus = true;

        Assert.Same(first, menu.FocusedItem);
    }

    [Fact]
    public void GamePadPress_WithNoFocusedItem_SelectsNothing()
    {
        var item = new CCMenuItem();
        var menu = new GamePadMenu(item);
        menu.HasFocus = true;
        item.HasFocus = false;

        menu.PressA();

        Assert.Null(menu.SelectedItem);
        Assert.False(item.IsSelected);
    }

    [Fact]
    public void GamePadPress_SelectsTheFocusedItem()
    {
        var item = new CCMenuItem();
        var menu = new GamePadMenu(item);
        menu.HasFocus = true;

        menu.PressA();

        Assert.Same(item, menu.SelectedItem);
        Assert.True(item.IsSelected);
    }

    [Fact]
    public void AddChild_ANodeThatIsNotAMenuItem_ThrowsWithoutAddingIt()
    {
        var menu = new CCMenu();

        Assert.Throws<ArgumentException>(() => menu.AddChild(new CCNode()));
        Assert.Equal(0, menu.ChildrenCount);
    }

    // Exposes the gamepad handler, which is protected.
    private sealed class GamePadMenu : CCMenu
    {
        public GamePadMenu(params CCMenuItem[] items) : base(items)
        {
        }

        public void PressA()
        {
            const CCGamePadButtonStatus none = CCGamePadButtonStatus.NotApplicable;
            OnGamePadButtonUpdate(none, none, none, CCGamePadButtonStatus.Pressed, none, none, none, none, none, PlayerIndex.One);
        }
    }
}
