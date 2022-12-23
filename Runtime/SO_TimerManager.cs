using System.Collections.Generic;
using UnityEngine;

namespace Ransom
{
    [CreateAssetMenu(
        fileName = Folder.Name_Timer + Folder.Name_Manager, 
        menuName = Folder.SO + Folder.Base_Manager + Folder.Name_Timer, 
        order    = 0
    )]
    public sealed class SO_TimerManager : SO_Manager
    {
        #region Fields
        private static List<Timer> _timers = new List<Timer>(32);
        private const float Threshold = .01f;
        #endregion Fields
        
        #region Properties
        public static IReadOnlyList<Timer> Timers => _timers;
        #endregion Properties

        #region Unity Callbacks
        public override void OnUpdate()
        {
            int count = _timers.Count;
            if (count == 0) return;

            for (var i = 0; i < count; ++i)
            {
                var timer = _timers[i];
                if (TimerIsDestroyed(timer, ref i)) { continue; }
                if (TimerIsCancelled(timer, ref i)) { continue; }
                if (timer.IsSuspended) { continue; }
                if (!TimerIsDone(timer, ref i)) { continue; }

                var p = timer.PercentageDone();
                timer.Actions.OnUpdated?.Invoke(p);
                timer.Actions.OnCompleted?.Invoke();
                timer.SetState(TimerState.Completed);
                
                if (!TimerHasLoop(timer, ref i)) { return; }
                
                timer.LoopDuration(timer.Duration);
            }

            bool TimerHasLoop(Timer timer, ref int index)
            {
                if (timer.HasLoop) { return true; }
                
                timer = Remove(ref index);
                return false;
            }

            bool TimerIsCancelled(Timer timer, ref int index)
            {
                if (!timer.IsCancelled) { return false; }
                
                timer.Actions.OnCancelled?.Invoke();
                timer = Remove(ref index);
                return true;
            }

            bool TimerIsDestroyed(Timer timer, ref int index)
            {
                if (timer is object && (!timer.HasReference || !timer.IsDestroyed)) { return false; }
                
                timer = Remove(ref index);
                return true;
            }

            bool TimerIsDone(Timer timer, ref int index)
            {
                if (timer.IsDone) { return true; }

                if (TryGetNextTimer(out Timer nextTimer, index))
                {
                    var nextTimerIsActive = nextTimer.State == TimerState.Active;
                    var nextTimerAsyncEndTime = Mathf.Abs(nextTimer.EndTime - timer.EndTime);
                    if (nextTimerIsActive && nextTimer.IsDone && nextTimerAsyncEndTime <= Threshold)
                    {
                        timer.ForceCompletion();
                        return true;
                    }
                }

                var p = timer.PercentageDone();
                timer.Actions.OnUpdated?.Invoke(p);
                return false;
            }

            /// <summary>
            /// Returns the next Timer if one is found.
            /// </summary>
            /// <param name="index">The index of the current (active) timer.</param>
            /// <returns>The next Timer, otherwise null.</returns>
            // Timer GetNextTimer(int index)
            // {
            //     // return (++index < count) ? _timers[index] : null;
            //     if (++index < count) { return _timers[index]; }
            //     return null;
            // }

            /// <summary>
            /// Remove the Timer at the specified index of the List<T>.
            /// </summary>
            /// <param name="index">The zero-based index of the Timer to remove.</param>
            Timer Remove(ref int index)
            {
                var timer = _timers[index];
                _timers.RemoveAt(index--);
                count--;
                return timer;
            }

            /// <summary>
            /// Gets the Timer, if it exists. 
            /// </summary>
            /// <param name="nextTimer">The output argument that will contain the Timer or null.</param>
            /// <param name="index">The index of the current (active) timer.</param>
            /// <returns>Returns true if the Timer is found, false otherwise.</returns>
            bool TryGetNextTimer(out Timer nextTimer, int index)
            {
                // return (++index < count) ? _timers[index] : null;
                if (++index < count)
                {
                    nextTimer = _timers[index];
                    return true;
                }

                nextTimer = null;
                return false;
            }
        }
        #endregion Unity Callbacks

        #region Methods
        public static void Add(Timer timer) => _timers.Add(timer);
        #endregion Methods
    }
}
