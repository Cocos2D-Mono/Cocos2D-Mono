#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cocos2D;

namespace Cocos2D
{
    //
    // CCTimer
    //
    /** @brief Light weight timer */

    public class CCTimer : ICCSelectorProtocol
    {
        private CCScheduler _scheduler;
        private readonly ICCSelectorProtocol _target;

        private readonly bool _runForever;
        private readonly float _delay;
        private readonly uint _repeat; //0 = once, 1 is 2 x executed
        private float _elapsed;
        private bool _useDelay;

        //private int m_nScriptHandler;
        private uint _timesExecuted;

        public float OriginalInterval;
        public float Interval;
        public Action<float> Selector;

        public CCTimer()
        {
        }

        /** Initializes a timer with a target and a selector. 
         */

        public CCTimer(CCScheduler scheduler, ICCSelectorProtocol target, Action<float> selector)
            : this(scheduler, target, selector, 0, 0, 0)
        {
        }

        /** Initializes a timer with a target, a selector and an interval in seconds. 
         *  Target is not needed in c#, it is just for compatibility.
         */

        public CCTimer(CCScheduler scheduler, ICCSelectorProtocol target, Action<float> selector, float seconds)
            : this(scheduler, target, selector, seconds, 0, 0)
        {
        }

        public CCTimer(CCScheduler scheduler, ICCSelectorProtocol target, Action<float> selector, float seconds,
                       uint repeat, float delay)
        {
            _scheduler = scheduler;
            _target = target;
            Selector = selector;
            _elapsed = -1;
            OriginalInterval = seconds;
            Interval = seconds;
            _delay = delay;
            _useDelay = delay > 0f;
            _repeat = repeat;
            _runForever = (_repeat == CCScheduler.kCCRepeatForever);
        }

        /*
        public CCTimer(int scriptHandler, float seconds)
        {
            m_nScriptHandler = scriptHandler;
            Elapsed = -1;
            Interval = seconds;
        }
        */

        #region SelectorProtocol Members

        public void Update(float dt)
        {
            if (_elapsed == -1)
            {
                _elapsed = 0;
                _timesExecuted = 0;
            }
            else
            {
                if (_runForever && !_useDelay)
                {
                    //standard timer usage
                    _elapsed += dt;
                    if (_elapsed >= Interval)
                    {
                        if (Selector != null)
                        {
                            Selector(_elapsed);
                        }

                        /*
                        if (m_nScriptHandler != 0)
                        {
                            CCScriptEngineManager::sharedManager()->getScriptEngine()->executeSchedule(this, _elapsed);
                        }
                        */
                        Interval = OriginalInterval - (_elapsed - Interval);
                        _elapsed = 0f;
                        if (Interval < 0f)
                        {
                            Interval = OriginalInterval;
                        }
                    }
                }
                else
                {
                    //advanced usage
                    _elapsed += dt;

                    if (_useDelay)
                    {
                        if (_elapsed >= _delay)
                        {
                            if (Selector != null)
                            {
                                Selector(_elapsed);
                            }

                            /*
                            if (m_nScriptHandler != 0)
                            {
                                CCScriptEngineManager::sharedManager()->getScriptEngine()->executeSchedule(this, _elapsed);
                            }
                            */

                            _elapsed = _elapsed - _delay;
                            _timesExecuted += 1;
                            _useDelay = false;
                        }
                    }
                    else
                    {
                        if (_elapsed >= Interval)
                        {
                            if (Selector != null)
                            {
                                Selector(_elapsed);
                            }

                            /*
                            if (m_nScriptHandler)
                            {
                                CCScriptEngineManager::sharedManager()->getScriptEngine()->executeSchedule(m_nScriptHandler, _elapsed);
                            }
                            */

                            Interval = OriginalInterval - (_elapsed - Interval);
                            _elapsed = 0;
                            _timesExecuted += 1;
                        }
                    }

                    if (!_runForever && _timesExecuted > _repeat)
                    {
                        //unschedule timer
                        _scheduler.UnscheduleSelector(Selector, _target);
                    }
                }
            }
        }

