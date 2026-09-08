using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace XrayUI.Helpers
{
    public static class PortHelper
    {
        private const uint ErrorInsufficientBuffer = 122;
        private const int AddressFamilyInternet = 2;
        private const int TcpTableOwnerPidListener = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpRowOwnerPid
        {
            public uint State;
            public uint LocalAddress;
            public uint LocalPort;
            public uint RemoteAddress;
            public uint RemotePort;
            public uint OwningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr tcpTable,
            ref int size,
            [MarshalAs(UnmanagedType.Bool)] bool order,
            int ipVersion,
            int tableClass,
            uint reserved);

        /// <summary>
        /// Returns the processes that currently own an IPv4 TCP listener on a local port.
        /// This is intentionally best effort: an unavailable owner is safer than guessing
        /// and never grants permission to terminate an unknown process.
        /// </summary>
        public static IReadOnlyCollection<int> GetTcpListenerProcessIds(int port)
        {
            if (port < 1 || port > 65535) return Array.Empty<int>();

            IntPtr buffer = IntPtr.Zero;
            try
            {
                var size = 0;
                var result = GetExtendedTcpTable(
                    IntPtr.Zero, ref size, true, AddressFamilyInternet, TcpTableOwnerPidListener, 0);
                if (result != ErrorInsufficientBuffer || size <= sizeof(int))
                {
                    return Array.Empty<int>();
                }

                buffer = Marshal.AllocHGlobal(size);
                result = GetExtendedTcpTable(
                    buffer, ref size, true, AddressFamilyInternet, TcpTableOwnerPidListener, 0);
                if (result != 0)
                {
                    return Array.Empty<int>();
                }

                var count = Marshal.ReadInt32(buffer);
                var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
                var maximumRows = (size - sizeof(int)) / rowSize;
                if (count < 0 || count > maximumRows)
                {
                    return Array.Empty<int>();
                }
                var processIds = new HashSet<int>();
                var rowAddress = IntPtr.Add(buffer, sizeof(int));
                for (var index = 0; index < count; index++)
                {
                    var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowAddress);
                    if (FromNetworkOrderPort(row.LocalPort) == port && row.OwningPid <= int.MaxValue)
                    {
                        processIds.Add((int)row.OwningPid);
                    }

                    rowAddress = IntPtr.Add(rowAddress, rowSize);
                }

                return processIds;
            }
            catch
            {
                return Array.Empty<int>();
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        /// <summary>
        /// Compares two file paths after normalisation. Callers still need an independent
        /// ownership signal (for example, the target listener port) before acting on a match.
        /// </summary>
        public static bool PathsAreEqual(string? candidatePath, string? expectedPath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(expectedPath))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Path.GetFullPath(candidatePath),
                    Path.GetFullPath(expectedPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static int FromNetworkOrderPort(uint port)
            => (ushort)IPAddress.NetworkToHostOrder((short)(port & 0xffff));

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

                // Check active TCP connections (ignore closing/time-wait connections since
                // modern proxy listeners set SO_REUSEADDR and can bind immediately)
                var tcpConns = ipGlobal.GetActiveTcpConnections();
                if (tcpConns.Any(ep => ep.LocalEndPoint.Port == port &&
                                       ep.State != TcpState.TimeWait &&
                                       ep.State != TcpState.CloseWait &&
                                       ep.State != TcpState.Closed))
                {
                    return false;
                }

                // Check active UDP listeners
                var udpListeners = ipGlobal.GetActiveUdpListeners();
                if (udpListeners.Any(ep => ep.Port == port)) return false;

                // Try binding a test socket with SO_REUSEADDR to verify OS permission and binding
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for a local port to become bindable. This is deliberately bounded: it covers
        /// the short kernel cleanup window after Xray exits without concealing a real conflict
        /// with another application.
        /// </summary>
        public static async Task<bool> WaitForPortAvailableAsync(
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (port < 1 || port > 65535) return false;

            var deadline = DateTime.UtcNow + timeout;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsPortAvailable(port)) return true;

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                await Task.Delay(remaining < TimeSpan.FromMilliseconds(75)
                    ? remaining
                    : TimeSpan.FromMilliseconds(75), cancellationToken);
            }
            while (DateTime.UtcNow < deadline);

            return IsPortAvailable(port);
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
