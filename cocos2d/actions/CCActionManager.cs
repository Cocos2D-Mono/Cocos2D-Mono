using System.Collections.Generic;
using System.Diagnostics;

namespace Cocos2D;

public class CCActionManager : ICCSelectorProtocol
{
    private static CCNode[] _tmpKeysArray = new CCNode[128];
    private bool _currentTargetSalvaged;
    // The element being updated, only while Update runs.
    private HashElement? _currentTarget;
    private readonly Dictionary<object, HashElement> _targets = new Dictionary<object, HashElement>();

    #region SelectorProtocol Members

    public void Update(float dt)
    {
        int count = _targets.Keys.Count;
        while (_tmpKeysArray.Length < count)
        {
            _tmpKeysArray = new CCNode[_tmpKeysArray.Length * 2];
        }

        _targets.Keys.CopyTo(_tmpKeysArray, 0);

        for (int i = 0; i < count; i++)
        {
            HashElement? elt;
            if (!_targets.TryGetValue(_tmpKeysArray[i], out elt))
            {
                continue;
            }

            _currentTarget = elt;
            _currentTargetSalvaged = false;

            if (!_currentTarget.Paused)
            {
                // The 'actions' may change while inside this loop.
                for (_currentTarget.ActionIndex = 0;
                     _currentTarget.ActionIndex < _currentTarget.Actions.Count;
                     _currentTarget.ActionIndex++)
                {
                    _currentTarget.CurrentAction = _currentTarget.Actions[_currentTarget.ActionIndex];
                    if (_currentTarget.CurrentAction == null)
                    {
                        continue;
                    }

                    _currentTarget.CurrentActionSalvaged = false;

                    _currentTarget.CurrentAction.Step(dt);

                    if (_currentTarget.CurrentActionSalvaged)
                    {
                        // The currentAction told the node to remove it. To prevent the action from
                        // accidentally deallocating itself before finishing its step, we retained
                        // it. Now that step is done, it's safe to release it.

                        //_currentTarget->currentAction->release();
                    }
                    else if (_currentTarget.CurrentAction.IsDone)
                    {
                        _currentTarget.CurrentAction.Stop();

                        CCAction action = _currentTarget.CurrentAction;
                        // Make currentAction nil to prevent removeAction from salvaging it.
                        _currentTarget.CurrentAction = null;
                        RemoveAction(action);
                    }

                    _currentTarget.CurrentAction = null;
                }
            }

            // only delete currentTarget if no actions were scheduled during the cycle (issue #481)
            if (_currentTargetSalvaged && _currentTarget.Actions.Count == 0)
            {
                DeleteHashElement(_currentTarget);
            }
        }

        // issue #635
        _currentTarget = null;
    }

    #endregion

    ~CCActionManager()
    {
        RemoveAllActions();
    }

    protected void DeleteHashElement(HashElement element)
    {
        element.Actions.Clear();
        Debug.Assert(element.Target != null, "Only elements still in the table are deleted");
        _targets.Remove(element.Target);
        element.Target = null;
    }

    protected void ActionAllocWithHashElement(HashElement element)
    {
        if (element.Actions == null)
        {
            element.Actions = new List<CCAction>();
        }
    }

    protected void RemoveActionAtIndex(int index, HashElement element)
    {
        CCAction action = element.Actions[index];

        if (action == element.CurrentAction && (!element.CurrentActionSalvaged))
        {
            element.CurrentActionSalvaged = true;
        }

        element.Actions.RemoveAt(index);

        // update actionIndex in case we are in tick. looping over the actions
        if (element.ActionIndex >= index)
        {
            element.ActionIndex--;
        }

        if (element.Actions.Count == 0)
        {
            if (_currentTarget == element)
            {
                _currentTargetSalvaged = true;
            }
            else
            {
                DeleteHashElement(element);
            }
        }
    }

    public void PauseTarget(object target)
    {
        HashElement? element;
        if (_targets.TryGetValue(target, out element))
        {
            element.Paused = true;
        }
    }

    public void ResumeTarget(object target)
    {
        HashElement? element;
        if (_targets.TryGetValue(target, out element))
        {
            element.Paused = false;
        }
    }

    public List<object> PauseAllRunningActions()
    {
        var idsWithActions = new List<object>();

        foreach (var pair in _targets)
        {
            HashElement element = pair.Value;
            if (!element.Paused)
            {
                element.Paused = true;
                idsWithActions.Add(pair.Key);
            }
        }

        return idsWithActions;
    }