        #endregion
    }
}

/** @brief Scheduler is responsible for triggering the scheduled callbacks.
    You should not use NSTimer. Instead use this class.

    There are 2 different types of callbacks (selectors):

    - update selector: the 'update' selector will be called every frame. You can customize the priority.
    - custom selector: A custom selector will be called every frame, or with a custom interval of time

    The 'custom selectors' should be avoided when possible. It is faster, and consumes less memory to use the 'update selector'.
    */

namespace Cocos2D
{
    public class CCScheduler
    {
        public const uint kCCRepeatForever = uint.MaxValue;
        public const int kCCPrioritySystem = int.MinValue;
        public const int kCCPriorityNonSystemMin = kCCPrioritySystem + 1;

        private static HashTimeEntry[] s_pTmpHashSelectorArray = new HashTimeEntry[128];
        private static ICCSelectorProtocol[] s_pTmpSelectorArray = new ICCSelectorProtocol[128];

        private readonly Dictionary<ICCSelectorProtocol, HashTimeEntry> _hashForTimers =
            new Dictionary<ICCSelectorProtocol, HashTimeEntry>();

        private readonly Dictionary<ICCSelectorProtocol, HashUpdateEntry> _hashForUpdates =
            new Dictionary<ICCSelectorProtocol, HashUpdateEntry>();

        // hash used to fetch quickly the list entries for pause,delete,etc
        private readonly LinkedList<ListEntry> _updates0List = new LinkedList<ListEntry>(); // list priority == 0
        private readonly LinkedList<ListEntry> _updatesNegList = new LinkedList<ListEntry>(); // list of priority < 0
        private readonly LinkedList<ListEntry> _updatesPosList = new LinkedList<ListEntry>(); // list priority > 0

        private HashTimeEntry _currentTarget;
        private bool _currentTargetSalvaged;
        private bool _updateHashLocked;

        public float TimeScale = 1.0f;

        public event Action<Exception> OnUnhandledException;

        private void UpdateTarget(ICCSelectorProtocol target, float dt)
        {
            try
            {
                target.Update(dt);
            }
            catch (Exception exception)
            {
                OnUnhandledException?.Invoke(exception);
            }
        }

