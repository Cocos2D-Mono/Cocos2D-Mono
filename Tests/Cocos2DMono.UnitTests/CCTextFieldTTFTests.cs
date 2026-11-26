using Cocos2D;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression test for C2D-222: CCTextFieldTTF.CheckTouchState must clear the internal
// "registered" flag when it unregisters from the TouchDispatcher, so a field that goes
// editable -> readOnly can re-register when it becomes editable again. Registration is
// observed through the public TouchDispatcher.FindHandler (non-null == registered).
public class CCTextFieldTTFTests : System.IDisposable
{
    private CCTextFieldTTF? _field;

    [Fact]
    public void ReadOnlyToggle_ReRegistersTouchHandler()
    {
        var dispatcher = CCDirector.SharedDirector.TouchDispatcher;
        var field = new CCTextFieldTTF("hi", "arial", 12) { AutoEdit = true };
        _field = field; // tracked for teardown

        field.OnEnter();                                     // running + AutoEdit -> registers
        Assert.NotNull(dispatcher.FindHandler(field));

        field.ReadOnly = true;                               // unregisters
        Assert.Null(dispatcher.FindHandler(field));

        field.ReadOnly = false;                              // must re-register (the C2D-222 bug: it did not)
        Assert.NotNull(dispatcher.FindHandler(field));
    }

    // C2D-348: on DesktopGL the field reads the keyboard itself, once a frame while it's being
    // edited. Backspace or Delete in an empty field took a substring of length -1, which threw
    // ArgumentOutOfRangeException on every frame the key was held.
    [Theory]
    [InlineData(Keys.Back)]
    [InlineData(Keys.Delete)]
    public void DeletingInAnEmptyField_DeletesNothing_AndCountsAsAKey(Keys key)
    {
        var field = NewEditingField("");

        Frame(field);            // no keys down, so the field may take a key
        Frame(field, key);       // the press
        Frame(field, key);       // held
        Assert.Equal("", field.Text);

        // As after any key, the field takes no key in a later frame until every key is released.
        Frame(field, key, Keys.A);
        Assert.Equal("", field.Text);
        Frame(field);
        Frame(field, Keys.A);
        Assert.Equal("a", field.Text);
    }

    [Theory]
    [InlineData(Keys.Back)]
    [InlineData(Keys.Delete)]
    public void DeletingInAField_RemovesTheLastCharacter_OncePerPress(Keys key)
    {
        var field = NewEditingField("hi");

        Frame(field);
        Frame(field, key);
        Frame(field, key);       // held
        Assert.Equal("h", field.Text);

        Frame(field);            // released
        Frame(field, key);       // pressed again
        Assert.Equal("", field.Text);
    }

    private CCTextFieldTTF NewEditingField(string text)
    {
        var field = new CCTextFieldTTF(text, "arial", 12);
        _field = field; // tracked for teardown
        field.Edit();   // on DesktopGL, Update now reads the keyboard
        return field;
    }

    // One frame of the game loop, with the given keys down.
    private static void Frame(CCTextFieldTTF field, params Keys[] keys)
    {
        CCInputState.Instance.CurrentKeyboardStates[0] = new KeyboardState(keys);
        field.Update(0f);
    }

    public void Dispose()
    {
        // The keyboard state is shared, so leave no keys down for later tests.
        CCInputState.Instance.CurrentKeyboardStates[0] = default;

        // CCTextFieldTTF registers with the shared TouchDispatcher and, under DESKTOPGL,
        // schedules an Update on the shared scheduler. OnExit unregisters the touch delegate
        // (CheckTouchState runs with m_bRunning == false); Cleanup unschedules the update
        // (UnscheduleAllForTarget). Together they keep the shared singletons clean for later tests.
        if (_field != null)
        {
            _field.OnExit();
            _field.Cleanup();
            _field = null;
        }
    }
}
