using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ransom.Tests
{
    public class Timer_RuntimeTests
    {
        [UnityTest]
        public IEnumerator Timer_ExtendDuration()
        {
            var dur    = 2f;
            var delta  = .005f;
            var time   = Time.time;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur / 2f);

            var stamp  = timer.TimeRemaining;
            timer.ExtendDuration(dur);

            Assert.AreEqual(dur   + dur, timer.Duration, delta);
            Assert.AreEqual(stamp + dur, timer.TimeRemaining, delta);
            Assert.AreEqual(time  + dur + dur, timer.EndTime, delta);
        }

        [UnityTest]
        public IEnumerator Timer_HasResumed()
        {
            var dur    = 2f;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur / 2f);

            var stamp  = timer.TimeRemaining;
            timer.Suspend();

            yield return new WaitForSeconds(3f);

            Assert.That(timer.IsSuspended, Is.True);

            timer.Resume();

            Assert.That(timer.IsSuspended, Is.False);
            Assert.That(Time.time + stamp, Is.EqualTo(timer.EndTime));

            yield return new WaitForSeconds(stamp);

            Assert.That(timer.IsDone, Is.True);
        }
        
        [UnityTest]
        public IEnumerator Timer_IsCancelled()
        {
            var dur    = 2f;
            // var delta  = .005f;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur / 2f);

            var stamp  = timer.TimeRemaining;
            timer.Cancel();

            yield return new WaitForSeconds(stamp);

            Assert.That(timer.IsDone,      Is.False);
            Assert.That(timer.IsCancelled, Is.True);
            Assert.That(timer.IsSuspended, Is.False);
            Assert.That(stamp,             Is.EqualTo(timer.TimeRemaining));
            // Assert.AreEqual(stamp, timer.TimeRemaining, delta);
        }
        
        [UnityTest]
        public IEnumerator Timer_IsComplete()
        {
            var dur    = 2f;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur);

            Assert.That(timer.IsDone, Is.True);
        }
        
        [UnityTest]
        public IEnumerator Timer_IsSuspended()
        {
            var dur    = 2f;
            // var delta  = .005f;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur / 2f);

            var stamp  = timer.TimeRemaining;
            timer.Suspend();

            yield return new WaitForSeconds(stamp);

            Assert.That(timer.IsDone,      Is.False);
            Assert.That(timer.IsCancelled, Is.False);
            Assert.That(timer.IsSuspended, Is.True);
            Assert.That(stamp,             Is.EqualTo(timer.TimeRemaining));
        }

        [UnityTest]
        public IEnumerator Timer_NewDuration()
        {
            var dur    = 2f;
            // var delta  = .005f;
            var time   = Time.time;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur / 2f);

            timer.NewDuration(dur);
            var curTime = Time.time;

            Assert.That(dur,           Is.EqualTo(timer.Duration));
            Assert.That(curTime,       Is.EqualTo(timer.StartTime));
            Assert.That(curTime + dur, Is.EqualTo(timer.EndTime));
            Assert.That(time,          Is.Not.EqualTo(timer.StartTime));
            Assert.That(time + dur,    Is.Not.EqualTo(timer.EndTime));
        }

        [UnityTest]
        public IEnumerator Timer_OnUpdate()
        {
            var isRunning = false;
            var x = Timer.Record(1f, () => Debug.Log("1 second timer completed after 1 second."));
            var y = Timer.Record(.995f, () => Debug.Log("1 second timer completed after .995 seconds."));
            var a = Timer.Record(1f, () => Debug.Log("This is a 1 second timer on a loop."), true);
            var b = Timer.Record(2f, () => Debug.Log("This is a 2 second timer suspended until 4 seconds."));
                    Timer.Record(1f, b.Suspend);
                    Timer.Record(1f, () => Debug.Log($"This is a 2 second timer suspended | Time Remaining: {b.TimeRemaining}"));
                    Timer.Record(1f, () => Assert.That(b.IsSuspended, Is.True, "This is a 2 second timer suspended after 1 second."));
                    Timer.Record(3f, b.Resume);
                    Timer.Record(3f, () => Assert.That(b.IsSuspended, Is.False, "This is a 2 second timer resumed after 3 seconds."));
            var c = Timer.Record(3f, () => Debug.Log("This is a 3 second timer."));
            var d = Timer.Record(4f, () => Debug.Log("This is a 4 second timer cancelled. You should not see this."));
                    Timer.Record(3f, d.Cancel);
                    Timer.Record(3f, () => Assert.That(d.IsCancelled, Is.True));
            var e = Timer.Record(4f, () => Debug.Log($"This is a 4 second timer. You will see this."));
                    Timer.Record(4f, () => Debug.Log($"1 second timer end time: {a.EndTime - a.Duration}"));
                    Timer.Record(4f, () => Debug.Log($"2 second timer end time: {b.EndTime}"));
                    Timer.Record(4f, () => Debug.Log($"4 second timer end time: {e.EndTime}"));
            var f = Timer.Record(5f, () => isRunning = false);
            isRunning = true;

            while (isRunning)
            {
                Timer.OnUpdate();
                yield return null;
            }

            Assert.That(Timer.Timers.Count, Is.GreaterThan(0));
            Assert.That(Timer.Timers.Count, Is.EqualTo(1));
            Assert.That(b.IsDone, Is.True);
            Assert.That(c.IsDone, Is.True);
            Assert.That(d.IsDone, Is.False);
            Assert.That(e.IsDone, Is.True);
        }
        
        [UnityTest]
        public IEnumerator Timer_PercentageDone()
        {
            var dur    = 2f;
            var delta  = .005f;
            var time   = Time.time;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur / 2f);

            Assert.AreEqual(0.5f, timer.PercentageDone(), delta);
        }
        
        [UnityTest]
        public IEnumerator Timer_TimeRemaining()
        {
            var dur    = 2f;
            var delta  = .0055f;
            var timer  = new Timer(dur);
            yield return new WaitForSeconds(dur / 2f);

            Assert.AreEqual(dur / 2f, timer.TimeRemaining, delta);
        }
    }
}
