#nullable disable
using System;

namespace Cocos2D;

public class CCCallFuncO : CCCallFunc
{
    private Action<object> _callFuncO;
    private object _object;

    public CCCallFuncO()
    {
        _object = null;
        _callFuncO = null;
    }

    public CCCallFuncO(Action<object> selector, object pObject) : this()
    {
        InitWithTarget(selector, pObject);
    }

    protected CCCallFuncO(CCCallFuncO callFuncO) : base(callFuncO)
    {
        InitWithTarget(callFuncO._callFuncO, callFuncO._object);
    }

    public bool InitWithTarget(Action<object> selector, object pObject)
    {
        _object = pObject;
        _callFuncO = selector;
        return true;
    }

    // super methods
    public override object Copy(ICCCopyable zone)
    {
        if (zone != null)
        {
            //in case of being called at sub class
            var pRet = (CCCallFuncO) (zone);
            base.Copy(zone);
            pRet.InitWithTarget(_callFuncO, _object);
            return pRet;
        }
        else
        {
            return new CCCallFuncO(this);
        }
    }

    public override void Execute()
    {
        if (null != _callFuncO)
        {
            _callFuncO(_object);
        }

        //if (CCScriptEngineManager::sharedScriptEngineManager()->getScriptEngine()) {
        //    CCScriptEngineManager::sharedScriptEngineManager()->getScriptEngine()->executeCallFunc0(
        //            m_scriptFuncName.c_str(), _object);
        //}
    }

    public object Object
    {
        get { return _object; }
        set { _object = value; }
    }
}