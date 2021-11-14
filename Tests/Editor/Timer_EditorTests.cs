using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ransom.Editor.Tests
{
    public class Timer_EditorTests
    {
        [Test]
        [TestCase(2)]
        public void Timer(float dur)
        {
            // var dur   = 2f;
            var time  = Time.time;
            var timer = new Timer(dur);
            Assert.That(timer.Behaviour, Is.Null);
            Assert.That(dur, Is.EqualTo(timer.Duration));
            Assert.That(time, Is.EqualTo(timer.StartTime));
            Assert.That(time + dur, Is.EqualTo(timer.EndTime));
        }
    }
}
