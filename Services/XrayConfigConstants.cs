namespace XrayUI.Services
{
    /// <summary>
    /// Inbound/outbound tags and well-known IP pools referenced by the xray-core JSON config.
    /// These strings are part of the contract with xray itself (they appear in routing rules,
    /// dns.servers entries, etc.), so changing them requires keeping every cross-reference in sync.
    /// </summary>
    internal static class XrayConfigConstants
    {
        // Inbound / outbound / DNS tags
        public const string TunInboundTag    = "tun-in";
        public const string MixedInboundTag  = "mixed-in";
        public const string DnsOutboundTag   = "dns-out";
        public const string FakeDnsServerTag = "fakedns";

        // FakeDNS IP pools. 198.18.0.0/15 is RFC-2544 benchmarking space (safe to reuse).
        public const string FakeDnsPoolV4 = "198.18.0.0/15";
        public const string FakeDnsPoolV6 = "fc00::/18";
    }
}