        internal void update(float dt)
        {
            _updateHashLocked = true;

            try
            {
                if (TimeScale != 1.0f)
                {
                    dt *= TimeScale;
                }

                LinkedListNode<ListEntry> next;

                // Null checks on lists before iterating
                if (_updatesNegList != null)
                {
                    for (LinkedListNode<ListEntry> node = _updatesNegList.First; node != null; node = next)
                    {
                        next = node.Next;
                        if (node.Value != null && !node.Value.Paused && !node.Value.MarkedForDeletion)
                        {
                            UpdateTarget(node.Value.Target, dt);
                        }
                    }
                }

                if (_updates0List != null)
                {
                    for (LinkedListNode<ListEntry> node = _updates0List.First; node != null; node = next)
                    {
                        next = node.Next;
                        if (node.Value != null && !node.Value.Paused && !node.Value.MarkedForDeletion)
                        {
                            UpdateTarget(node.Value.Target, dt);
                        }
                    }
                }

                if (_updatesPosList != null)
                {
                    for (LinkedListNode<ListEntry> node = _updatesPosList.First; node != null; node = next)
                    {
                        next = node.Next;
                        if (node.Value != null && !node.Value.Paused && !node.Value.MarkedForDeletion)
                        {
                            UpdateTarget(node.Value.Target, dt);
                        }
                    }
                }

                // Hash for timers null check
                if (_hashForTimers != null)
                {
                    var count = _hashForTimers.Keys.Count;
                    if (s_pTmpSelectorArray.Length < count)
                    {
                        s_pTmpSelectorArray = new ICCSelectorProtocol[s_pTmpSelectorArray.Length * 2];
                    }
                    _hashForTimers.Keys.CopyTo(s_pTmpSelectorArray, 0);

                    for (int i = 0; i < count; i++)
                    {
                        ICCSelectorProtocol key = s_pTmpSelectorArray[i];
                        if (key != null && _hashForTimers.ContainsKey(key))
                        {
                            HashTimeEntry elt = _hashForTimers[key];
                            _currentTarget = elt;
                            _currentTargetSalvaged = false;

                            if (elt != null && !_currentTarget.Paused)
                            {
                                for (elt.TimerIndex = 0; elt.TimerIndex < elt.Timers?.Count; ++elt.TimerIndex)
                                {
                                    elt.CurrentTimer = elt.Timers[elt.TimerIndex];
                                    if (elt.CurrentTimer != null)
                                    {
                                        elt.CurrentTimerSalvaged = false;
                                        UpdateTarget(elt.CurrentTimer, dt);
                                        elt.CurrentTimer = null;
                                    }
                                }
                            }
                            if (_currentTargetSalvaged && _currentTarget.Timers.Count == 0)
                            {
                                RemoveHashElement(_currentTarget);
                            }
                        }
                    }
                }

                // Delete all updates that are marked for deletion with null checks added on lists
                if (_updatesNegList != null)
                {
                    for (LinkedListNode<ListEntry> node = _updatesNegList.First; node != null; node = next)
                    {
                        next = node.Next;
                        if (node.Value != null && node.Value.MarkedForDeletion)
                        {
                            _updatesNegList.Remove(node);
                            RemoveUpdateFromHash(node.Value);
                        }
                    }
                }

                if (_updates0List != null)
                {
                    for (LinkedListNode<ListEntry> node = _updates0List.First; node != null; node = next)
                    {
                        next = node.Next;
                        if (node.Value != null && node.Value.MarkedForDeletion)
                        {
                            _updates0List.Remove(node);
                            RemoveUpdateFromHash(node.Value);
                        }
                    }
                }

                if (_updatesPosList != null)
                {
                    for (LinkedListNode<ListEntry> node = _updatesPosList.First; node != null; node = next)
                    {
                        next = node.Next;
                        if (node.Value != null && node.Value.MarkedForDeletion)
                        {
                            _updatesPosList.Remove(node);
                            RemoveUpdateFromHash(node.Value);
                        }
                    }
                }
            }
            finally
            {
                // Always reset these fields in the finally block to ensure proper cleanup
                _updateHashLocked = false;
                _currentTarget = null;
            }
        }

        /** The scheduled method will be called every 'interval' seconds.
         If paused is YES, then it won't be called until it is resumed.
         If 'interval' is 0, it will be called every frame, but if so, it's recommended to use 'scheduleUpdateForTarget:' instead.
         If the selector is already scheduled, then only the interval parameter will be updated without re-scheduling it again.
         repeat let the action be repeated repeat + 1 times, use kCCRepeatForever to let the action run continuously
         delay is the amount of time the action will wait before it'll start

         @since v0.99.3, repeat and delay added in v1.1
         */

        public void ScheduleSelector(Action<float> selector, ICCSelectorProtocol target, float interval, uint repeat,
                                     float delay, bool paused)
        {
            Debug.Assert(selector != null);
            Debug.Assert(target != null);

            HashTimeEntry element;

            lock (_hashForTimers)
            {
                if (!_hashForTimers.TryGetValue(target, out element))
                {
                    element = new HashTimeEntry { Target = target };
                    _hashForTimers[target] = element;

                    // Is this the 1st element ? Then set the pause level to all the selectors of this target
                    element.Paused = paused;
                }
                else
                {
                    if (element != null)
                    {
                        Debug.Assert(element.Paused == paused);
                    }
                }
                if (element != null)
                {
                    if (element.Timers == null)
                    {
                        element.Timers = new List<CCTimer>();
                    }
                    else
                    {
                        CCTimer[] timers = element.Timers.ToArray();
                        foreach (var timer in timers)
                        {
                            if (timer == null)
                            {
                                continue;
                            }
                            if (selector == timer.Selector)
                            {
                                CCLog.Log(
                                    "CCSheduler#scheduleSelector. Selector already scheduled. Updating interval from: {0} to {1}",
                                    timer.Interval, interval);
                                timer.Interval = interval;
                                return;
                            }
                        }
                    }

                    element.Timers.Add(new CCTimer(this, target, selector, interval, repeat, delay));
                }
            }
        }

