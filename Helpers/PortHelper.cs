using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XrayUI.Helpers
{
    public static class PortHelper
    {
        /// <summary>
        /// Checks whether the specified port is available for local binding (TCP and UDP).
        /// </summary>
        public static bool IsPortAvailable(int port)
        {
            if (port < 1 || port > 65535) return false;

            try
            {
                var ipGlobal = IPGlobalProperties.GetIPGlobalProperties();

                // Check active TCP listeners
                var tcpListeners = ipGlobal.GetActiveTcpListeners();
                if (tcpListeners.Any(ep => ep.Port == port)) return false;

                // Check active TCP connections
                var tcpConns = ipGlobal.GetActiveTcpConnections();
                if (tcpConns.Any(ep => ep.LocalEndPoint.Port == port)) return false;

                // Check active UDP listeners
                var udpListeners = ipGlobal.GetActiveUdpListeners();
                if (udpListeners.Any(ep => ep.Port == port)) return false;

                // Try binding a test socket to verify OS permission and binding
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a random available port in the specified range (defaults to 10000 - 65000)
        /// that does not conflict with any running service in the system.
        /// </summary>
        public static int GenerateRandomAvailablePort(int min = 10000, int max = 65000)
        {
            if (min < 1024) min = 1024;
            if (max > 65535) max = 65535;

            var random = new Random();
            for (int i = 0; i < 300; i++)
            {
                int candidate = random.Next(min, max);
                if (IsPortAvailable(candidate))
                {
                    return candidate;
                }
            }

            // Fallback default
            return 16891;
        }
    }
}
