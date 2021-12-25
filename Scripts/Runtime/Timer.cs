using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ransom
{
    /// <summary>
    /// A set of callbacks invoked during specific events on the timer. 
    /// </summary>
    public struct TimerActions
    {
        #region Events
        public Action OnComplete;
        public Action OnCancelled;
        public Action OnSuspended;
        public Action OnResumed;
        public Action<float> OnUpdated;
        #endregion Events

        public TimerActions(Action onComplete = default, Action onCancelled = default, Action onSuspended = default, Action onResumed = default, Action<float> onUpdated = default)
        {
            OnComplete  = onComplete;
            OnCancelled = onCancelled;
            OnSuspended = onSuspended;
            OnResumed   = onResumed;
            OnUpdated   = onUpdated;
        }
    }

    /// <summary>
    /// Trigger an event after a specific interval (delay) of time.
    /// </summary>
    [Serializable]
    public class Timer
    {
        #region Fields
        private static List<(Timer Timer, Action Action)> _timers = new List<(Timer, Action)>();
        private const float _threshold = .01f;
        [SerializeField] private bool  _canLoop;
        [SerializeField] private bool  _isCancelled;
        [SerializeField] private bool  _isSuspended;
        [SerializeField] private bool  _hasReference;
        [SerializeField] private bool  _useUnscaledTime;
        [ReadOnly]
        [SerializeField] private float _endTime;
        [ReadOnly]
        [SerializeField] private float _duration;
        [SerializeField] private float _startTime;
        [SerializeField] private TimerActions _timerActions;

        private bool  _isDirty;
        private float _suspendedTime;
        private MonoBehaviour _behaviour;
        #endregion Fields
        
        #region Properties
        public static List<(Timer Timer, Action Action)> Timers { get => _timers; private set => _timers = value; }

        /// <summary>
        /// A set of callbacks for the timer.
        /// </summary>
        /// <value>OnComplete, OnCancelled, OnSuspended, OnResumed, OnUpdate.</value>
        public TimerActions Actions { get => _timerActions; private set => _timerActions = value; }

        /// <summary>
        /// The object reference this timer is attached to.
        /// </summary>
        public MonoBehaviour Behaviour { get => _behaviour; private set => _behaviour = value; }

        /// <summary>
        /// The length of the timer in seconds.
        /// </summary>
        public float Duration => _duration = EndTime - StartTime;

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
        public bool  IsDone { get { if (IsCancelled || IsSuspended) return false; if (!_isDirty) return this.Time >= EndTime; else return _isDirty; } private set { _isDirty = value; } }

        /// <summary>
        /// Is the timer on hold (stopped execution)?
        /// </summary>
        public bool  IsSuspended { get => _isSuspended; private set => _isSuspended = value; }

        /// <summary>
        /// The normalized time in seconds since the start of the timer (Read Only). Helpful for Lerp methods.
        /// </summary>
        public float PercentageDone { get { if (!IsCancelled && !IsSuspended) return Mathf.InverseLerp(_startTime, _endTime, this.Time); return Mathf.InverseLerp(_startTime, _endTime, _endTime - _suspendedTime); } }

        /// <summary>
        /// The #PercentageDone with a SmoothStep applied (Read Only).
        /// </summary>
        public float PercentageDoneSmoothStep => Mathf.SmoothStep(0f, 1f, PercentageDone);

        /// <summary>
        /// The time recorded of the timer in seconds.
        /// </summary>
        public float StartTime { get => _startTime; private set => _startTime = value; }

        /// <summary>
        /// The time in seconds since the start of the application, in scaled or timeScale-independent time, dependending on the useUnscaledTime mode (Read Only).
        /// </summary>
        public float Time => _useUnscaledTime ? Ransom.StaticTime.UnscaledTime : Ransom.StaticTime.ScaledTime;

        /// <summary>
        /// The time in seconds left until completion of the timer.
        /// </summary>
        public float TimeRemaining { get { if (IsCancelled || IsSuspended) return _suspendedTime; return EndTime - this.Time; } }

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
        public Timer(bool isUnscaled = false) => _useUnscaledTime = isUnscaled;

        /// <summary>
        /// Create an Timer instance (active).
        /// </summary>
        /// <param name="time">The timer duration in seconds.</param>
        /// <param name="hasLoop">Does the timer repeat after execution?</param>
        /// <param name="isUnscaled">Is the timer affected by scale (game time) or timeScale-independent (real-time: not affected by pause or slow motion)?</param>
        public Timer(float time, bool hasLoop = false, bool isUnscaled = false)
        {
            _useUnscaledTime = isUnscaled;
            _canLoop = hasLoop;
            NewDuration(time);
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
            _timerActions  = timerActions;
            _canLoop = hasLoop;
            _useUnscaledTime = isUnscaled;
            NewDuration(time);
            _timers.Add((this, Actions.OnComplete));
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
            _timerActions  = new TimerActions(action);
            _canLoop = hasLoop;
            _useUnscaledTime = isUnscaled;
            NewDuration(time);
            _timers.Add((this, Actions.OnComplete));
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
            _hasReference = true;
            _behaviour    = behaviour;
            _timerActions = timerActions;
            _canLoop = hasLoop;
            _useUnscaledTime = isUnscaled;
            NewDuration(time);
            _timers.Add((this, Actions.OnComplete));
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
            _hasReference = true;
            _behaviour    = behaviour;
            _timerActions = new TimerActions(action);
            _canLoop = hasLoop;
            _useUnscaledTime = isUnscaled;
            NewDuration(time);
            _timers.Add((this, Actions.OnComplete));
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
            if (_timers.Count == 0) return;

            for (var i = 0; i < _timers.Count; i++)
            {
                var (Timer, OnComplete) = _timers[i];
                if  (Timer.IsCancelled)
                {
                    Timer.Actions.OnCancelled?.Invoke();
                    Timer = Remove(ref i);
                    continue;
                }
                
                if  (Timer.HasReference && Timer.IsDestroyed())
                {
                    Timer = Remove(ref i);
                    continue;
                }

                if  ( Timer.IsSuspended) continue;
                if  (!Timer.IsDone)
                {
                    var timer = GetNextTimer(i);
                    if (timer is null)
                    {
                        Timer.Actions.OnUpdated?.Invoke(Timer.PercentageDone);
                        continue;
                    }
                    
                    if (timer.IsDone && Mathf.Abs(timer.EndTime - Timer.EndTime) <= _threshold) Timer.ForceCompletion();
                    else
                    {
                        Timer.Actions.OnUpdated?.Invoke(Timer.PercentageDone);
                        continue;
                    }
                }

                // OnComplete?.Invoke();
                Timer.Actions.OnComplete?.Invoke();
                if (!Timer.HasLoop) Timer = Remove(ref i);
                else Timer.LoopDuration(Timer.Duration);
            }

            // <summary>
            // Returns the next timer, if one is found.
            // </summary>
            // <param name="index">The index of the current (active) timer.</param>
            Timer GetNextTimer(int index)
            {
                index++;
                return (index < _timers.Count) ? _timers[index].Timer : null;
            }

            // <summary>
            // Remove a timer at the specified index.
            // </summary>
            // <param name="index">The index of the timer.</param>
            Timer Remove(ref int index)
            {
                var timer = _timers[index].Timer;
                _timers.RemoveAt(index--);
                return timer = null;
            }
        }
        #endregion Unity Callbacks

        #region Methods
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
        /// Stop (cancel) the timer.
        /// </summary>
        public void Cancel()
        {
            _suspendedTime = TimeRemaining;
            IsCancelled    = true;
        }

        /// <summary>
        /// Extend the timer, if needed.
        /// </summary>
        /// <param name="addDuration">The extended length of time in seconds.</param>
        public void ExtendDuration(float addDuration) => EndTime += addDuration;

        public void ForceCompletion() => IsDone = true;

        /// <summary>
        /// Repeat the timer by assigning a new start and end time.
        /// </summary>
        /// <param name="newDuration">A length of time in seconds.</param>
        public void LoopDuration(float newDuration)
        {
            _isDirty   = false;
            StartTime += newDuration;
            EndTime   += newDuration;
            // NewDuration(newDuration + TimeRemaining);
            // var extendedTime = TimeRemaining;
            // NewDuration(newDuration);
            // ExtendDuration(extendedTime);
        }

        /// <summary>
        /// Set a new duration, effectively resetting the timer.
        /// </summary>
        /// <param name="newDuration">A length of time in seconds.</param>
        public void NewDuration(float newDuration)
        {
            _isDirty  = false;
            StartTime = this.Time;
            EndTime   = _startTime + newDuration;
        }

        /// <summary>
        /// Reset the timer to default settings.
        /// </summary>
        public void Reset()
        {
            _canLoop         = false;
            _isCancelled     = false;
            _isSuspended     = false;
            _hasReference    = false;
            _useUnscaledTime = false;
            _endTime         = 0f;
            _duration        = 0f;
            _startTime       = 0f;
            _behaviour       = default;
            _suspendedTime   = 0f;
            _isDirty         = false;
            _timerActions    = new TimerActions();
        }

        /// <summary>
        /// Continue the timer.
        /// </summary>
        public void Resume()
        {
            EndTime     = this.Time + _suspendedTime;
            StartTime   = EndTime - (Duration - _suspendedTime);
            IsSuspended = false;
            Actions.OnResumed?.Invoke();
        }

        /// <summary>
        /// Temporarily halt the timer.
        /// </summary>
        public void Suspend()
        {
            _suspendedTime = TimeRemaining;
            IsSuspended    = true;
            Actions.OnSuspended?.Invoke();
        }

        // <summary>
        // Is the attached object reference destroyed?
        // </summary>
        private bool IsDestroyed() => !ReferenceEquals(_behaviour, null) && _behaviour == null;
        #endregion Methods    
    }
}