        /** Schedules the 'update' selector for a given target with a given priority.
    	     The 'update' selector will be called every frame.
    	     The lower the priority, the earlier it is called.
    	     @since v0.99.3
    	     */

        public void ScheduleUpdateForTarget(ICCSelectorProtocol targt, int priority, bool paused)
        {
            HashUpdateEntry element;

            if (_hashForUpdates.TryGetValue(targt, out element))
            {
                Debug.Assert(element.Entry.MarkedForDeletion);

                // TODO: check if priority has changed!
                element.Entry.MarkedForDeletion = false;

                return;
            }

            // most of the updates are going to be 0, that's way there
            // is an special list for updates with priority 0
            if (priority == 0)
            {
                AppendIn(_updates0List, targt, paused);
            }
            else if (priority < 0)
            {
                PriorityIn(_updatesNegList, targt, priority, paused);
            }
            else
            {
                PriorityIn(_updatesPosList, targt, priority, paused);
            }
        }

        /** Unschedule a selector for a given target.
    	     If you want to unschedule the "update", use unscheudleUpdateForTarget.
    	     @since v0.99.3
    	     */

        public void UnscheduleSelector(Action<float> selector, ICCSelectorProtocol target)
        {
            // explicity handle nil arguments when removing an object
            if (selector == null || target == null)
            {
                return;
            }

            HashTimeEntry element;
            if (_hashForTimers.TryGetValue(target, out element))
            {
                for (int i = 0; i < element.Timers.Count; i++)
                {
                    var timer = element.Timers[i];
                    if (timer == null)
                    {
                        continue;
                    }
                    if (selector == timer.Selector)
                    {
                        if (timer == element.CurrentTimer && (!element.CurrentTimerSalvaged))
                        {
                            element.CurrentTimerSalvaged = true;
                        }

                        element.Timers.RemoveAt(i);

                        // update timerIndex in case we are in tick:, looping over the actions
                        if (element.TimerIndex >= i)
                        {
                            element.TimerIndex--;
                        }

                        if (element.Timers.Count == 0)
                        {
                            if (_currentTarget == element)
                            {
                                _currentTargetSalvaged = true;
                            }
                            else
                            {
                                RemoveHashElement(element);
                            }
                        }

                        return;
                    }
                }
            }
        }

        /** Unschedules all selectors for a given target.
    	     This also includes the "update" selector.
    	     @since v0.99.3
    	     */

        public void UnscheduleAllForTarget(ICCSelectorProtocol target)
        {
            // explicit NULL handling
            if (target == null)
            {
                return;
            }

            // custom selectors           
            HashTimeEntry element;

            if (_hashForTimers.TryGetValue(target, out element))
            {
                if (element.Timers.Contains(element.CurrentTimer))
                {
                    element.CurrentTimerSalvaged = true;
                }
                element.Timers.Clear();

                if (_currentTarget == element)
                {
                    _currentTargetSalvaged = true;
                }
                else
                {
                    RemoveHashElement(element);
                }
            }

            // update selector
            UnscheduleUpdateForTarget(target);
        }

        /*
        unsigned int CCScheduler::scheduleScriptFunc(unsigned int nHandler, float fInterval, bool bPaused)
        {
            CCSchedulerScriptHandlerEntry* pEntry = CCSchedulerScriptHandlerEntry::create(nHandler, fInterval, bPaused);
            if (!m_pScriptHandlerEntries)
            {
                m_pScriptHandlerEntries = CCArray::create(20);
                m_pScriptHandlerEntries->retain();
            }
            m_pScriptHandlerEntries->addObject(pEntry);
            return pEntry->getEntryId();
        }

        void CCScheduler::unscheduleScriptEntry(unsigned int uScheduleScriptEntryID)
        {
            for (int i = m_pScriptHandlerEntries->count() - 1; i >= 0; i--)
            {
                CCSchedulerScriptHandlerEntry* pEntry = static_cast<CCSchedulerScriptHandlerEntry*>(m_pScriptHandlerEntries->objectAtIndex(i));
                if (pEntry->getEntryId() == uScheduleScriptEntryID)
                {
                    pEntry->markedForDeletion();
                    break;
                }
            }
        }
        */

