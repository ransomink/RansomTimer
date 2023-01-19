using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ransom
{
    /// <summary>
    /// States representing the current timeline of a Timer.
    /// </summary>
    public enum TimerState
    {
        Inactive,
        Active,
        Suspended,
        Cancelled,
        Completed
    }

    /// <summary>
    /// A set of callbacks invoked during specific events on the Timer. 
    /// </summary>
    public sealed class TimerActions
    {
        #region Events
        public Action OnCompleted = default;
        public Action OnCancelled = default;
        public Action OnSuspended = default;
        public Action OnRestarted = default;
        public Action OnResumed   = default;
        public Action<float> OnUpdated = default;
        #endregion Events

        #region Constructors
        public TimerActions() {}
        
        public TimerActions(
            Action onCompleted = default, 
            Action onCancelled = default, 
            Action onSuspended = default, 
            Action onResumed = default, 
            Action<float> onUpdated = default, 
            Action onRestarted = default)
        {
            Set(onCompleted, onCancelled, onSuspended, onResumed, onUpdated, onRestarted);
        }

        public TimerActions(TimerActions actions)
        {
            Set(actions);
        }
        #endregion Constructors

        #region Methods
        public void Reset() => Set();

        public void Set(
            Action onCompleted = default, 
            Action onCancelled = default, 
            Action onSuspended = default, 
            Action onResumed = default, 
            Action<float> onUpdated = default, 
            Action onRestarted = default)
        {
            OnRestarted = onRestarted;
            OnCompleted = onCompleted;
            OnCancelled = onCancelled;
            OnSuspended = onSuspended;
            OnResumed   = onResumed;
            OnUpdated   = onUpdated;
        }

        public void Set(TimerActions actions) => Set
        (
            actions.OnCompleted,
            actions.OnCancelled,
            actions.OnSuspended,
            actions.OnResumed,
            actions.OnUpdated,
            actions.OnRestarted
        );
        #endregion Methods
    }

    /// <summary>
    /// Trigger an event after a specific interval (delay) of time.
    /// </summary>
    [Serializable]
    public sealed class Timer : IComparable<Timer>
    {
        #region Fields
        private const float Threshold = .01f;
        private static List<Timer> _timers = new List<Timer>(32);

        [Header("SETTINGS")]
        [SerializeField] private bool _canLoop = default;
        [SerializeField] private bool _isCancelled  = default;
        [SerializeField] private bool _isSuspended  = default;
        [SerializeField] private bool _hasReference = default;
        [SerializeField] private bool _useUnscaledTime = default;

        [Header("FIELDS")]
        [SerializeField] private TimerState _state = TimerState.Inactive;
        [ReadOnly]
        [SerializeField] private float _endTime = default;
        [ReadOnly]
        [SerializeField] private float _duration = default;
        [ReadOnly]
        [SerializeField] private float _startTime = default;

        private bool  _isDirty = default;
        private bool  _isSuspendedManually = false;
        private float _suspendedTime = default;
        private MonoBehaviour _behaviour = default;
        #endregion Fields
        
        #region Properties
        public static IReadOnlyList<Timer> Timers => _timers;

        /// <summary>
        /// A set of callbacks for the Timer.
        /// </summary>
        /// <returns>The TimerActions containing each Timer event: OnCompleted, OnCancelled, OnSuspended, OnResumed, OnUpdated.</returns>
        public TimerActions Actions { get; set; } = new TimerActions();

        /// <summary>
        /// The object reference the Timer is attached to.
        /// </summary>
        public MonoBehaviour Behaviour { get => _behaviour; private set => _behaviour = value; }

        /// <summary>
        /// The length of the Timer in seconds.
        /// </summary>
        public float Duration => _duration = (_endTime - _startTime);

        /// <summary>
        /// The expiration of the Timer in seconds.
        /// </summary>
        public float EndTime { get => _endTime; private set => _endTime = value; }

        /// <summary>
        /// Can the Timer repeat after execution?
        /// </summary>
        public bool  HasLoop { get => _canLoop; private set => _canLoop = value; }

        /// <summary>
        /// Does the Timer have a reference to an object?
        /// </summary>
        public bool  HasReference { get => _hasReference; private set => _hasReference = value; }

        /// <summary>
        /// Is the Timer canceled?
        /// </summary>
        public bool  IsCancelled  { get => _isCancelled;  private set => _isCancelled  = value; }

        // <summary>
        // Is the attached object reference destroyed?
        // </summary>
        public bool  IsDestroyed => ((object)_behaviour is null);

        /// <summary>
        /// Is the Timer complete (current time exceeds end time)?
        /// </summary>
        public bool  IsDone 
        { 
            get 
            { 
                if (_isCancelled || _isSuspended) { return false; } 
                if (!_isDirty)  { return (this.Time >= _endTime); } 
                
                return _isDirty;
            } 
            private set { _isDirty = value; } }

        /// <summary>
        /// Is the Timer on hold (stopped execution)?
        /// </summary>
        public bool  IsSuspended { get => _isSuspended; private set => _isSuspended = value; }

        /// <summary>
        /// Was the Timer manually placed on hold (stopped execution)?
        /// </summary>
        public bool  IsSuspendedManually { get => _isSuspendedManually; private set => _isSuspendedManually = value; }
        
        /// <summary>
        /// The normalized time in seconds since the start of the timer (Read Only). Helpful for Lerp methods.
        /// </summary>
        // public float PercentageDone { get { if (!_isCancelled && !_isSuspended) return Mathf.InverseLerp(_startTime, _endTime, this.Time); return Mathf.InverseLerp(_startTime, _endTime, _endTime - _suspendedTime); } }

        /// <summary>
        /// The #PercentageDone with a SmoothStep applied (Read Only).
        /// </summary>
        // public float PercentageDoneSmoothStep => Mathf.SmoothStep(0f, 1f, PercentageDone);

        /// <summary>
        /// The time recorded of the Timer in seconds.
        /// </summary>
        public float StartTime { get => _startTime; private set => _startTime = value; }

        public TimerState State { get => _state; private set => _state = value; }

        /// <summary>
        /// The time in seconds since the start of the application, in scaled or timeScale-independent time, depending on the useUnscaledTime mode (Read Only).
        /// </summary>
        public float Time { get { if (_useUnscaledTime) { return StaticTime.UnscaledTime; } return StaticTime.ScaledTime; } }

        /// <summary>
        /// The time in seconds left until completion of the Timer.
        /// </summary>
        public float TimeRemaining { get { if (_isCancelled || _isSuspended) { return _suspendedTime; } return (_endTime - this.Time); } }

        /// <summary>
        /// Determines whether the application Time in seconds is considered game time (scaled) or  real-time (timeScale-independent: not affected by pause or slow-motion).
        /// </summary>
        public bool  UnscaledTime  { get => _useUnscaledTime; private set => _useUnscaledTime = value; }
        #endregion Properties

        #region Constructors
        /// <summary>
        /// Create a Timer (inactive).
        /// </summary>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(bool isUnscaled = false)
        {
            _state = TimerState.Inactive;
            _useUnscaledTime = isUnscaled;
        }

        /// <summary>
        /// Create a Timer (inactive) set to loop.
        /// </summary>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(bool hasLoop, bool isUnscaled)
        {
            _canLoop = hasLoop;
            _state = TimerState.Inactive;
            _useUnscaledTime = isUnscaled;
        }

        /// <summary>
        /// Create a Timer (active).
        /// </summary>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Default(time, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer with an event callback.
        /// </summary>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(time, action, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer with with event callbacks.
        /// </summary>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the Timer.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(time, timerActions, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create an (inactive) Timer attached to the life cycle of an object.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(MonoBehaviour behaviour, bool isUnscaled = false)
        {
            SetBehaviour(behaviour);
            _state = TimerState.Inactive;
            _useUnscaledTime = isUnscaled;
        }
        

        /// <summary>
        /// Create an (inactive) Timer attached to the life cycle of an object set to loop.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(MonoBehaviour behaviour, bool hasLoop, bool isUnscaled)
        {
            SetBehaviour(behaviour);
            _canLoop = hasLoop;
            _state = TimerState.Inactive;
            _useUnscaledTime = isUnscaled;
        }

        /// <summary>
        /// Create a Timer attached to the life cycle of an object.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(MonoBehaviour behaviour, float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(behaviour, time, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer attached to the life cycle of an object with an event callback.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(MonoBehaviour behaviour, float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Set(behaviour, time, action, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Create a Timer attached to the life cycle of an object with event callbacks.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the Timer.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
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
            _timers.Sort();

            var index = 0;
            var count = _timers.Count;
            var timer = (Timer)default;

            if (count == 0) { return; }

            for (index = 0; index < count; ++index)
            {
                timer = _timers[index];
                if (TimerIsDestroyed()) { continue; }
                if (TimerIsCancelled()) { continue; }
                if (TimerIsSuspended()) { continue; }

                if (timer.HasReference && !timer.Behaviour.enabled)
                {
                    timer.Suspend(false);
                    continue;
                }

                if (!timer.IsDone) { continue; }

                var p = timer.PercentageDone();
                timer.SetState(TimerState.Completed);
                timer.Actions.OnUpdated?.Invoke(p);
                timer.Actions.OnCompleted?.Invoke();
                
                if (!TimerHasLoop()) { return; }
                
                timer.LoopDuration();
            }

            bool TimerHasLoop()
            {
                if (timer.HasLoop) { return true; }
                
                timer = RemoveTimer();
                return false;
            }

            bool TimerIsCancelled()
            {
                if (!timer.IsCancelled) { return false; }
                
                timer.Actions.OnCancelled?.Invoke();
                timer = RemoveTimer();
                return true;
            }

            bool TimerIsDestroyed()
            {
                if (timer is object && (!timer.HasReference || !timer.IsDestroyed)) { return false; }
                
                timer = RemoveTimer();
                return true;
            }

            bool TimerIsSuspended()
            {
                if (!timer.IsSuspended) { return false; }

                // TODO: Suspend timer when its host MonoBehaviour is disabled.
                if (timer.IsSuspendedManually) { return true; }

                // TODO: Resume Timer when its host MonoBehaviour is enabled.
                if (timer.HasReference && timer.Behaviour.enabled) { timer.Resume(); }

                return true;
            }

            /// <summary>
            /// Remove the Timer at the specified index of the List<T>.
            /// </summary>
            /// <param name="index">The zero-based index of the Timer to remove.</param>
            Timer RemoveTimer()
            {
                _timers.RemoveAt(index);
                index--;
                count--;
                return null;
            }
        }
        #endregion Unity Callbacks

        #region Methods
        /// <summary>
        /// Attach an (inactive) Timer to the life cycle of an object reference.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, bool isUnscaled = false)
        {
            return new Timer(behaviour, isUnscaled);
        }
        
        /// <summary>
        /// Attach an (inactive) Timer to the life cycle of an object reference set to loop.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, bool hasLoop, bool isUnscaled)
        {
            return new Timer(behaviour, hasLoop, isUnscaled);
        }
        
        /// <summary>
        /// Attach a Timer to the life cycle of an object reference.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, float time, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(behaviour, time, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Attach a Timer to the life cycle of an object reference with an event callback.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(behaviour, time, action, hasLoop, isUnscaled);
        }
        
        /// <summary>
        /// Attach a Timer to the life cycle of an object reference with event callbacks.
        /// </summary>
        /// <param name="behaviour">The object reference to attach to.</param>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the Timer.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Bind(MonoBehaviour behaviour, float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(behaviour, time, timerActions, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Track the progression of a Timer.
        /// </summary>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Record(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(time, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Track the Timer progression and invoke an event callback.
        /// </summary>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="action">The callback to invoke upon completion.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Record(float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(time, action, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Track the Timer progression and invoke event callbacks.
        /// </summary>
        /// <param name="time">The Timer duration in seconds.</param>
        /// <param name="timerActions">A group of actions to invoke at specific intervals of the Timer.</param>
        /// <param name="hasLoop">Does the Timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the Timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public static Timer Record(float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            return new Timer(time, timerActions, hasLoop, isUnscaled);
        }

        /// <summary>
        /// Stop (cancel) the Timer.
        /// </summary>
        public void Cancel()
        {
            _state = TimerState.Cancelled;
            _isCancelled = true;
            _suspendedTime = TimeRemaining;
            Actions.OnCancelled?.Invoke();
        }

        public int CompareTo(Timer other) => EndTime.CompareTo(other.EndTime);

        /// <summary>
        /// Extend the duration of the Timer.
        /// </summary>
        /// <param name="addDuration">The length of time in seconds to extend.</param>
        public void ExtendDuration(float addDuration) => _endTime += addDuration;

        /// <summary>
        /// Forcibly interrupt and complete the Timer.
        /// </summary>
        public void ForceCompletion() => IsDone = true;

        /// <summary>
        /// Repeat the Timer (use previous settings).
        /// </summary>
        /// <param name="duration">The length of time in seconds.</param>
        public void LoopDuration()
        {
            ResetState();
            _suspendedTime = 0;
            _state      = TimerState.Active;
            _isDirty    = false;
            _startTime += _duration;
            _endTime   += _duration;
            Actions.OnRestarted?.Invoke();
        }

        /// <summary>
        /// Set a duration, effectively starting the Timer.
        /// </summary>
        /// <param name="newDuration">The length of time in seconds.</param>
        public void NewDuration(float newDuration)
        {
            ResetTime();
            ResetState();
            _state     = TimerState.Active;
            _isDirty   = false;
            _startTime = this.Time;
            _duration  = newDuration;
            _endTime   = _startTime + newDuration;
            Actions.OnRestarted?.Invoke();

            if (SO_TimerManager.Contains(this)) { return; }

            AddTimer();
        }

        /// <summary>
        /// The normalized time in seconds since the start of the Timer (Read Only). Helpful for Lerp methods.
        /// </summary>
        public float PercentageDone()
        {
            var  interpolant = this.Time;
            if (_isCancelled || _isSuspended) { interpolant = _endTime - _suspendedTime; }
            return Mathf.InverseLerp(_startTime, _endTime, interpolant);
        }

        /// <summary>
        /// The #PercentageDone with a SmoothStep applied (Read Only).
        /// </summary>
        public float PercentageDoneSmoothStep() => Mathf.SmoothStep(0f, 1f, PercentageDone());

        /// <summary>
        /// Reset the Timer to default (excludes the TimerActions events).
        /// </summary>
        public void Reload()
        {
            ResetTime();
            ResetState();
            _canLoop = false;
            _isDirty = false;
            _useUnscaledTime = false;
        }

        /// <summary>
        /// Reset the Timer to default settings (includes the TimerActions events).
        /// </summary>
        public void Reset()
        {
            Reload();
            Actions.Reset();
            ResetBehaviour();
        }

        /// <summary>
        /// Restart the Timer (use current settings).
        /// </summary>
        public void Restart() => NewDuration(_duration);

        /// <summary>
        /// Continue the Timer.
        /// </summary>
        public void Resume()
        {
            _isSuspended = false;
            _isSuspendedManually = false;
            _endTime = this.Time + _suspendedTime;
            _startTime = _endTime - Duration;
            _state = TimerState.Active;
            Actions.OnResumed?.Invoke();
        }

        /// <summary>
        /// Start the Timer using the supplied duration.
        /// </summary>
        /// <param name="duration">The length of time in seconds.</param>
        public void Start(float duration) => NewDuration(duration);

        /// <summary>
        /// Temporarily halt the Timer.
        /// </summary>
        public void Suspend(bool isManual = true)
        {
            _isSuspended = true;
            _isSuspendedManually = isManual;
            _suspendedTime = TimeRemaining;
            _state = TimerState.Suspended;
            Actions.OnSuspended?.Invoke();
        }

        public void Set(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Default(time, hasLoop, isUnscaled);
        }

        public void Set(float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Actions.Set(action);
            Default(time, hasLoop, isUnscaled);
        }

        public void Set(float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            Actions = timerActions;
            Default(time, hasLoop, isUnscaled);
        }

        public void Set(MonoBehaviour behaviour, float time, bool hasLoop = false, bool isUnscaled = false)
        {
            Reload();
            SetBehaviour(behaviour);
            Default(time, hasLoop, isUnscaled);
        }

        public void Set(MonoBehaviour behaviour, float time, Action action, bool hasLoop = false, bool isUnscaled = false)
        {
            Reset();
            Actions.Set(action);
            SetBehaviour(behaviour);
            Default(time, hasLoop, isUnscaled);
        }

        public void Set(MonoBehaviour behaviour, float time, TimerActions timerActions, bool hasLoop = false, bool isUnscaled = false)
        {
            Reset();
            Actions = timerActions;
            SetBehaviour(behaviour);
            Default(time, hasLoop, isUnscaled);
        }

        public void SetState(TimerState state) => _state = state;

        private void AddTimer() => SO_TimerManager.AddTimer(this);

        // TODO: Remove and scrub the `hasAction` parameter. Every Timer will be added to the internal collection.
        private void Default(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            _useUnscaledTime = isUnscaled;
            _canLoop = hasLoop;
            NewDuration(time);
        }

        private void ResetBehaviour()
        {
            _behaviour = default;
            _hasReference = false;
        }

        private void ResetState()
        {
            _isCancelled = false;
            _isSuspended = false;
            _isSuspendedManually = false;
            _state = TimerState.Inactive;
        }

        private void ResetTime()
        {
            _endTime = 0f;
            _duration = 0f;
            _startTime = 0f;
            _suspendedTime = 0f;
        }

        private void SetBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null) { return; }
            
            _behaviour = behaviour;
            _hasReference = true;
        }
        #endregion Methods    
    }
}
