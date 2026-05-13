namespace XrayUI.Services
{
    /// <summary>
    /// xray-core DNS queryStrategy values. Persisted in AppSettings.DnsQueryStrategy
    /// and written verbatim to the xray dns config.
    /// </summary>
    internal static class DnsQueryStrategy
    {
        public const string IPv4 = "UseIPv4";
        public const string IPv6 = "UseIPv6";
        public const string Any  = "UseIP";
    }
}
