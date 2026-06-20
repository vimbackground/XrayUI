using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace XrayUI.Models
{
    public class CustomRoutingRule
    {
        // Type/Match/Matches feed EffectiveMatches; their setters drop the cached
        // result so a deserialized-then-reassigned rule never serves a stale list.
        private string _type = "domain";
        private string _match = "";
        private List<string>? _matches;

        /// <summary>"domain" | "ip" | "process"</summary>
        public string Type { get => _type; set { _type = value; _effectiveMatches = null; } }

        /// <summary>youtube.com / 192.168.0.0/16 / geosite:cn / geoip:cn / chrome.exe / C:\Games\xxx.exe</summary>
        public string Match { get => _match; set { _match = value; _effectiveMatches = null; } }

        /// <summary>
        /// Optional multi-value form used by process rules. <see cref="Match"/> remains the
        /// first value for backward compatibility; when this list is present it is authoritative.
        /// </summary>
        public List<string>? Matches { get => _matches; set { _matches = value; _effectiveMatches = null; } }

        /// <summary>"proxy" | "direct" | "block"</summary>
        public string OutboundTag { get; set; } = "proxy";

        public bool IsEnabled { get; set; } = true;

        /// <summary>True when this rule matches on process name (xray <c>process</c> field).</summary>
        [JsonIgnore] public bool IsProcess => Type == "process";

        /// <summary>
        /// Trim, drop blanks, and de-duplicate (case-insensitive). Shared by
        /// <see cref="EffectiveMatches"/> and the Add/Edit dialog so the match
        /// contract lives in one place.
        /// </summary>
        public static string[] Normalize(IEnumerable<string> values) => values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        /// <summary>
        /// Normalized values used by the UI and xray config builder. Cached on first
        /// read; the Type/Match/Matches setters invalidate it. (Mutating the
        /// <see cref="Matches"/> list in place would not — the codebase only ever
        /// reassigns it.)
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<string> EffectiveMatches
            => _effectiveMatches ??= Normalize(IsProcess && Matches is not null ? Matches : [Match]);

        private IReadOnlyList<string>? _effectiveMatches;

        [JsonIgnore] public bool HasMatch => EffectiveMatches.Count > 0;

        [JsonIgnore]
        public string MatchSummary
        {
            get
            {
                var matches = EffectiveMatches;
                return matches.Count switch
                {
                    0 => "",
                    1 => matches[0],
                    _ => string.Join(" · ", matches),
                };
            }
        }

        [JsonIgnore] public string MatchDetails => string.Join(Environment.NewLine, EffectiveMatches);

        // Helpers for x:Bind (OneTime) inside DataTemplate.
        // Visibility is computed directly to avoid converter lookups in a Window root.
        [JsonIgnore] public Visibility DomainVisibility  => Type == "domain"  ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore] public Visibility IpVisibility      => Type == "ip"      ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore] public Visibility ProcessVisibility => IsProcess         ? Visibility.Visible : Visibility.Collapsed;

        public CustomRoutingRule Clone() => new()
        {
            Type        = Type,
            Match       = Match,
            Matches     = Matches?.ToList(),
            OutboundTag = OutboundTag,
            IsEnabled   = IsEnabled,
        };
    }
}
