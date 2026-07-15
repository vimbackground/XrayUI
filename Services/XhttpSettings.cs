namespace XrayUI.Services
{
    /// <summary>
    /// Shared constants and normalization for the XHTTP transport mode.
    /// Used by parser, serializer, config builder, and dialog.
    /// </summary>
    internal static class XhttpSettings
    {
        public const string Auto = "auto";
        public const string PacketUp = "packet-up";
        public const string StreamUp = "stream-up";
        public const string StreamOne = "stream-one";

        /// <summary>Canonical mode values, in the order the edit dialog offers them.</summary>
        public static readonly string[] Modes = [Auto, PacketUp, StreamUp, StreamOne];

        /// <summary>
        /// Returns the canonical mode if value matches one (after trim + lower-invariant);
        /// otherwise empty — the model stores "not set" (xray defaults to auto) as empty string.
        /// A typo or a foreign "mode" value (grpc links reuse the key for gun/multi) can then
        /// never produce an xray config that fails to load.
        /// </summary>
        public static string NormalizeMode(string? value)
        {
            value = value?.Trim().ToLowerInvariant();
            return value is Auto or PacketUp or StreamUp or StreamOne ? value : string.Empty;
        }
    }
}
