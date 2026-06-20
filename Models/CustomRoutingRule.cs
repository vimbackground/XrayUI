using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using XrayUI.Services;

namespace XrayUI.Models
{
    public class CustomRoutingRule
    {
        /// <summary>"domain" | "ip" | "process"</summary>
        public string Type { get; set; } = "domain";

        [JsonPropertyName("domain")]
        public List<string>? Domain { get; set; }

        [JsonPropertyName("ip")]
        public List<string>? Ip { get; set; }

        [JsonPropertyName("process")]
        public List<string>? Process { get; set; }

        /// <summary>Legacy settings.json compatibility. New saves use domain/ip/process arrays.</summary>
        [JsonPropertyName("Match")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LegacyMatch
        {
            get => null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    Match = value;
            }
        }

        /// <summary>youtube.com / 192.168.0.0/16 / geosite:cn / geoip:cn / chrome.exe / C:\Games\xxx.exe</summary>
        [JsonIgnore]
        public string Match
        {
            get => string.Join(Environment.NewLine, MatchValues);
            set => SetMatchValues(CustomRuleValueParser.Parse(value));
        }

        [JsonIgnore]
        public IReadOnlyList<string> MatchValues => Type switch
        {
            "ip"      => Ip ?? [],
            "process" => Process ?? [],
            _         => Domain ?? [],
        };

        /// <summary>"proxy" | "direct" | "block"</summary>
        public string OutboundTag { get; set; } = "proxy";

        public bool IsEnabled { get; set; } = true;

        // Helpers for x:Bind (OneTime) inside DataTemplate.
        // Visibility is computed directly to avoid converter lookups in a Window root.
        [JsonIgnore] public Visibility DomainVisibility  => Type == "domain"  ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore] public Visibility IpVisibility      => Type == "ip"      ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore] public Visibility ProcessVisibility => Type == "process" ? Visibility.Visible : Visibility.Collapsed;

        private void SetMatchValues(IReadOnlyList<string> values)
        {
            Domain = null;
            Ip = null;
            Process = null;

            var list = values.ToList();
            switch (Type)
            {
                case "ip":      Ip = list; break;
                case "process": Process = list; break;
                default:        Domain = list; break;
            }
        }

        public CustomRoutingRule Clone() => new()
        {
            Type        = Type,
            Domain      = Domain?.ToList(),
            Ip          = Ip?.ToList(),
            Process     = Process?.ToList(),
            OutboundTag = OutboundTag,
            IsEnabled   = IsEnabled,
        };
    }
}
