using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ransom
{
    /// <summary>
    /// States representing the current timeline of a timer.
    /// </summary>
    public enum TimerStates
    {
        Inactive,
        Active,
        Suspended,
        Cancelled,
        Completed
    }

    /// <summary>
    /// A set of callbacks invoked during specific events on the timer. 
    /// </summary>
    public class TimerActions
    {
        #region Events
        public Action OnCompleted = default;
        public Action OnCancelled = default;
        public Action OnSuspended = default;
        public Action OnResumed   = default;
        public Action<float> OnUpdated = default;
        #endregion Events

        #region Constructors
        public TimerActions() {}
        
        public TimerActions(Action onCompleted = default, Action onCancelled = default, Action onSuspended = default, Action onResumed = default, Action<float> onUpdated = default)
        {
            Set(onCompleted, onCancelled, onSuspended, onResumed, onUpdated);
        }

        public TimerActions(TimerActions actions)
        {
            Set(actions);
        }
        #endregion Constructors

        #region Methods
        public void Set(Action onCompleted = default, Action onCancelled = default, Action onSuspended = default, Action onResumed = default, Action<float> onUpdated = default)
        {
            OnCompleted = onCompleted;
            OnCancelled = onCancelled;
            OnSuspended = onSuspended;
            OnResumed   = onResumed;
            OnUpdated   = onUpdated;
        }

        public void Set(TimerActions actions)
        {
            OnCompleted = actions.OnCompleted;
            OnCancelled = actions.OnCancelled;
            OnSuspended = actions.OnSuspended;
            OnResumed   = actions.OnResumed;
            OnUpdated   = actions.OnUpdated;
        }
        #endregion Methods
    }

    /// <summary>
    /// Trigger an event after a specific interval (delay) of time.
    /// </summary>
    [Serializable]
    public class Timer
    {
        #region Fields
        private static List<Timer> _timers = new List<Timer>(32);
        private const float Threshold = .01f;

        [Header("SETTINGS")]
        [SerializeField] private bool  _canLoop = default;
        [SerializeField] private bool  _isCancelled = default;
        [SerializeField] private bool  _isSuspended = default;
        [SerializeField] private bool  _hasReference = default;
        [SerializeField] private bool  _useUnscaledTime = default;

        [Header("FIELDS")]
        [SerializeField] private TimerStates _state = TimerStates.Inactive;
        [ReadOnly]
        [SerializeField] private float _endTime = default;
        [ReadOnly]
        [SerializeField] private float _duration = default;
        [ReadOnly]
        [SerializeField] private float _startTime = default;
        // [SerializeField] private TimerActions _timerActions = new TimerActions();

        private bool  _isDirty = default;
        private float _suspendedTime = default;
        private MonoBehaviour _behaviour = default;
        #endregion Fields
        
        #region Properties
        public static IReadOnlyList<Timer> Timers => _timers;

        /// <summary>
        /// A set of callbacks for the timer.
        /// </summary>
        /// <value>OnCompleted, OnCancelled, OnSuspended, OnResumed, OnUpdated.</value>
        public TimerActions Actions { get; set; } = new TimerActions();

        /// <summary>
        /// The object reference this timer is attached to.
        /// </summary>
        public MonoBehaviour Behaviour { get => _behaviour; private set => _behaviour = value; }

        /// <summary>
        /// The length of the timer in seconds.
        /// </summary>
        public float Duration => _duration = _endTime - _startTime;

        /// <summary>
        /// The expiration of the timer in seconds.
        /// </summary>
        public float EndTime { get => _endTime; private set => _endTime = value; }

        /// <summary>
        /// If the timer can repeat after execution.
        /// </summary>
        public bool  HasLoop { get => _canLoop; private set => _canLoop = value; }

        /// <summary>
        /// Does the timer have a reference to an object?
        /// </summary>
        public bool  HasReference { get => _hasReference; private set => _hasReference = value; }

        /// <summary>
        /// Is the timer canceled?
        /// </summary>
        public bool  IsCancelled  { get => _isCancelled;  private set => _isCancelled  = value; }

        /// <summary>
        /// Is the timer complete (current time exceeds end time)?
        /// </summary>
        public bool  IsDone { get { if (_isCancelled || _isSuspended) return false; if (!_isDirty) return this.Time >= _endTime; else return _isDirty; } private set { _isDirty = value; } }

        /// <summary>
        /// Is the timer on hold (stopped execution)?
        /// </summary>
        public bool  IsSuspended { get => _isSuspended; private set => _isSuspended = value; }

        /// <summary>
        /// The normalized time in seconds since the start of the timer (Read Only). Helpful for Lerp methods.
        /// </summary>
        // public float PercentageDone { get { if (!_isCancelled && !_isSuspended) return Mathf.InverseLerp(_startTime, _endTime, this.Time); return Mathf.InverseLerp(_startTime, _endTime, _endTime - _suspendedTime); } }

        /// <summary>
        /// The #PercentageDone with a SmoothStep applied (Read Only).
        /// </summary>
        // public float PercentageDoneSmoothStep => Mathf.SmoothStep(0f, 1f, PercentageDone);

        /// <summary>
        /// The time recorded of the timer in seconds.
        /// </summary>
        public float StartTime { get => _startTime; private set => _startTime = value; }

        public TimerStates State { get => _state; private set => _state = value; }

        /// <summary>
        /// The time in seconds since the start of the application, in scaled or timeScale-independent time, dependending on the useUnscaledTime mode (Read Only).
        /// </summary>
        public float Time { get { if (_useUnscaledTime) return Ransom.StaticTime.UnscaledTime; else return Ransom.StaticTime.ScaledTime; } }

        /// <summary>
        /// The time in seconds left until completion of the timer.
        /// </summary>
        public float TimeRemaining { get { if (_isCancelled || _isSuspended) return _suspendedTime; return _endTime - this.Time; } }

        /// <summary>
        /// Determines whether the application #Time in seconds is considered game time (scaled) or  real-time (timeScale-independent: not affected by pause or slow-motion).
        /// </summary>
        public bool  UnscaledTime  { get => _useUnscaledTime; private set => _useUnscaledTime = value; }
        #endregion Properties

        #region Constructors
        /// <summary>
        /// Create a Timer instance (inactive).
        /// </summary>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(bool isUnscaled = false)
        {
            _state = TimerStates.Inactive;
            _useUnscaledTime = isUnscaled;
        }

        /// <summary>
        /// Create an Timer instance (active).
        /// </summary>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Default(time, true, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer instance with an event callback.
        /// </summary>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(time, action, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer instance with TimerActions to invoke.
        /// </summary>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the timer.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(time, timerActions, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer instance attached to the life cycle of an object with an event callback.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(MonoBehaviour behaviour, float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(behaviour, time, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer instance attached to the life cycle of an object with an event callback.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(MonoBehaviour behaviour, float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(behaviour, time, action, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer instance attached to the life cycle of an object.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the timer.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(MonoBehaviour behaviour, float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(behaviour, time, timerActions, hasLoop, isUnscaled);
        }
        #endregion Constructors

        #region Unity Callbacks
        /// <summary>
        /// Lifecycle method to manage recorded timers.
        /// Must be called from an MonoBehaviour Update method.
        /// (Alternatively, subscribe it to an OnUpdate event.)
        /// </summary>
        public static void OnUpdate()
        {
            int count = _timers.Count;
            if (count == 0) return;

            for (var i = 0; i < count; ++i)
            {
                var timer = _timers[i];
                if (timer.IsCancelled)
                {
                    timer.Actions.OnCancelled?.Invoke();
                    timer = Remove(ref i);
                    continue;
                }
                
                if (timer.HasReference && timer.IsDestroyed())
                {
                    timer = Remove(ref i);
                    continue;
                }

                if ( timer.IsSuspended) continue;
                if (!timer.IsDone)
                {
                    var nextTimer = GetNextTimer(i);
                    if (!(nextTimer is null) && nextTimer.IsDone && Mathf.Abs(nextTimer.EndTime - timer.EndTime) <= Threshold) timer.ForceCompletion();
                    else
                    {
                        timer.Actions.OnUpdated?.Invoke(timer.PercentageDone());
                        continue;
                    }
                }

                timer.Actions.OnUpdated?.Invoke(timer.PercentageDone());
                timer.State = TimerStates.Completed;
                
                if (!timer.HasLoop)
                {
                    timer = Remove(ref i);
                    timer.Actions.OnCompleted?.Invoke();
                }
                else
                {
                    timer.Actions.OnCompleted?.Invoke();
                    timer.LoopDuration(timer.Duration);
                }
            }

            // <summary>
            // Returns the next timer, if one is found.
            // </summary>
            // <param name="index">The index of the current (active) timer.</param>
            Timer GetNextTimer(int index)
            {
                index++;
                return (index < count) ? _timers[index] : null;
            }

            // <summary>
            // Remove a timer at the specified index.
            // </summary>
            // <param name="index">The index of the timer.</param>
            Timer Remove(ref int index)
            {
                var timer = _timers[index];
                _timers.RemoveAt(index--);
                count--;
                return timer /* = null */;
            }
        }
        #endregion Unity Callbacks

        #region Methods
        /// <summary>
        /// Attach a timer to the life cycle of an object reference.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, float time, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(behaviour, time, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Attach a timer to the life cycle of an object reference.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(behaviour, time, action, hasLoop, isUnscaled);
        }
        
        /// <summary>
        /// Attach a timer to the life cycle of an object reference.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the timer.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(behaviour, time, timerActions, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Track the timer progression and invoke an action after its completion.
        /// </summary>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Record(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(time, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Track the timer progression and invoke an action after its completion.
        /// </summary>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Record(float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(time, action, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Track the timer progression and invoke an action after its completion.
        /// </summary>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the timer.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Record(float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(time, timerActions, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Stop (cancel) the timer.
        /// </summary>
        public void Cancel()
        {
            _state = TimerStates.Cancelled;
            _suspendedTime = TimeRemaining;
            _isCancelled   = true;
        }

        /// <summary>
        /// Extend the timer, if needed.
        /// </summary>
        /// <param name="addDuration">The extended length of time in seconds.</param>
        public void ExtendDuration(float addDuration) => _endTime += addDuration;

        public void ForceCompletion() => IsDone = true;

        // <summary>
        // Is the attached object reference destroyed?
        // </summary>
        public bool IsDestroyed() => _behaviour is null;

        // <summary>
        // Repeat the timer by assigning a new start and end time.
        // </summary>
        // <param name="newDuration">A length of time in seconds.</param>
        public void LoopDuration(float newDuration)
        {
            ResetState();
            _isDirty    = false;
            _startTime += newDuration;
            _duration   = newDuration;
            _endTime   += newDuration;
        }

        /// <summary>
        /// Set a new duration, effectively resetting the timer.
        /// </summary>
        /// <param name="newDuration">A length of time in seconds.</param>
        public void NewDuration(float newDuration)
        {
            ResetState();
            _isDirty   = false;
            _startTime = this.Time;
            _duration  = newDuration;
            _endTime   = _startTime + newDuration;
        }

        /// <summary>
        /// The normalized time in seconds since the start of the timer (Read Only). Helpful for Lerp methods.
        /// </summary>
        public float PercentageDone()
        {
            if (!_isCancelled && !_isSuspended) return Mathf.InverseLerp(_startTime, _endTime, this.Time);
            return Mathf.InverseLerp(_startTime, _endTime, _endTime - _suspendedTime);
        }

        /// <summary>
        /// The #PercentageDone with a SmoothStep applied (Read Only).
        /// </summary>
        public float PercentageDoneSmoothStep() => Mathf.SmoothStep(0f, 1f, PercentageDone());

        /// <summary>
        /// Reload the timer to default.
        /// </summary>
        public void Reload()
        {
            ResetTime();
            ResetState();
            ResetBehaviour();
            _canLoop         = false;
            _isDirty         = false;
            _useUnscaledTime = false;
            // if (hasActions) _timerActions.Set();
        }

        /// <summary>
        /// Reset the timer to default settings.
        /// </summary>
        public void Reset()
        {
            ResetTime();
            ResetState();
            ResetBehaviour();
            _canLoop         = false;
            _isDirty         = false;
            _useUnscaledTime = false;
            Actions.Set();
        }

        /// <summary>
        /// Continue the timer.
        /// </summary>
        public void Resume()
        {
            _isSuspended = false;
            _state       = TimerStates.Active;
            _endTime     = this.Time + _suspendedTime;
            _startTime   = _endTime  - Duration;
            Actions.OnResumed?.Invoke();
        }

        /// <summary>
        /// Temporarily halt the timer.
        /// </summary>
        public void Suspend()
        {
            _isSuspended   = true;
            _suspendedTime = TimeRemaining;
            _state = TimerStates.Suspended;
            Actions.OnSuspended?.Invoke();
        }

        public void Set(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Default(time, true, hasLoop, isUnscaled);
        }

        public void Set(float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Actions.Set(action);
            Default(time, true, hasLoop, isUnscaled);
        }

        public void Set(float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Actions = timerActions;
            Default(time, true, hasLoop, isUnscaled);
        }

        public void Set(MonoBehaviour behaviour, float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            SetBehaviour(behaviour);
            Default(time, true, hasLoop, isUnscaled);
        }

        public void Set(MonoBehaviour behaviour, float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Actions.Set(action);
            SetBehaviour(behaviour);
            Default(time, true, hasLoop, isUnscaled);
        }

        public void Set(MonoBehaviour behaviour, float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Actions = timerActions;
            SetBehaviour(behaviour);
            Default(time, true, hasLoop, isUnscaled);
        }

        private void AddTimer() => SO_TimerManager.Add(this);

        private void Default(float time, bool hasAction = false, bool hasLoop = false, bool isUnscaled = false)
        {
            _useUnscaledTime = isUnscaled;
            _state   = TimerStates.Active;
            _canLoop = hasLoop;
            NewDuration(time);
            if (hasAction) AddTimer();
        }

        private void ResetBehaviour()
        {
            _behaviour    = default;
            _hasReference = false;
        }

        private void ResetState()
        {
            _isCancelled = false;
            _isSuspended = false;
            _state = TimerStates.Inactive;
        }

        private void ResetTime()
        {
            _endTime       = 0f;
            _duration      = 0f;
            _startTime     = 0f;
            _suspendedTime = 0f;
        }

        private void SetBehaviour(MonoBehaviour behaviour)
        {
            _hasReference = true;
            _behaviour = behaviour;
        }
        #endregion Methods    
    }
}