        public void UnscheduleUpdateForTarget(ICCSelectorProtocol target)
        {
            if (target == null)
            {
                return;
            }

            HashUpdateEntry element;
            if (_hashForUpdates.TryGetValue(target, out element))
            {
                if (_updateHashLocked)
                {
                    element.Entry.MarkedForDeletion = true;
                }
                else
                {
                    RemoveUpdateFromHash(element.Entry);
                }
            }
        }

        public void UnscheduleAll()
        {
            UnscheduleAllWithMinPriority(int.MinValue);
        }

        public void UnscheduleAllWithMinPriority(int minPriority)
        {
            var count = _hashForTimers.Values.Count;
            if (s_pTmpHashSelectorArray.Length < count)
            {
                s_pTmpHashSelectorArray = new HashTimeEntry[s_pTmpHashSelectorArray.Length * 2];
            }

            _hashForTimers.Values.CopyTo(s_pTmpHashSelectorArray, 0);

            for (int i = 0; i < count; i++)
            {
                // Element may be removed in unscheduleAllSelectorsForTarget
                UnscheduleAllForTarget(s_pTmpHashSelectorArray[i].Target);
            }

            // Updates selectors
            if (minPriority < 0 && _updatesNegList.Count > 0)
            {
                LinkedList<ListEntry> copy = new LinkedList<ListEntry>(_updatesNegList);
                foreach (ListEntry entry in copy)
                {
                    if (entry.Priority >= minPriority)
                    {
                        UnscheduleAllForTarget(entry.Target);
                    }
                }
            }

            if (minPriority <= 0 && _updates0List.Count > 0)
            {
                LinkedList<ListEntry> copy = new LinkedList<ListEntry>(_updates0List);
                foreach (ListEntry entry in copy)
                {
                    UnscheduleAllForTarget(entry.Target);
                }
            }

            if (_updatesPosList.Count > 0)
            {
                LinkedList<ListEntry> copy = new LinkedList<ListEntry>(_updatesPosList);
                foreach (ListEntry entry in copy)
                {
                    if (entry.Priority >= minPriority)
                    {
                        UnscheduleAllForTarget(entry.Target);
                    }
                }
            }
        }

        public List<ICCSelectorProtocol> PauseAllTargets()
        {
            return PauseAllTargetsWithMinPriority(int.MinValue);
        }

        public List<ICCSelectorProtocol> PauseAllTargetsWithMinPriority(int minPriority)
        {
            var idsWithSelectors = new List<ICCSelectorProtocol>();

            // Custom Selectors
            foreach (HashTimeEntry element in _hashForTimers.Values)
            {
                element.Paused = true;
                idsWithSelectors.Add(element.Target);
            }

            // Updates selectors
            if (minPriority < 0)
            {
                foreach (ListEntry element in _updatesNegList)
                {
                    if (element.Priority >= minPriority)
                    {
                        element.Paused = true;
                        idsWithSelectors.Add(element.Target);
                    }
                }
            }

            if (minPriority <= 0)
            {
                foreach (ListEntry element in _updates0List)
                {
                    element.Paused = true;
                    idsWithSelectors.Add(element.Target);
                }
            }

            if (minPriority < 0)
            {
                foreach (ListEntry element in _updatesPosList)
                {
                    if (element.Priority >= minPriority)
                    {
                        element.Paused = true;
                        idsWithSelectors.Add(element.Target);
                    }
                }
            }

            return idsWithSelectors;
        }

        public void ResumeTargets(List<ICCSelectorProtocol> targetsToResume)
        {
            foreach (ICCSelectorProtocol target in targetsToResume)
            {
                ResumeTarget(target);
            }
        }

