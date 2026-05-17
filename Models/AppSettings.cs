using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace XrayUI.Models
{
    public class AppSettings
    {
        public int LocalMixedPort { get; set; } = 16890;
        /// <summary>"smart" | "global"</summary>
        public string RoutingMode { get; set; } = "smart";
        /// <summary>Whether TUN mode is enabled.</summary>
        public bool IsTunMode { get; set; } = false;
        public string? LastTunServerHost { get; set; }
        public bool IsStartupEnabled { get; set; } = false;
        public bool IsAutoConnect    { get; set; } = false;
        /// <summary>true = global proxy (default); false = do not take over the system proxy.</summary>
        public bool IsSystemProxyEnabled { get; set; } = true;
        /// <summary>Stable ID (ServerEntry.Id) of the most recently connected server — used for auto-connect on boot.</summary>
        public string? LastAutoConnectServerId { get; set; }
        /// <summary>Legacy (pre-Id) name-based setting. Read once for migration on first load after upgrade.</summary>
        public string? LastAutoConnectServerName { get; set; }
        /// <summary>"" | "quarter" | "half" | "full"; controls Xray log IP masking.</summary>
        public string LogMaskAddress { get; set; } = string.Empty;

        // ── Personalization ───────────────────────────────────────────────────
        /// <summary>"Light" | "Dark" | "Default" (follows system)</summary>
        public string? ThemeSetting { get; set; }
        /// <summary>"Mica" | "Acrylic"</summary>
        public string? BackdropSetting { get; set; }
        public string? ColorSs        { get; set; }
        public string? ColorVless     { get; set; }
        public string? ColorVmess     { get; set; }
        public string? ColorHysteria2 { get; set; }
        public string? ColorFallback  { get; set; }
        public bool ShowLatencyInDetails { get; set; } = true;
        public bool ShowAiUnlockInDetails { get; set; } = true;

        // ── DNS ───────────────────────────────────────────────────────────────
        /// <summary>Direct DNS for domestic domains (geosite:cn). null = choose the default based on TUN mode.</summary>
        public string? DirectDnsServer { get; set; }
        /// <summary>Proxy DNS for foreign domains, resolved through the proxy outbound. null = use the default 8.8.8.8.</summary>
        public string? ProxyDnsServer { get; set; }
        /// <summary>Values from <see cref="XrayUI.Services.DnsQueryStrategy"/>.</summary>
        public string DnsQueryStrategy { get; set; } = "UseIPv4";
        public bool DnsCacheEnabled { get; set; } = true;
        /// <summary>Enable xray FakeDNS. Only takes effect when IsTunMode is true.</summary>
        public bool FakeDnsEnabled { get; set; } = false;

        // ── Custom routing rules ──────────────────────────────────────────────
        /// <summary>User-defined routing rules. Applied only when RoutingMode == "smart".</summary>
        public List<CustomRoutingRule>? CustomRules { get; set; }

        /// <summary>
        /// Advanced routing JSON: a complete xray routing object ({ domainStrategy, balancers?, rules }).
        /// When non-null, replaces the default routing template; required TUN rules are still
        /// inserted at the start of rules, and CustomRules are still appended to the end.
        /// Only active when RoutingMode == "smart".
        /// </summary>
        public JsonObject? AdvancedRouting { get; set; }

        // ── Subscriptions ─────────────────────────────────────────────────────
        /// <summary>Persisted subscription sources. Nodes derived from these carry SubscriptionId = the entry's Id.</summary>
        public List<SubscriptionEntry>? Subscriptions { get; set; }
    }
}
