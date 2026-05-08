namespace XrayUI.Services
{
    /// <summary>
    /// Shared constants and normalization for VLESS+TLS Encrypted Client Hello (ECH) settings.
    /// Used by parser, serializer, config builder, dialog, and detail view.
    /// </summary>
    internal static class EchSettings
    {
        public const string None = "none";
        public const string Half = "half";
        public const string Full = "full";

        /// <summary>
        /// Returns "half"/"full" if value matches (after trim + lower-invariant); otherwise empty.
        /// "none" maps to empty since the model stores absence as empty string.
        /// </summary>
        public static string NormalizeForceQuery(string? value)
        {
            value = value?.Trim().ToLowerInvariant();
            return value is Half or Full ? value : string.Empty;
        }
    }
}