    public void ResumeTargets(List<object> targetsToResume)
    {
        for (int i = 0; i < targetsToResume.Count; i++)
        {
            ResumeTarget(targetsToResume[i]);
        }
    }

    public void AddAction(CCAction action, CCNode target, bool paused)
    {
        Debug.Assert(action != null);
        Debug.Assert(target != null);

        HashElement? element;
        if (!_targets.TryGetValue(target, out element))
        {
            element = new HashElement();
            element.Paused = paused;
            element.Target = target;
            _targets.Add(target, element);
        }

        ActionAllocWithHashElement(element);

        Debug.Assert(!element.Actions.Contains(action));
        element.Actions.Add(action);

        action.StartWithTarget(target);
    }

    public void RemoveAllActions()
    {
        int count = _targets.Keys.Count;
        if (_tmpKeysArray.Length < count)
        {
            _tmpKeysArray = new CCNode[_tmpKeysArray.Length * 2];
        }

        _targets.Keys.CopyTo(_tmpKeysArray, 0);

        for (int i = 0; i < count; i++)
        {
            RemoveAllActionsFromTarget(_tmpKeysArray[i]);
        }
    }

    public void RemoveAllActionsFromTarget(CCNode? target)
    {
        if (target == null)
        {
            return;
        }

        HashElement? element;
        if (_targets.TryGetValue(target, out element))
        {
            if (element.CurrentAction != null && element.Actions.Contains(element.CurrentAction) && (!element.CurrentActionSalvaged))
            {
                element.CurrentActionSalvaged = true;
            }

            element.Actions.Clear();

            if (_currentTarget == element)
            {
                _currentTargetSalvaged = true;
            }
            else
            {
                DeleteHashElement(element);
            }
        }
    }

    public void RemoveAction(CCAction? action)
    {
        if (action == null || action.OriginalTarget == null)
        {
            return;
        }

        object target = action.OriginalTarget;
        HashElement? element;
        if (_targets.TryGetValue(target, out element))
        {
            int i = element.Actions.IndexOf(action);

            if (i != -1)
            {
                RemoveActionAtIndex(i, element);
            }
            else
            {
                CCLog.Log("cocos2d: removeAction: Action not found");
            }
        }
        else
        {
            CCLog.Log("cocos2d: removeAction: Target not found");
        }
    }

    public void RemoveActionByTag(int tag, CCNode target)
    {
        Debug.Assert((tag != CCAction.kInvalidTag));
        Debug.Assert(target != null);

        HashElement? element;
        if (_targets.TryGetValue(target, out element))
        {
            int limit = element.Actions.Count;
            for (int i = 0; i < limit; i++)
            {
                CCAction action = element.Actions[i];

                if (action.Tag == tag && action.OriginalTarget == target)
                {
                    RemoveActionAtIndex(i, element);
                    break;
                }
            }
            CCLog.Log("cocos2d : removeActionByTag: Tag " + tag + " not found");
        }
        else
        {
            CCLog.Log("cocos2d : removeActionByTag: Target not found");
        }
    }

    public CCAction? GetAction(int tag, CCNode target)
    {
        Debug.Assert(tag != CCAction.kInvalidTag);

        HashElement? element;
        if (_targets.TryGetValue(target, out element))
        {
            if (element.Actions != null)
            {
                int limit = element.Actions.Count;
                for (int i = 0; i < limit; i++)
                {
                    CCAction action = element.Actions[i];

                    if (action.Tag == tag)
                    {
                        return action;
                    }
                }
                CCLog.Log("cocos2d : GetAction: Tag " + tag + " not found");
            }
        }
        else
        {
            CCLog.Log("cocos2d : GetAction: Target not found");
        }
        return null;
    }

    public int NumberOfRunningActionsInTarget(CCNode target)
    {
        HashElement? element;
        if (_targets.TryGetValue(target, out element))
        {
            return (element.Actions != null) ? element.Actions.Count : 0;
        }
        return 0;
    }

    protected class HashElement
    {
        public int ActionIndex;
        public List<CCAction> Actions = new List<CCAction>();
        public CCAction? CurrentAction;
        public bool CurrentActionSalvaged;
        public bool Paused;
        // Set when the element is added; DeleteHashElement clears it.
        public object? Target;
    }
}