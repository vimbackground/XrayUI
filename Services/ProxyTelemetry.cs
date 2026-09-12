using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using XrayUI.Models;

namespace XrayUI.Services
{
    public sealed record ProxyTelemetrySnapshot(
        int ActiveConnections,
        long TotalConnections,
        long FailedConnections,
        IReadOnlyList<ProxyConnectionEvent> RecentConnections);

    public sealed class ProxyTelemetry
    {
        private const int MaxRecentConnections = 100;
        private readonly object _gate = new();
        private readonly LinkedList<ProxyConnectionEvent> _recent = new();
        private int _activeConnections;
        private long _totalConnections;
        private long _failedConnections;

        public event EventHandler<ProxyConnectionEvent>? EventReceived;

        public void Reset()
        {
            lock (_gate)
            {
                _activeConnections = 0;
                _totalConnections = 0;
                _failedConnections = 0;
                _recent.Clear();
            }
        }

        public void Observe(string line)
        {
            var connectionEvent = ProxyConnectionLogParser.Parse(line);
            if (connectionEvent is null)
                return;

            lock (_gate)
            {
                switch (connectionEvent.Kind)
                {
                    case ProxyConnectionEventKind.Accepted:
                        _activeConnections++;
                        _totalConnections++;
                        break;
                    case ProxyConnectionEventKind.Closed:
                        _activeConnections = Math.Max(0, _activeConnections - 1);
                        break;
                    case ProxyConnectionEventKind.Failed:
                        _failedConnections++;
                        break;
                }

                _recent.AddLast(connectionEvent);
                while (_recent.Count > MaxRecentConnections)
                    _recent.RemoveFirst();
            }

            EventReceived?.Invoke(this, connectionEvent);
        }

        public ProxyTelemetrySnapshot Snapshot()
        {
            lock (_gate)
            {
                return new ProxyTelemetrySnapshot(
                    _activeConnections,
                    _totalConnections,
                    _failedConnections,
                    _recent.Reverse().ToArray());
            }
        }
    }

    public static class ProxyConnectionLogParser
    {
        private static readonly Regex AcceptedRegex = new(
            @"accepted\s+(?<network>[^:\s]+):(?<target>[^\s]+)(?:\s+\[(?<route>[^\]]+)\])?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex RouteRegex = new(
            @"\[(?<inbound>[^\]\s]+)\s*->\s*(?<outbound>[^\]\s]+)\]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static ProxyConnectionEvent? Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var timestamp = ParseTimestamp(line);
            var route = RouteRegex.Match(line);
            var inbound = route.Success ? route.Groups["inbound"].Value : string.Empty;
            var outbound = route.Success ? route.Groups["outbound"].Value : string.Empty;

            var accepted = AcceptedRegex.Match(line);
            if (accepted.Success)
            {
                if (string.IsNullOrWhiteSpace(outbound))
                    outbound = accepted.Groups["route"].Value;

                return new ProxyConnectionEvent(
                    timestamp,
                    ProxyConnectionEventKind.Accepted,
                    inbound,
                    outbound,
                    accepted.Groups["target"].Value,
                    line);
            }

            if (ContainsAny(line, "connection ends", "connection closed", "closed connection"))
            {
                return new ProxyConnectionEvent(
                    timestamp,
                    ProxyConnectionEventKind.Closed,
                    inbound,
                    outbound,
                    string.Empty,
                    line);
            }

            if (ContainsAny(line, "failed", "failure", "timeout", "timed out", "connection refused", "rejected"))
            {
                return new ProxyConnectionEvent(
                    timestamp,
                    ProxyConnectionEventKind.Failed,
                    inbound,
                    outbound,
                    string.Empty,
                    line);
            }

            return null;
        }

        private static bool ContainsAny(string value, params string[] candidates)
            => candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

        private static DateTimeOffset ParseTimestamp(string line)
        {
            var length = LogLineParser.TimestampLength(line);
            return length > 0 && DateTimeOffset.TryParse(line[..length], out var timestamp)
                ? timestamp
                : DateTimeOffset.Now;
        }
    }
}
