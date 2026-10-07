#nullable disable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Cocos2D;

	public delegate void CCTextFieldTTFDelegate(object sender, ref string text, ref bool canceled);

public class CCTextFieldTTF : CCLabelTTF, ICCTargetedTouchDelegate
{
    private IAsyncResult _guideShowHandle;
    private string _editTitle = "Input";
    private string _editDescription = "Please provide input";
    private bool _readOnly = false;
    private bool _autoEdit;
    private bool _touchHandled;

    public event CCTextFieldTTFDelegate BeginEditing;
    public event CCTextFieldTTFDelegate EndEditing;

    private bool beginKeyboardEditing = false;
    private bool canEdit = false;

    public bool ReadOnly
    {
        get { return _readOnly; }
        set
        {
            _readOnly = value;
            
            if (!value)
            {
                EndEdit();
            }
            
            CheckTouchState();
        }
    }

    public string EditTitle
    {
        get { return _editTitle; }
        set { _editTitle = value; }
    }

    public string EditDescription
    {
        get { return _editDescription; }
        set { _editDescription = value; }
    }

    public bool AutoEdit
    {
        get { return _autoEdit; }
        set
        {
            _autoEdit = value;
            CheckTouchState();
        }
    }

    public CCTextFieldTTF(string text, string fontName, float fontSize) :
        this(text, fontName, fontSize, CCSize.Zero, CCTextAlignment.Center,
             CCVerticalTextAlignment.Top)
    {
    }

    public CCTextFieldTTF(string text, string fontName, float fontSize, CCSize dimensions,
                          CCTextAlignment hAlignment) :
                              this(text, fontName, fontSize, dimensions, hAlignment, CCVerticalTextAlignment.Top)
    {
    }

    public CCTextFieldTTF(string text, string fontName, float fontSize, CCSize dimensions,
                          CCTextAlignment hAlignment,
                          CCVerticalTextAlignment vAlignment)
    {
        InitWithString(text, fontName, fontSize, dimensions, hAlignment, vAlignment);

#if DESKTOPGL
        ScheduleUpdate();
#endif
    }

    public void Edit()
    {
        Edit(_editTitle, _editDescription);
    }

    public void Edit(string title, string defaultText)
    {
        if (!_readOnly)
        {
            var canceled = false;
            var text = Text;

            DoBeginEditing(ref text, ref canceled);

            if (!canceled)
            {
#if (ANDROID && ANDROID31_0_OR_GREATER) || __IOS__ || WINDOWS

                Task.Run(async () =>
                {
                    var newText = await Microsoft.Xna.Framework.Input.KeyboardInput.Show(title, defaultText);

                    if (newText != null && Text != newText)
                    {
                        canceled = false;

                        ScheduleOnce(
                            time =>
                            {
                                DoEndEditing(ref newText, ref canceled);

                                if (!canceled)
                                {
                                    Text = newText;
                                }
                            }, 0);
                    }
                });
#else
                beginKeyboardEditing = true;
#endif
            }
        }

    }

    protected virtual void DoBeginEditing(ref string newText, ref bool canceled)
    {
        if (BeginEditing != null)
        {
            BeginEditing(this, ref newText, ref canceled);
        }
    }

    protected virtual void DoEndEditing(ref string newText, ref bool canceled)
    {
        if (EndEditing != null)
        {
            EndEditing(this, ref newText, ref canceled);
        }
    }

    public void EndEdit()
    {
        if (_guideShowHandle != null)
			{
				//Guide.EndShowKeyboardInput(_guideShowHandle);
            _guideShowHandle = null;
        }

#if DESKTOPGL
        beginKeyboardEditing = false;

#endif
    }

    private void CheckTouchState()
    {
        if (m_bRunning)
        {
            if (!_touchHandled && !_readOnly && _autoEdit)
            {
                CCDirector.SharedDirector.TouchDispatcher.AddTargetedDelegate(this, 0, true);
                _touchHandled = true;
            }
            else if (_touchHandled && (_readOnly || !_autoEdit))
            {
                CCDirector.SharedDirector.TouchDispatcher.RemoveDelegate(this);
                _touchHandled = false;
            }
        }
        else
        {
            if (!m_bRunning && _touchHandled)
            {
                CCDirector.SharedDirector.TouchDispatcher.RemoveDelegate(this);
                _touchHandled = false;
            }
        }
    }

    public override void OnEnter()
    {
        base.OnEnter();
        CheckTouchState();
    }

    public override void OnExit()
    {
        base.OnExit();
        CheckTouchState();
    }

    public override bool TouchBegan(CCTouch pTouch)
    {
        var pos = ConvertTouchToNodeSpace(pTouch);
        if (pos.X >= 0 && pos.X < ContentSize.Width && pos.Y >= 0 && pos.Y <= ContentSize.Height)
        {
            return true;
        }
#if DESKTOPGL
        EndEdit();
#endif
        return false;
    }

    public override void TouchMoved(CCTouch pTouch)
    {
        //nothing
    }

    public override void TouchEnded(CCTouch pTouch)
    {
        var pos = ConvertTouchToNodeSpace(pTouch);
        if (pos.X >= 0 && pos.X < ContentSize.Width && pos.Y >= 0 && pos.Y <= ContentSize.Height)
        {
            Edit();
        }
    }

    public override void TouchCancelled(CCTouch pTouch)
    {
        //nothing
    }

    public override void Update(float dt)
    {
        if (beginKeyboardEditing)
        {
            HandleKeyboardInput();
        }
        base.Update(dt);
    }

