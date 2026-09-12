using System;

namespace XrayUI.Models
{
    public sealed record ProxyConnectionEvent(
        DateTimeOffset Timestamp,
        ProxyConnectionEventKind Kind,
        string Inbound,
        string Outbound,
        string Target,
        string Detail);

    public enum ProxyConnectionEventKind
    {
        Accepted,
        Closed,
        Failed,
    }
}
