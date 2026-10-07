#nullable disable

using System.Collections.Generic;
using System.Linq;

namespace Cocos2D;

public class CCTouchDispatcher : ICCEGLTouchDelegate
{
    private static List<CCTouch> pMutableTouches;
    private bool _dispatchEvents;
    private bool _locked;
    private bool _toAdd;
    private bool _toQuit;
    private bool _toRemove;
    private List<CCTouchHandler> _handlersToAdd;
    private List<object> _handlersToRemove;
    protected List<CCTouchHandler> m_pStandardHandlers;
    protected List<CCTouchHandler> m_pTargetedHandlers;
    private bool _rearrangeTargetedHandlersUponTouch = false;
    private bool _rearrangeStandardHandlersUponTouch = false;

    /// <summary>
    /// Whether or not the events are going to be dispatched. Default: true
    /// </summary>
    public bool IsDispatchEvents
    {
        get { return _dispatchEvents; }
        set { _dispatchEvents = value; }
    }

    #region IEGLTouchDelegate Members

    public virtual void TouchesBegan(List<CCTouch> touches)
    {
        if (_dispatchEvents)
        {
            Touches(touches, (int) CCTouchType.Began);
        }
    }

    public virtual void TouchesMoved(List<CCTouch> touches)
    {
        if (_dispatchEvents)
        {
            Touches(touches, CCTouchType.Moved);
        }
    }

    public virtual void TouchesEnded(List<CCTouch> touches)
    {
        if (_dispatchEvents)
        {
            Touches(touches, CCTouchType.Ended);
        }
    }

    public virtual void TouchesCancelled(List<CCTouch> touches)
    {
        if (_dispatchEvents)
        {
            Touches(touches, CCTouchType.Cancelled);
        }
    }

    #endregion

    public bool Init()
    {
        _dispatchEvents = true;
        m_pTargetedHandlers = new List<CCTouchHandler>();
        m_pStandardHandlers = new List<CCTouchHandler>();

        _handlersToAdd = new List<CCTouchHandler>();
        _handlersToRemove = new List<object>();

        _toRemove = false;
        _toAdd = false;
        _toQuit = false;
        _locked = false;

        return true;
    }

    /// <summary>
    /// Use this to update the priority of the given delegate when its graph priority 
    /// changes due to a parenting change.
    /// </summary>
    /// <param name="d"></param>
    public void UpdateGraphPriority(ICCTouchDelegate d)
    {
        CCTouchHandler h = FindHandler(d);
        if (h != null)
        {
            h.Priority = d.TouchPriority;
            RearrangeAllHandlersUponTouch();
        }
    }

    /// <summary>
    /// Adds a standard touch delegate to the dispatcher's list.
    /// See StandardTouchDelegate description.
    /// IMPORTANT: The delegate will be retained.
    /// </summary>
    public void AddStandardDelegate(ICCStandardTouchDelegate pDelegate, int nPriority)
    {
        CCTouchHandler pHandler = CCStandardTouchHandler.HandlerWithDelegate(pDelegate, nPriority);
        if (!_locked)
        {
            ForceAddHandler(pHandler, m_pStandardHandlers);
        }
        else
        {
            _handlersToAdd.Add(pHandler);
            _toAdd = true;
        }
    }

    public void RearrangeAllHandlersUponTouch()
    {
        _rearrangeStandardHandlersUponTouch = true;
        _rearrangeTargetedHandlersUponTouch = true;
    }

    public void AddStandardDelegate(ICCStandardTouchDelegate pDelegate)
    {
        AddStandardDelegate(pDelegate, pDelegate.TouchPriority);
        _rearrangeStandardHandlersUponTouch = true;
    }

    /// <summary>
    /// Adds a targeted touch delegate to the dispatcher's list.
    /// See TargetedTouchDelegate description.
    /// IMPORTANT: The delegate will be retained.
    /// </summary>
    public void AddTargetedDelegate(ICCTargetedTouchDelegate pDelegate, int nPriority, bool bConsumesTouches)
    {
        CCTouchHandler pHandler = CCTargetedTouchHandler.HandlerWithDelegate(pDelegate, nPriority, bConsumesTouches);
        if (!_locked)
        {
            ForceAddHandler(pHandler, m_pTargetedHandlers);
        }
        else
        {
            _handlersToAdd.Add(pHandler);
            _toAdd = true;
        }
    }

