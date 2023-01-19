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
        #endregion Fields
        
        #region Properties
        public static IReadOnlyList<Timer> Timers => _timers;
        #endregion Properties

        #region Unity Callbacks
        public override void OnUpdate()
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
        public static void AddTimer(Timer timer) => _timers.Add(timer);

        public static bool Contains(Timer timer) => _timers.Contains(timer);
        #endregion Methods
    }
}
