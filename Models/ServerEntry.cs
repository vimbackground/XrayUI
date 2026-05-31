using System.Text.Json.Serialization;
using XrayUI.Helpers;

namespace XrayUI.Models
{
    public partial class ServerEntry : ObservableObject
    {
        // Stable identifier for this entry; survives rename/dedupe.
        // Auto-generated for new entries and for legacy entries loaded from JSON without an ID.
        private string _id = System.Guid.NewGuid().ToString("N");

        public ServerEntry()
        {
            SubscriptionId = string.Empty;
            Name = string.Empty;
            Host = string.Empty;
            Protocol = string.Empty;
            Encryption = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            Uuid = string.Empty;
            Network = "tcp";
            Path = string.Empty;
            WsHost = string.Empty;
            Security = string.Empty;
            Sni = string.Empty;
            Fingerprint = string.Empty;
            EchConfigList = string.Empty;
            EchForceQuery = string.Empty;
            PublicKey = string.Empty;
            ShortId = string.Empty;
            SpiderX = string.Empty;
            Flow = string.Empty;
            VlessEncryption = string.Empty;
            Finalmask = string.Empty;
            ChainEntryServerId = string.Empty;
            ChainExitServerId = string.Empty;
        }

        /// <summary>ID of the subscription this node was imported from; empty = manually added.</summary>
        [ObservableProperty]
        public partial string SubscriptionId { get; set; }

        [ObservableProperty]
        public partial string Name { get; set; }

        [ObservableProperty]
        public partial string Host { get; set; }

        [ObservableProperty]
        public partial int Port { get; set; }

        /// <summary>ss | vmess | vless | hysteria2 | trojan | socks | chain</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayProtocol))]
        [NotifyPropertyChangedFor(nameof(IsChain))]
        public partial string Protocol { get; set; }

        /// <summary>Cipher for ss; "TLS" or "Reality" label for vless/vmess/hysteria2</summary>
        [ObservableProperty]
        public partial string Encryption { get; set; }

        // Runtime-only flag (which server xray is currently proxying through).
        // Never persisted — otherwise an export captures it and a later import
        // would resurrect the badge on a stale server.
        [JsonIgnore]
        [ObservableProperty]
        public partial bool IsActive { get; set; }

        // Runtime-only latency probe result in milliseconds; null = not yet measured.
        // Never persisted — a probe is meaningless across sessions and would bloat exports.
        [JsonIgnore]
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LatencyText))]
        [NotifyPropertyChangedFor(nameof(HasLatency))]
        public partial int? LatencyMs { get; set; }

        [ObservableProperty]
        public partial bool IsFavorite { get; set; }

        // Auth
        [ObservableProperty]
        public partial string Username { get; set; }

        [ObservableProperty]
        public partial string Password { get; set; }

        [ObservableProperty]
        public partial string Uuid { get; set; }

        // Transport
        [ObservableProperty]
        public partial string Network { get; set; }

        [ObservableProperty]
        public partial string Path { get; set; }

        [ObservableProperty]
        public partial string WsHost { get; set; }

        [ObservableProperty]
        public partial int AlterId { get; set; }

        // TLS / Security
        [ObservableProperty]
        public partial string Security { get; set; }

        [ObservableProperty]
        public partial string Sni { get; set; }

        [ObservableProperty]
        public partial string Fingerprint { get; set; }

        [ObservableProperty]
        public partial bool AllowInsecure { get; set; }

        /// <summary>Client-side TLS ECH config list. Empty = disabled.</summary>
        [ObservableProperty]
        public partial string EchConfigList { get; set; }

        /// <summary>ECH force query mode: "half", "full", or empty/none.</summary>
        [ObservableProperty]
        public partial string EchForceQuery { get; set; }

        // VLESS Reality
        [ObservableProperty]
        public partial string PublicKey { get; set; }

        [ObservableProperty]
        public partial string ShortId { get; set; }

        [ObservableProperty]
        public partial string SpiderX { get; set; }

        /// <summary>VLESS flow: "xtls-rprx-vision" or empty string.</summary>
        [ObservableProperty]
        public partial string Flow { get; set; }

        /// <summary>VLESS user-level encryption (Xray PR #5067 PQ encryption, e.g. "mlkem768x25519plus...."). Empty = "none".</summary>
        [ObservableProperty]
        public partial string VlessEncryption { get; set; }

        /// <summary>Raw Xray streamSettings.finalmask JSON, shared as the "fm" URI parameter.</summary>
        [ObservableProperty]
        public partial string Finalmask { get; set; }

        // Proxy chain
        [ObservableProperty]
        public partial string ChainEntryServerId { get; set; }

        [ObservableProperty]
        public partial string ChainExitServerId { get; set; }

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? System.Guid.NewGuid().ToString("N") : value);
        }

        [JsonIgnore]
        public string DisplayProtocol => Protocol.ToLowerInvariant() switch
        {
            "ss" => "Shadowsocks",
            "vmess" => "VMess",
            "vless" => "VLESS",
            "hysteria2" => "Hysteria 2",
            "trojan" => "Trojan",
            "socks" => "SOCKS",
            "chain" => "ProxyChain",
            _ => Protocol ?? string.Empty
        };

        [JsonIgnore]
        public bool IsChain => string.Equals(Protocol, "chain", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Latency formatted for display: "30 ms" for a measurement, a timeout label for a
        /// failed probe (negative sentinel such as -1), empty when not measured.
        /// </summary>
        [JsonIgnore]
        public string LatencyText => LatencyMs switch
        {
            null   => string.Empty,
            < 0    => L.ServerDetail_Timeout,
            int ms => Loc.Format("ServerDetail_LatencyMs", ms),
        };

        /// <summary>Whether a measured latency is available to show in the list row.</summary>
        [JsonIgnore]
        public bool HasLatency => LatencyMs.HasValue;

        public void RefreshProtocolColor() => OnPropertyChanged(nameof(Protocol));
    }
}