    public void AddTargetedDelegate(ICCTargetedTouchDelegate pDelegate, bool bConsumesTouches)
    {
        AddTargetedDelegate(pDelegate, pDelegate.TouchPriority, bConsumesTouches);
        _rearrangeTargetedHandlersUponTouch = true;
    }

    public void AddTargetedDelegate(ICCTargetedTouchDelegate pDelegate)
    {
        AddTargetedDelegate(pDelegate, pDelegate.TouchPriority, true);
        _rearrangeTargetedHandlersUponTouch = true;
    }

    /// <summary>
    /// Removes a touch delegate.
    /// The delegate will be released
    /// </summary>
    public void RemoveDelegate(ICCTouchDelegate pDelegate)
    {
        if (pDelegate == null)
        {
            return;
        }

        if (!_locked)
        {
            ForceRemoveDelegate(pDelegate);
        }
        else
        {
            _handlersToRemove.Add(pDelegate);
            _toRemove = true;
        }
    }

    /// <summary>
    /// Removes all touch delegates, releasing all the delegates
    /// </summary>
    public void RemoveAllDelegates()
    {
        if (!_locked)
        {
            ForceRemoveAllDelegates();
        }
        else
        {
            _toQuit = true;
        }
    }

    /// <summary>
    /// Changes the priority of a previously added delegate. 
    /// The lower the number, the higher the priority
    /// </summary>
    public void SetPriority(int nPriority, ICCTouchDelegate pDelegate)
    {
        CCTouchHandler handler = FindHandler(pDelegate);
        handler.Priority = nPriority;

        RearrangeHandlers(m_pTargetedHandlers);
        RearrangeHandlers(m_pStandardHandlers);
    }

