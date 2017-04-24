using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NodaTime
{
    /// <summary>
    /// Represents a clock that retrieves its time from a NTP server, rather than from the local computer.
    /// </summary>
    public class NetworkClock : IClock
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private Instant _time;

        public static readonly NetworkClock Instance = new NetworkClock();

        private NetworkClock()
        {
            NtpServer = "pool.ntp.org";
            CacheTimeout = Duration.FromMinutes(15);
        }

        /// <summary>
        /// Gets or sets the NTP server to use.  Defaults to "pool.ntp.org"
        /// </summary>
        public string NtpServer { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the cache, which prevents the NTP server from being called too often.
        /// Within the cache period, the time is resolved by taking the value last received, and adding the time elapsed since that call.
        /// Defaults to 15 minutes.
        /// </summary>
        public Duration CacheTimeout { get; set; }

        public Instant GetCurrentInstant()
        {
            var elapsed = Duration.FromTimeSpan(_stopwatch.Elapsed);
            if (_stopwatch.IsRunning && elapsed < CacheTimeout)
            {
                return _time + elapsed;
            }

            _time = GetNetworkTime();
            _stopwatch.Restart();
            return _time;
        }

        private Instant GetNetworkTime()
        {
            // http://stackoverflow.com/a/12150289

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x23; // 0010 0011 : LI = 0 (no warning), VN = 4 (IPv4 or IPv6), Mode = 3 (Client Mode)

            var ipEndPoint = ResolveIPEndPoint();

            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            socket.Connect(ipEndPoint);

            //Stops code hang if NTP is blocked
            socket.ReceiveTimeout = 3000;

            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (long)(intPart * 1000 + fractPart * 1000 / 0x100000000L);
            var instant = Instant.FromUtc(1900, 1, 1, 0, 0) + Duration.FromMilliseconds(milliseconds);

            return instant;
        }

        private IPEndPoint ResolveIPEndPoint()
        {
            const int NTP_PORT = 123;

            var ntpServer = NtpServer;

            IPAddress ipAddress;
            if (IPAddress.TryParse(ntpServer, out ipAddress))
            {
                return new IPEndPoint(ipAddress, NTP_PORT);
            }
            else
            {
                // If it's not already an IPAddress, use DNS to look it up as a host name
                var addresses = Dns.GetHostEntry(ntpServer).AddressList;
                return new IPEndPoint(addresses[0], NTP_PORT);
            }
        }

        private static uint SwapEndianness(ulong x)
        {
            // http://stackoverflow.com/a/3294698

            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
    }
}