    private Keys[] IgnoredKeys = new Keys[] { 
        Keys.RightShift, Keys.LeftShift, Keys.CapsLock, Keys.Enter, Keys.End, Keys.OemClear,
        Keys.Attn, Keys.Left, Keys.Right, Keys.Apps, Keys.BrowserBack, Keys.BrowserFavorites, Keys.BrowserHome,
        Keys.BrowserForward, Keys.BrowserRefresh, Keys.BrowserSearch, Keys.BrowserStop, Keys.ChatPadGreen,
        Keys.ChatPadOrange, Keys.Crsel, Keys.D0, Keys.D1, Keys.D2, Keys.D5, Keys.D3, Keys.D4, Keys.D6,
        Keys.D7, Keys.D8, Keys.D9, Keys.Down, Keys.EraseEof, Keys.Escape, Keys.Execute, Keys.F1, Keys.F2,
        Keys.Exsel, Keys.F3, Keys.F10, Keys.F11, Keys.F12, Keys.F13, Keys.F14, Keys.F15, Keys.F16, Keys.F17,
        Keys.F18, Keys.F19, Keys.F20, Keys.F21, Keys.F22, Keys.F23, Keys.F24, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9,
        Keys.Help, Keys.Home, Keys.ImeConvert, Keys.ImeNoConvert, Keys.Kana, Keys.Kanji, Keys.LaunchApplication1, Keys.LaunchApplication2,
        Keys.LaunchMail, Keys.LeftAlt, Keys.LeftControl, Keys.LeftWindows, Keys.MediaNextTrack, Keys.MediaPlayPause, Keys.MediaStop,
        Keys.MediaPreviousTrack, Keys.None, Keys.NumLock, Keys.OemCopy, Keys.OemEnlW, Keys.Pa1, Keys.PageDown, Keys.PageUp, Keys.Pause, Keys.Print,
        Keys.Play, Keys.PrintScreen, Keys.RightAlt, Keys.RightControl, Keys.ProcessKey, Keys.RightWindows, Keys.Scroll, Keys.Select, Keys.SelectMedia,
        Keys.Sleep, Keys.Tab, Keys.Up, Keys.VolumeDown, Keys.VolumeUp, Keys.VolumeMute, Keys.Zoom
    };
    private Keys[] AlteredKeys = new Keys[] { 
        Keys.OemComma, Keys.OemSemicolon, Keys.OemQuestion, 
        Keys.OemBackslash, Keys.OemCloseBrackets, Keys.OemPlus,
        Keys.OemMinus, Keys.OemPeriod, Keys.OemOpenBrackets, Keys.OemPipe,
        Keys.OemQuotes, Keys.OemTilde
    };

    public void HandleKeyboardInput()
    {
        var currentKeyboardState = CCInputState.Instance.CurrentKeyboardStates.FirstOrDefault();
        if (currentKeyboardState != null)
        {
            var pressedKeyCount = currentKeyboardState.GetPressedKeyCount();
            if (pressedKeyCount > 0 && canEdit)
            {
                var pressedKeys = currentKeyboardState.GetPressedKeys();
                var isHoldingShift = pressedKeys.Any(p => p == Keys.LeftShift || p == Keys.RightShift);

                pressedKeys.ToList().ForEach(p =>
                {
                    if (p == Keys.Back || p == Keys.Delete)
                    {
                        Text = Text.Substring(0, Text.Length - 1);
                        canEdit = false;
                        return;
                    }

                    var keyChar = (char)p;

                    if (IgnoredKeys.Any(ignoredKey => p == ignoredKey))
                    {
                        return;
                    }

                    if (AlteredKeys.Any(alteredKey => p == alteredKey))
                    {
                        var alteredKeyToAdd = "";
                        switch(p)
                        {
                            case Keys.OemComma:
                                alteredKeyToAdd = ",";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "<";
                                }
                                break;
                            case Keys.OemSemicolon:
                                alteredKeyToAdd = ";";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = ":";
                                }
                                break;
                            case Keys.OemQuestion:
                                alteredKeyToAdd = "/";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "?";
                                }
                                break;
                            case Keys.OemBackslash:
                            case Keys.OemPipe:
                                alteredKeyToAdd = "\\";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "|";
                                }
                                break;
                            case Keys.OemCloseBrackets:
                                alteredKeyToAdd = "]";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "}";
                                }
                                break;
                            case Keys.OemPlus:
                                alteredKeyToAdd = "=";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "+";
                                }
                                break;
                            case Keys.OemMinus:
                                alteredKeyToAdd = "-";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "_";
                                }
                                break;
                            case Keys.OemPeriod:
                                alteredKeyToAdd = ".";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = ">";
                                }
                                break;
                            case Keys.OemOpenBrackets:
                                alteredKeyToAdd = "[";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "{";
                                }
                                break;
                            case Keys.OemQuotes:
                                alteredKeyToAdd = "'";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "\"";
                                }
                                break;
                            case Keys.OemTilde:
                                alteredKeyToAdd = "`";
                                if (isHoldingShift)
                                {
                                    alteredKeyToAdd = "~";
                                }
                                break;
                        }

                        Text += alteredKeyToAdd;
                        canEdit = false;
                        return;
                    }

                    var keyToAdd = keyChar.ToString().ToLower();
                    if (currentKeyboardState.CapsLock)
                    {
                        keyToAdd = keyToAdd.ToUpper();
                        if (isHoldingShift)
                        {
                            keyToAdd = keyToAdd.ToLower();
                        }
                    } else if (isHoldingShift)
                    {
                        keyToAdd = keyToAdd.ToUpper();
                    }

                    Text += keyToAdd;
                    canEdit = false;
                });

            } 
            else if (pressedKeyCount == 0 || (pressedKeyCount == 1 && (currentKeyboardState.IsKeyDown(Keys.LeftShift) || currentKeyboardState.IsKeyDown(Keys.RightShift))))
            {
                canEdit = true;
            }
        }
    }
}