    public void Touches(List<CCTouch> pTouches, CCTouchType touchType)
    {
        _locked = true;
        if (_rearrangeTargetedHandlersUponTouch)
        {
            RearrangeHandlers(m_pTargetedHandlers);
            _rearrangeTargetedHandlersUponTouch = false;
        }
        if (_rearrangeStandardHandlersUponTouch)
        {
            RearrangeHandlers(m_pStandardHandlers);
            _rearrangeStandardHandlersUponTouch = false;
        }

        // optimization to prevent a mutable copy when it is not necessary
        int uTargetedHandlersCount = m_pTargetedHandlers.Count;
        int uStandardHandlersCount = m_pStandardHandlers.Count;
        bool bNeedsMutableSet = (uTargetedHandlersCount > 0 && uStandardHandlersCount > 0);

        if (bNeedsMutableSet)
        {
            CCTouch[] tempArray = pTouches.ToArray();
            pMutableTouches = tempArray.ToList();
        }
        else
        {
            pMutableTouches = pTouches;
        }

//            var sHelper = (CCTouchType) touchType;

        //
        // Process non-began touches that were consumed by a handler and they 
        // need to be focused on their targets
        //
        /*
         * Remove this preprocessing step for these touches. Let't the claim/unclaimed logic
         * handle targeted touches.
        if (touchType != CCTouchType.Began)
        {
#if WINDOWS_PHONE || XBOX360
            List<CCTouch> focused = new List<CCTouch>();
            foreach (CCTouch t in pTouches)
            {
                if (t.Consumer != null)
                {
                    focused.Add(t);
                }
            }
#else
            var focused = pTouches.FindAll((t) => t.Consumer != null);
#endif
            if (focused != null)
            {
                // Thes touches already were handled by another consumer, so continue to send them to that
                // consumer. Make sure we remove them from the other lists.
                foreach (CCTouch t in focused)
                {
                    var pDelegate = (ICCTargetedTouchDelegate)(t.Consumer.Delegate);
                    switch (touchType)
                    {
                        case CCTouchType.Moved:
                            pDelegate.TouchMoved(t);
                            break;
                        case CCTouchType.Ended:
                            pDelegate.TouchEnded(t);
                            t.Consumer.ClaimedTouches.Remove(t);
                            break;
                        case CCTouchType.Cancelled:
                            pDelegate.TouchCancelled(t);
                            t.Consumer.ClaimedTouches.Remove(t);
                            break;
                    }
                }
            }
        }
         */

        // process the target handlers 1st
        if (uTargetedHandlersCount > 0)
        {
            #region CCTargetedTouchHandler

            foreach (CCTouch pTouch in pTouches)
            {
                if (pTouch.Consumer != null)
                {
                    var pDelegate = (ICCTargetedTouchDelegate)(pTouch.Consumer.Delegate);
                    switch (touchType)
                    {
                        case CCTouchType.Moved:
                            pDelegate.TouchMoved(pTouch);
                            break;
                        case CCTouchType.Ended:
                            pDelegate.TouchEnded(pTouch);
                            pTouch.Consumer.ClaimedTouches.Remove(pTouch);
                            break;
                        case CCTouchType.Cancelled:
                            pDelegate.TouchCancelled(pTouch);
                            pTouch.Consumer.ClaimedTouches.Remove(pTouch);
                            break;
                    }
                    continue;
                }
                bool bClaimed = false;
                foreach (CCTargetedTouchHandler pHandler in m_pTargetedHandlers)
                {
                    if (bClaimed)
                    {
                        break;
                    }
                    var pDelegate = (ICCTargetedTouchDelegate) (pHandler.Delegate);
                    if (!pDelegate.VisibleForTouches)
                    {
                        continue;
                    }

                    if (touchType == CCTouchType.Began)
                    {
                        bClaimed = pDelegate.TouchBegan(pTouch);

                        // Touches must be claimed here regardless of ConsumesTouches.
                        // If the touch doesn't get claimed properly then it will not be
                        // associated with the proper delegate for TouchMoved.
                        if (bClaimed)
                        {
                            pHandler.ClaimedTouches.Add(pTouch);
                            pTouch.Consumer = pHandler;
                        }
                    }
                    else
                    {
                        if (pHandler.ClaimedTouches.Contains(pTouch))
                        {
                            // move ended cancelled
                            bClaimed = true;

                            switch (touchType)
                            {
                                case CCTouchType.Moved:
                                    pDelegate.TouchMoved(pTouch);
                                    break;
                                case CCTouchType.Ended:
                                    pDelegate.TouchEnded(pTouch);
                                    pHandler.ClaimedTouches.Remove(pTouch);
                                    break;
                                case CCTouchType.Cancelled:
                                    pDelegate.TouchCancelled(pTouch);
                                    pHandler.ClaimedTouches.Remove(pTouch);
                                    break;
                            }
                        }
                    }

                    if (bClaimed && pHandler.ConsumesTouches)
                    {
                        if (bNeedsMutableSet)
                        {
                            pMutableTouches.Remove(pTouch);
                        }

                        break;
                    }
                }
            }

            #endregion
        }

        // process standard handlers 2nd
        if (uStandardHandlersCount > 0 && pMutableTouches.Count > 0)
        {
            #region CCStandardTouchHandler

            foreach (CCStandardTouchHandler pHandler in m_pStandardHandlers)
            {
                var pDelegate = (ICCStandardTouchDelegate) pHandler.Delegate;
                if (!pDelegate.VisibleForTouches)
                {
                    continue;
                }
                switch (touchType)
                {
                    case CCTouchType.Began:
                        pDelegate.TouchesBegan(pMutableTouches);
                        break;
                    case CCTouchType.Moved:
                        pDelegate.TouchesMoved(pMutableTouches);
                        break;
                    case CCTouchType.Ended:
                        pDelegate.TouchesEnded(pMutableTouches);
                        break;
                    case CCTouchType.Cancelled:
                        pDelegate.TouchesCancelled(pMutableTouches);
                        break;
                }
            }

            #endregion
        }

        if (bNeedsMutableSet)
        {
            pMutableTouches = null;
        }

        //
        // Optimization. To prevent a [handlers copy] which is expensive
        // the add/removes/quit is done after the iterations
        //
        _locked = false;
        if (_toRemove)
        {
            _toRemove = false;
            for (int i = 0; i < _handlersToRemove.Count; ++i)
            {
                ForceRemoveDelegate((ICCTouchDelegate) _handlersToRemove[i]);
            }
            _handlersToRemove.Clear();
        }

        if (_toAdd)
        {
            _toAdd = false;
            foreach (CCTouchHandler pHandler in _handlersToAdd)
            {
                if (pHandler is CCTargetedTouchHandler && pHandler.Delegate is ICCTargetedTouchDelegate)
                {
                    ForceAddHandler(pHandler, m_pTargetedHandlers);
                }
                else if (pHandler is CCStandardTouchHandler && pHandler.Delegate is ICCStandardTouchDelegate)
                {
                    ForceAddHandler(pHandler, m_pStandardHandlers);
                }
                else
                {
                    CCLog.Log("ERROR: inconsistent touch handler and delegate found in _handlersToAdd of CCTouchDispatcher");
                }
            }

            _handlersToAdd.Clear();
        }

        if (_toQuit)
        {
            _toQuit = false;
            ForceRemoveAllDelegates();
        }
    }

