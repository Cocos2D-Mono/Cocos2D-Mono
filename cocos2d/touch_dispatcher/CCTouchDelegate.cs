using System.Collections.Generic;

namespace Cocos2D;

public class CCTouchDelegate : ICCTouchDelegate
{
    // Created by the first RegisterScriptTouchHandler call.
    protected Dictionary<int, string>? m_pEventTypeFuncMap;

    public virtual int TouchPriority
    {
        get
        {
            return (0);
        }
    }
    public virtual bool VisibleForTouches
    {
        get
        {
            return (true);
        }
        set
        {
            // do nothing
        }
    }
    /// <summary>
    /// functions for script call back
    /// </summary>
    public void RegisterScriptTouchHandler(int eventType, string pszScriptFunctionName)
    {
        if (m_pEventTypeFuncMap == null)
        {
            m_pEventTypeFuncMap = new Dictionary<int, string>();
        }

        (m_pEventTypeFuncMap)[eventType] = pszScriptFunctionName;
    }

    public bool DoesScriptHandlerExist(int eventType)
    {
        if (m_pEventTypeFuncMap != null)
        {
            return m_pEventTypeFuncMap.TryGetValue(eventType, out var handler) && !string.IsNullOrEmpty(handler);
        }

        return false;
    }

    public void ExcuteScriptTouchHandler(int eventType, CCTouch pTouch)
    {
        if (m_pEventTypeFuncMap != null && CCScriptEngineManager.SharedScriptEngineManager.ScriptEngine != null && m_pEventTypeFuncMap.TryGetValue(eventType, out var handler) && !string.IsNullOrEmpty(handler))
        {
            CCScriptEngineManager.SharedScriptEngineManager.ScriptEngine.ExecuteTouchEvent(handler,
                                                                                             pTouch);
        }
    }

    public void ExcuteScriptTouchesHandler(int eventType, List<CCTouch> pTouches)
    {
        if (m_pEventTypeFuncMap != null && CCScriptEngineManager.SharedScriptEngineManager.ScriptEngine != null && m_pEventTypeFuncMap.TryGetValue(eventType, out var handler) && !string.IsNullOrEmpty(handler))
        {
            CCScriptEngineManager.SharedScriptEngineManager.ScriptEngine.ExecuteTouchesEvent(handler,
                                                                                               pTouches);
        }
    }
}