using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace XrayUI.Services
{
    internal static class FinalmaskJson
    {
        // Compact + default escaping — output is embedded in share URLs, where
        // the conservative default encoder (escapes `<`, `>`, `&`, `+`) is the
        // right choice. Storage path reuses AppJsonSerializerContext.WriteReadable
        // (indented + relaxed escaping, appropriate for the local config file).
        private static readonly JsonSerializerOptions CompactJson = new()
        {
            WriteIndented = false
        };

        public static string NormalizeForStorage(string? value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var node = Parse(value);
            return node?.ToJsonString(AppJsonSerializerContext.WriteReadable) ?? value;
        }

        public static string NormalizeForShare(string? value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var node = Parse(value);
            return node?.ToJsonString(CompactJson) ?? value;
        }

        public static JsonNode? Parse(string? value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(value);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Opens a stored mask for mutation. Fails for anything that is not an object — a mask
        /// we cannot understand must be handed back to the caller verbatim rather than silently
        /// replaced by a fresh one, so every mutating helper starts here.
        /// </summary>
        private static bool TryOpenRoot(string finalmask, [NotNullWhen(true)] out JsonObject? root)
        {
            var parsed = Parse(finalmask);
            root = parsed as JsonObject;
            if (root is not null)
                return true;

            if (parsed is null && string.IsNullOrWhiteSpace(finalmask))
            {
                root = [];
                return true;
            }

            return false;
        }

        private static string Write(JsonObject root) =>
            root.ToJsonString(AppJsonSerializerContext.WriteReadable);

        public static string AddHysteria2SalamanderMask(string finalmask, string password)
        {
            if (!TryOpenRoot(finalmask, out var root))
                return finalmask;

            var udp = root["udp"] as JsonArray;
            if (udp is null)
            {
                udp = [];
                root["udp"] = udp;
            }

            foreach (var item in udp)
            {
                if (item is JsonObject itemObject
                    && string.Equals(itemObject["type"]?.GetValue<string>(), "salamander", StringComparison.OrdinalIgnoreCase))
                {
                    return Write(root);
                }
            }

            udp.Insert(0, new JsonObject
            {
                ["type"] = "salamander",
                ["settings"] = new JsonObject
                {
                    ["password"] = password
                }
            });

            return Write(root);
        }

        /// <summary>Hop interval written for links that carry only a port range. Matches the hysteria2 default.</summary>
        public const string DefaultUdpHopInterval = "30";

        /// <summary>
        /// Folds a hysteria2 port-hopping range ("35000-39000", or a comma-separated list) into
        /// quicParams.udpHop. Idempotent: an existing udpHop wins, so a link carrying both an
        /// explicit fm= mask and mport= does not get its hand-tuned hop settings overwritten.
        /// </summary>
        public static string AddHysteria2UdpHop(
            string finalmask,
            string ports,
            string interval = DefaultUdpHopInterval)
        {
            ports = ports.Trim();
            if (ports.Length == 0)
                return finalmask;
            interval = string.IsNullOrWhiteSpace(interval)
                ? DefaultUdpHopInterval
                : interval.Trim();

            if (!TryOpenRoot(finalmask, out var root))
                return finalmask;

            JsonObject quicParams;
            switch (root["quicParams"])
            {
                case JsonObject existing: quicParams = existing; break;
                case null: root["quicParams"] = quicParams = []; break;
                default: return finalmask; // non-object quicParams: leave the mask alone
            }

            quicParams["udpHop"] ??= new JsonObject
            {
                ["ports"] = ports,
                ["interval"] = interval
            };

            return Write(root);
        }

        /// <summary>
        /// Inverse of <see cref="AddHysteria2SalamanderMask"/>: pulls the obfs password out so a
        /// share link can re-emit it as obfs= / obfs-password=, and returns what is left of the
        /// mask for the fm= parameter.
        /// </summary>
        public static (bool has, string? password, string remaining) TakeHysteria2Salamander(string finalmask)
        {
            if (Parse(finalmask) is not JsonObject root || root["udp"] is not JsonArray udp)
                return (false, null, finalmask);

            string? password = null;
            for (int i = 0; i < udp.Count; i++)
            {
                if (udp[i] is JsonObject itemObject
                    && string.Equals(AsText(itemObject["type"]), "salamander", StringComparison.OrdinalIgnoreCase))
                {
                    password = AsText((itemObject["settings"] as JsonObject)?["password"]) ?? string.Empty;
                    udp.RemoveAt(i);
                    break;
                }
            }

            if (password is null)
                return (false, null, finalmask);

            if (udp.Count == 0)
                root.Remove("udp");

            return (true, password, Remaining(root));
        }

        /// <summary>
        /// Inverse of <see cref="AddHysteria2UdpHop"/>: returns the hop port range so a share link
        /// can re-emit it as "mport". The entry is only removed when it carries this class's own
        /// shape; a hand-tuned hop has settings no link parameter can express, so it is reported
        /// *and* left in the returned mask — the caller then emits both, and re-importing does not
        /// double-apply because <see cref="AddHysteria2UdpHop"/> leaves an existing hop alone.
        /// </summary>
        public static (string? ports, string remaining) TakeHysteria2UdpHop(string finalmask)
        {
            if (Parse(finalmask) is not JsonObject root
                || root["quicParams"] is not JsonObject quicParams
                || quicParams["udpHop"] is not JsonObject udpHop
                || AsText(udpHop["ports"]) is not { Length: > 0 } ports)
            {
                return (null, finalmask);
            }

            // Remove only what AddHysteria2UdpHop itself writes: ports plus the default interval,
            // nothing else. Everything else is reported but left in place, so fm= carries it back
            // byte-exact. In particular a hand-written hop with no interval at all is NOT ours —
            // dropping it would mean re-importing invents an interval the author never wrote, on
            // the unverified assumption that omitting it is the same as writing "30".
            bool isCanonical = udpHop.Count == 2
                               && AsText(udpHop["interval"]) == DefaultUdpHopInterval;
            if (!isCanonical)
                return (ports, finalmask);

            quicParams.Remove("udpHop");
            if (quicParams.Count == 0)
                root.Remove("quicParams");

            return (ports, Remaining(root));
        }

        /// <summary>
        /// Re-encodes a mask that just had an entry lifted out of it, for the fm= parameter.
        /// A mask emptied by the removal shares as an absent parameter, not as "{}".
        /// </summary>
        private static string Remaining(JsonObject root) =>
            root.Count == 0 ? string.Empty : root.ToJsonString();

        /// <summary>
        /// Reads a mask value as text whether it was written as a JSON string or a bare number
        /// (hand-written masks use both for ports/interval). Returns null for anything else.
        /// </summary>
        private static string? AsText(JsonNode? node) => node switch
        {
            JsonValue value when value.TryGetValue<string>(out var s) => s,
            JsonValue value when value.TryGetValue<long>(out var n) => n.ToString(CultureInfo.InvariantCulture),
            _ => null
        };
    }
}