    public CCTouchHandler FindHandler(ICCTouchDelegate pDelegate)
    {
        foreach (CCTouchHandler handler in m_pTargetedHandlers)
        {
            if (handler.Delegate == pDelegate)
            {
                return handler;
            }
        }

        foreach (CCTouchHandler handler in m_pStandardHandlers)
        {
            if (handler.Delegate == pDelegate)
            {
                return handler;
            }
        }

        return null;
    }

    protected void ForceRemoveDelegate(ICCTouchDelegate pDelegate)
    {
        // remove handler from m_pStandardHandlers
        for (var index = 0; index < m_pStandardHandlers.Count; index++)
        {
            var pHandler = m_pStandardHandlers[index];
            if (pHandler != null && pHandler.Delegate == pDelegate)
            {
                m_pStandardHandlers.Remove(pHandler);
                break;
            }
        }

        // remove handler from m_pTargetedHandlers
        for (var index = 0; index < m_pTargetedHandlers.Count; index++)
        {
            var pHandler = m_pTargetedHandlers[index];
            if (pHandler != null && pHandler.Delegate == pDelegate)
            {
                m_pTargetedHandlers.Remove(pHandler);
                break;
            }
        }
    }

    protected void ForceAddHandler(CCTouchHandler pHandler, List<CCTouchHandler> pArray)
    {
        int u = 0;
        for (int i = 0; i < pArray.Count; i++)
        {
            CCTouchHandler h = pArray[i];

            if (h != null)
            {
                if (h.Priority < pHandler.Priority)
                {
                    ++u;
                }

                if (h.Delegate == pHandler.Delegate)
                {
                    return;
                }
            }
        }

        pArray.Insert(u, pHandler);
    }

    protected void ForceRemoveAllDelegates()
    {
        m_pStandardHandlers.Clear();
        m_pTargetedHandlers.Clear();
    }

    protected void RearrangeHandlers(List<CCTouchHandler> pArray)
    {
        if (CCConfiguration.SharedConfiguration.UseGraphPriority)
        {
            pArray.Sort(HighToLow);
        }
        else
        {
            pArray.Sort(LowToHigh);
        }
    }

    /// <summary>
    /// Used for sorting low to high order of priority
    /// </summary>
    private int LowToHigh(CCTouchHandler p1, CCTouchHandler p2)
    {
        return p1.Priority - p2.Priority;
    }
    /// <summary>
    /// Used for sorting high to low order of priority
    /// </summary>
    private int HighToLow(CCTouchHandler p1, CCTouchHandler p2)
    {
        return p2.Priority - p1.Priority;
    }
}

public enum CCTouchType
{
    Began = 0,
    Moved = 1,
    Ended = 2,
    Cancelled = 3,
    TouchMax = 4
}