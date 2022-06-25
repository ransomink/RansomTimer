using System.Collections.Generic;
using UnityEngine;

namespace Ransom
{
    [CreateAssetMenu(
        fileName = Folder.NAME_TIMER + Folder.NAME_MANAGER, 
        menuName = Folder.SO + Folder.BASE_MANAGER + Folder.NAME_TIMER, 
        order    = 0
    )]
    public class SO_TimerManager : Manager
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

                timer.Actions.OnCompleted?.Invoke();
                if (!timer.HasLoop) timer = Remove(ref i);
                else timer.LoopDuration(timer.Duration);
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
                return timer = null;
            }
        }
        #endregion Unity Callbacks

        #region Methods
        public static void Add(Timer timer) => _timers.Add(timer);
        #endregion Methods
    }
}
