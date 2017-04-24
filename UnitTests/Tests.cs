using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
            var networkNow = NetworkClock.Instance.GetCurrentInstant();
            var systemNow = SystemClock.Instance.GetCurrentInstant();
            var deltaSeconds = (systemNow - networkNow).ToTimeSpan().TotalSeconds;

            Debug.WriteLine(networkNow);

            // If this fails, the system clock is way off
            Assert.InRange(deltaSeconds, -30, 30);
        }

        [Fact]
        public void Can_Get_Network_Time_Twice_Within_Cache_Period()
        {
            var first = NetworkClock.Instance.GetCurrentInstant();
            Debug.WriteLine(first);

            Thread.Sleep(2000);

            var second = NetworkClock.Instance.GetCurrentInstant();
            Debug.WriteLine(second);

            var deltaMillis = (second - first).ToTimeSpan().TotalMilliseconds;

            // Thread sleeping is imprecise, so allow up to 500 extra millseconds
            Assert.InRange(deltaMillis, 2000, 2500);
        }

        // This time server runs on both IPv4 and IPv6
        const string NIST_TIME_SERVER = "time.nist.gov";

        [Fact]
        public void Can_Get_Network_Time_From_IPv4_Addresses()
        {
            var ipv4Address = Dns.GetHostEntry(NIST_TIME_SERVER).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);

            var networkNow = QueryTimeWithServer(ipv4Address.ToString());
            var systemNow = SystemClock.Instance.GetCurrentInstant();
            var deltaSeconds = (systemNow - networkNow).ToTimeSpan().TotalSeconds;

            Debug.WriteLine(networkNow);

            // If this fails, the system clock is way off
            Assert.InRange(deltaSeconds, -30, 30);
        }

        [Fact]
        public void Can_Get_Network_Time_From_IPv6_Addresses()
        {
            var ipv6Address = Dns.GetHostEntry(NIST_TIME_SERVER).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetworkV6);

            var networkNow = QueryTimeWithServer("[" + ipv6Address + "]");
            var systemNow = SystemClock.Instance.GetCurrentInstant();
            var deltaSeconds = (systemNow - networkNow).ToTimeSpan().TotalSeconds;

            Debug.WriteLine(networkNow);

            // If this fails, the system clock is way off
            Assert.InRange(deltaSeconds, -30, 30);
        }

        Instant QueryTimeWithServer(string ntpServer)
        {
            string previousServer = NetworkClock.Instance.NtpServer;
            try
            {
                NetworkClock.Instance.NtpServer = ntpServer;
                return NetworkClock.Instance.GetCurrentInstant();
            }
            finally
            {
                // Reset the time server so other tests use the default
                NetworkClock.Instance.NtpServer = previousServer;
            }
        }
    }
}
