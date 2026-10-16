using System;
using System.Diagnostics;

namespace Cocos2D;

public class CCCallFuncN : CCCallFunc
{
    // Null for a CCCallFuncN made with the parameterless constructor.
    private Action<CCNode>? _callFuncN;

    public CCCallFuncN() : base()
    {
        _callFuncN = null;
    }


    public CCCallFuncN(Action<CCNode> selector)
    {
        InitWithTarget(selector);
    }

    public CCCallFuncN(CCCallFuncN callFuncN) : base(callFuncN)
    {
        InitWithTarget(callFuncN._callFuncN);
    }

    public bool InitWithTarget(Action<CCNode>? selector)
    {
        _callFuncN = selector;
        return true;
    }

    public override object Copy(ICCCopyable? zone)
    {
        if (zone != null)
        {
            //in case of being called at sub class
            var pRet = (CCCallFuncN) (zone);
            base.Copy(zone);

            pRet.InitWithTarget(_callFuncN);

            return pRet;
        }
        else
        {
            return new CCCallFuncN(this);
        }
    }

    public override void Execute()
    {
        if (null != _callFuncN)
        {
            Debug.Assert(m_pTarget != null, "Execute only runs while the action is started");
            _callFuncN(m_pTarget);
        }
        //if (m_nScriptHandler) {
        //    CCScriptEngineManager::sharedManager()->getScriptEngine()->executeFunctionWithobject(m_nScriptHandler, m_pTarget, "CCNode");
        //}
    }
}