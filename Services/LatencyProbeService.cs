using System;
using System.Threading;
using System.Threading.Tasks;
using XrayUI.Models;

namespace XrayUI.Services
{
    public enum LatencyProbeStatus
    {
        Success,
        Timeout,
        Failed
    }

    public sealed class LatencyProbeResult
    {
        public LatencyProbeStatus Status { get; init; }

        public int? Milliseconds { get; init; }
    }

    public sealed class LatencyProbeService
    {
        private readonly TcpConnectProbeService _tcpConnectProbe;
        private readonly PingProbeService _pingProbe;

        public LatencyProbeService(
            TcpConnectProbeService tcpConnectProbe,
            PingProbeService pingProbe)
        {
            _tcpConnectProbe = tcpConnectProbe;
            _pingProbe = pingProbe;
        }

        public Task<LatencyProbeResult> ProbeAsync(
            ServerEntry server,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (server is null)
            {
                return Task.FromResult(new LatencyProbeResult
                {
                    Status = LatencyProbeStatus.Failed
                });
            }

            // hysteria2 and wireguard expose a UDP endpoint that won't answer a TCP connect, so
            // fall back to an ICMP ping of the host for their single-node latency readout.
            var isUdpEndpoint =
                string.Equals(server.Protocol, "hysteria2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(server.Protocol, "wireguard", StringComparison.OrdinalIgnoreCase);

            return isUdpEndpoint
                ? _pingProbe.ProbeAsync(server.Host, timeout, cancellationToken)
                : _tcpConnectProbe.ProbeAsync(server.Host, server.Port, timeout, cancellationToken);
        }
    }
}
