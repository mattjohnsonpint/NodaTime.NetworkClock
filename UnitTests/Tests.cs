using System.Diagnostics;
using System.Threading;
using NodaTime;
using Xunit;

namespace UnitTests
{
    public class Tests
    {
        [Fact]
        public void Can_Get_Network_Time()
        {
            var networkNow = NetworkClock.Instance.Now;
            var systemNow = SystemClock.Instance.Now;
            var deltaSeconds = (systemNow - networkNow).ToTimeSpan().TotalSeconds;

            Debug.WriteLine(networkNow);

            // If this fails, the system clock is way off
            Assert.InRange(deltaSeconds, -30, 30);
        }

        [Fact]
        public void Can_Get_Network_Time_Twice_Within_Cache_Period()
        {
            var first = NetworkClock.Instance.Now;
            Debug.WriteLine(first);

            Thread.Sleep(2000);

            var second = NetworkClock.Instance.Now;
            Debug.WriteLine(second);

            var deltaMillis = (second - first).ToTimeSpan().TotalMilliseconds;

            // Thread sleeping is imprecise, so allow up to 500 extra millseconds
            Assert.InRange(deltaMillis, 2000, 2500);
        }
    }
}