        public void PauseTarget(ICCSelectorProtocol target)
        {
            Debug.Assert(target != null);

            // custom selectors
            HashTimeEntry entry;
            if (_hashForTimers.TryGetValue(target, out entry))
            {
                entry.Paused = true;
            }

            // Update selector
            HashUpdateEntry updateEntry;
            if (_hashForUpdates.TryGetValue(target, out updateEntry))
            {
                updateEntry.Entry.Paused = true;
            }
        }

        public void ResumeTarget(ICCSelectorProtocol target)
        {
            Debug.Assert(target != null);

            // custom selectors
            HashTimeEntry element;
            if (_hashForTimers.TryGetValue(target, out element))
            {
                element.Paused = false;
            }

            // Update selector
            HashUpdateEntry elementUpdate;
            if (_hashForUpdates.TryGetValue(target, out elementUpdate))
            {
                elementUpdate.Entry.Paused = false;
            }
        }

        public bool IsTargetPaused(ICCSelectorProtocol target)
        {
            Debug.Assert(target != null, "target must be non nil");

            // Custom selectors
            HashTimeEntry element;
            if (_hashForTimers.TryGetValue(target, out element))
            {
                return element.Paused;
            }

            // We should check update selectors if target does not have custom selectors
            HashUpdateEntry elementUpdate;
            if (_hashForUpdates.TryGetValue(target, out elementUpdate))
            {
                return elementUpdate.Entry.Paused;
            }

            return false; // should never get here
        }

        private void RemoveHashElement(HashTimeEntry element)
        {
            _hashForTimers.Remove(element.Target);

            element.Timers.Clear();
            element.Target = null;
        }

        private void RemoveUpdateFromHash(ListEntry entry)
        {
            HashUpdateEntry element;
            if (_hashForUpdates.TryGetValue(entry.Target, out element))
            {
                // list entry
                element.List.Remove(entry);
                element.Entry = null;

                // hash entry
                _hashForUpdates.Remove(entry.Target);

                element.Target = null;
            }
        }

        private void PriorityIn(LinkedList<ListEntry> list, ICCSelectorProtocol target, int priority, bool paused)
        {
            var listElement = new ListEntry
                {
                    Target = target,
                    Priority = priority,
                    Paused = paused,
                    MarkedForDeletion = false
                };

            if (list.First == null)
            {
                list.AddFirst(listElement);
            }
            else
            {
                bool added = false;
                for (LinkedListNode<ListEntry> node = list.First; node != null; node = node.Next)
                {
                    if (priority < node.Value.Priority)
                    {
                        list.AddBefore(node, listElement);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    list.AddLast(listElement);
                }
            }

            // update hash entry for quick access
            var hashElement = new HashUpdateEntry
                {
                    Target = target,
                    List = list,
                    Entry = listElement
                };

            _hashForUpdates.Add(target, hashElement);
        }

        private void AppendIn(LinkedList<ListEntry> list, ICCSelectorProtocol target, bool paused)
        {
            var listElement = new ListEntry
                {
                    Target = target,
                    Paused = paused,
                    MarkedForDeletion = false
                };

            list.AddLast(listElement);

            // update hash entry for quicker access
            var hashElement = new HashUpdateEntry
                {
                    Target = target,
                    List = list,
                    Entry = listElement
                };

            _hashForUpdates.Add(target, hashElement);
        }

        #region Nested type: HashSelectorEntry

        private class HashTimeEntry
        {
            public CCTimer CurrentTimer;
            public bool CurrentTimerSalvaged;
            public bool Paused;
            public ICCSelectorProtocol Target;
            public int TimerIndex;
            public List<CCTimer> Timers;
        }

        #endregion

        #region Nested type: HashUpdateEntry

        private class HashUpdateEntry
        {
            public ListEntry Entry; // entry in the list
            public LinkedList<ListEntry> List; // Which list does it belong to ?
            public ICCSelectorProtocol Target; // hash key
        }

        #endregion

        #region Nested type: ListEntry

        private class ListEntry
        {
            public bool MarkedForDeletion;
            public bool Paused;
            public int Priority;
            public ICCSelectorProtocol Target;
        }

        #endregion
    }
}