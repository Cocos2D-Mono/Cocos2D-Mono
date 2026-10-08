using System;

namespace Cocos2D;

public class CCCallFunc : CCActionInstant
{
    // Null for a CCCallFunc made with the parameterless constructor.
    private Action? _callFunc;
    protected string m_scriptFuncName = "";

    public CCCallFunc()
    {
        m_scriptFuncName = "";
        _callFunc = null;
    }

    public CCCallFunc(Action selector) : base()
    {
        _callFunc = selector;
    }

    protected CCCallFunc(CCCallFunc callFunc) : base(callFunc)
    {
        _callFunc = callFunc._callFunc;
        m_scriptFuncName = callFunc.m_scriptFuncName;
    }

    public virtual void Execute()
    {
        if (null != _callFunc)
        {
            _callFunc();
        }
        //if (m_nScriptHandler) {
        //    CCScriptEngineManager::sharedManager()->getScriptEngine()->executeCallFuncActionEvent(this);
        //}
    }

    public override void Update(float time)
    {
        Execute();
    }

    public override object Copy(ICCCopyable? pZone)
    {
        if (pZone != null)
        {
            //in case of being called at sub class
            var pRet = (CCCallFunc) (pZone);
            base.Copy(pZone);
            pRet._callFunc = _callFunc;
            pRet.m_scriptFuncName = m_scriptFuncName;
            return pRet;
        }
        else
        {
            return new CCCallFunc(this);
        }
    }
}