using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using XrayUI.Helpers;
using XrayUI.Models;

namespace XrayUI.Services
{
    /// <summary>
    /// Builds an xray-core JSON configuration string for the given server and app settings.
    /// Uses JsonObject/JsonArray so Native AOT does not need reflection-based serialization.
    /// </summary>
    public static class XrayConfigBuilder
    {
        private const string DefaultLogLevel = "info";
        private const string ProxyOutboundTag = "proxy";
        private const string DirectOutboundTag = "direct";
        private const string BlockOutboundTag = "block";
        private const string ChainEntryOutboundTag = "chain-entry";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true
        };

        public static string Build(
            ServerEntry server,
            AppSettings settings,
            IEnumerable<ServerEntry>? availableServers = null)
        {
            var config = new JsonObject
            {
                ["log"] = BuildLog(settings),
                ["dns"] = BuildDns(settings),
                ["inbounds"] = BuildInbounds(settings),
                ["outbounds"] = BuildOutbounds(server, settings, availableServers),
                ["routing"] = BuildRouting(settings)
            };

            if (IsFakeDnsActive(settings))
            {
                var pools = new JsonArray();
                AddNode(pools, new JsonObject
                {
                    ["ipPool"] = XrayConfigConstants.FakeDnsPoolV4,
                    ["poolSize"] = 65535,
                });
                AddNode(pools, new JsonObject
                {
                    ["ipPool"] = XrayConfigConstants.FakeDnsPoolV6,
                    ["poolSize"] = 65535,
                });
                config["fakedns"] = pools;
            }

            return config.ToJsonString(JsonOpts);
        }

        /// <summary>True when xray will be built with a fakedns pool wired to the TUN inbound.</summary>
        private static bool IsFakeDnsActive(AppSettings settings) =>
            settings.IsTunMode && settings.FakeDnsEnabled;

        private static JsonObject BuildLog(AppSettings settings)
        {
            var log = new JsonObject
            {
                ["loglevel"] = DefaultLogLevel
            };

            if (LogMaskAddress.IsEnabled(settings.LogMaskAddress))
            {
                log["maskAddress"] = settings.LogMaskAddress;
            }

            return log;
        }

        private static JsonArray BuildInbounds(AppSettings settings)
        {
            var list = new JsonArray();

            if (settings.IsTunMode)
            {
                AddNode(list, BuildTunInbound(settings));
            }

            AddNode(list, new JsonObject
            {
                ["tag"] = XrayConfigConstants.MixedInboundTag,
                ["protocol"] = "socks",
                ["listen"] = "127.0.0.1",
                ["port"] = settings.LocalMixedPort,
                ["settings"] = new JsonObject
                {
                    ["auth"] = "noauth",
                    ["udp"] = true
                }
            });

            return list;
        }

        private static JsonObject BuildTunInbound(AppSettings settings)
        {
            var destOverride = settings.FakeDnsEnabled
                ? CreateStringArray(XrayConfigConstants.FakeDnsServerTag, "http", "tls", "quic")
                : CreateStringArray("http", "tls", "quic");

            var sniffing = new JsonObject
            {
                ["enabled"] = true,
                ["destOverride"] = destOverride,
            };
            if (settings.FakeDnsEnabled)
            {
                sniffing["metadataOnly"] = false;
            }

            // IPv6 is opt-in: only when enabled do we hand the TUN a v6 gateway and hijack ::/0,
            // so IPv4-only networks keep the leak-free v4-only behaviour.
            var gateway = settings.TunIpv6Enabled
                ? CreateStringArray(XrayConfigConstants.TunGatewayV4, XrayConfigConstants.TunGatewayV6)
                : CreateStringArray(XrayConfigConstants.TunGatewayV4);
            var autoRoutes = settings.TunIpv6Enabled
                ? CreateStringArray(XrayConfigConstants.TunAutoRouteV4, XrayConfigConstants.TunAutoRouteV6)
                : CreateStringArray(XrayConfigConstants.TunAutoRouteV4);

            return new JsonObject
            {
                ["tag"] = XrayConfigConstants.TunInboundTag,
                ["protocol"] = "tun",
                ["settings"] = new JsonObject
                {
                    ["name"] = XrayConfigConstants.TunInterfaceName,
                    ["MTU"] = XrayConfigConstants.NormalizeTunMtu(settings.TunMtu),
                    ["gateway"] = gateway,
                    ["autoSystemRoutingTable"] = autoRoutes,
                    ["autoOutboundsInterface"] = XrayConfigConstants.TunOutboundInterfaceAuto
                },
                ["sniffing"] = sniffing,
            };
        }

        private static JsonArray BuildOutbounds(
            ServerEntry server,
            AppSettings settings,
            IEnumerable<ServerEntry>? availableServers)
        {
            var list = new JsonArray();

            if (server.IsChain)
            {
                var (entryServer, exitServer) = ResolveChainServers(server, availableServers);
                var proxy = BuildProxyOutbound(exitServer, ProxyOutboundTag);
                var chainEntry = BuildProxyOutbound(entryServer, ChainEntryOutboundTag);
                ApplyProxySettings(proxy, ChainEntryOutboundTag);
                AddNode(list, proxy);
                AddNode(list, chainEntry);
            }
            else
            {
                AddNode(list, BuildProxyOutbound(server, ProxyOutboundTag));
            }

            var direct = new JsonObject
            {
                ["tag"] = DirectOutboundTag,
                ["protocol"] = "freedom",
                ["settings"] = new JsonObject()
            };

            AddNode(list, direct);

            // block outbound is needed by:
            //   1. TUN mode's UDP:443 quench rule
            //   2. Any enabled custom rule targeting "block" (smart mode only)
            bool customRulesUseBlock =
                settings.RoutingMode == "smart"
                && settings.CustomRules is { } rules
                && rules.Any(r => r.IsEnabled
                                  && !string.IsNullOrWhiteSpace(r.Match)
                                  && r.OutboundTag == BlockOutboundTag);

            if (settings.IsTunMode || customRulesUseBlock)
            {
                AddNode(list, new JsonObject
                {
                    ["tag"] = BlockOutboundTag,
                    ["protocol"] = "blackhole",
                    ["settings"] = new JsonObject()
                });
            }

            if (IsFakeDnsActive(settings))
            {
                AddNode(list, new JsonObject
                {
                    ["tag"] = XrayConfigConstants.DnsOutboundTag,
                    ["protocol"] = "dns",
                });
            }

            var outboundInterface = NormalizeTunOutboundInterface(settings.TunOutboundInterface);
            if (settings.IsTunMode && outboundInterface is not null)
            {
                foreach (var outbound in list.OfType<JsonObject>())
                {
                    var tag = outbound["tag"]?.GetValue<string>();
                    if (tag is ProxyOutboundTag or DirectOutboundTag or ChainEntryOutboundTag)
                        ApplyOutboundInterface(outbound, outboundInterface);
                }
            }

            return list;
        }

        private static string? NormalizeTunOutboundInterface(string? interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
                return null;

            var value = interfaceName.Trim();
            return string.Equals(value, XrayConfigConstants.TunOutboundInterfaceAuto, StringComparison.OrdinalIgnoreCase)
                ? null
                : value;
        }

        private static (ServerEntry entryServer, ServerEntry exitServer) ResolveChainServers(
            ServerEntry chain,
            IEnumerable<ServerEntry>? availableServers)
        {
            if (availableServers is null)
            {
                throw new InvalidOperationException(L.Chain_NeedServerList);
            }

            ServerEntry? entryServer = null;
            ServerEntry? exitServer = null;
            foreach (var s in availableServers)
            {
                if (entryServer is null && s.Id == chain.ChainEntryServerId) entryServer = s;
                if (exitServer is null && s.Id == chain.ChainExitServerId) exitServer = s;
                if (entryServer is not null && exitServer is not null) break;
            }

            if (entryServer is null || exitServer is null)
            {
                throw new InvalidOperationException(L.Chain_EndpointMissing);
            }

            if (entryServer.IsChain || exitServer.IsChain)
            {
                throw new InvalidOperationException(L.Chain_NoNesting);
            }

            return (entryServer, exitServer);
        }

        private static void ApplyProxySettings(JsonObject outbound, string tag)
        {
            outbound["proxySettings"] = new JsonObject
            {
                ["tag"] = tag,
                ["transportLayer"] = true
            };
        }

        private static void ApplyOutboundInterface(JsonObject outbound, string interfaceName)
        {
            var streamSettings = outbound["streamSettings"] as JsonObject;
            if (streamSettings is null)
            {
                streamSettings = new JsonObject();
                outbound["streamSettings"] = streamSettings;
            }

            var sockopt = streamSettings["sockopt"] as JsonObject;
            if (sockopt is null)
            {
                sockopt = new JsonObject();
                streamSettings["sockopt"] = sockopt;
            }

            sockopt["interface"] = interfaceName;
        }

        private static JsonObject BuildProxyOutbound(ServerEntry server, string tag)
        {
            return server.Protocol.ToLowerInvariant() switch
            {
                "vmess" => BuildVmessOutbound(server, tag),
                "vless" => BuildVlessOutbound(server, tag),
                "hysteria2" => BuildHysteria2Outbound(server, tag),
                "trojan" => BuildTrojanOutbound(server, tag),
                "socks" => BuildSocksOutbound(server, tag),
                _ => BuildSsOutbound(server, tag)
            };
        }

        private static JsonObject BuildSsOutbound(ServerEntry server, string tag)
        {
            var servers = new JsonArray();
            AddNode(servers, new JsonObject
            {
                ["address"] = server.Host,
                ["port"] = server.Port,
                ["method"] = server.Encryption,
                ["password"] = server.Password
            });

            var outbound = new JsonObject
            {
                ["tag"] = tag,
                ["protocol"] = "shadowsocks",
                ["settings"] = new JsonObject
                {
                    ["servers"] = servers
                },
                ["streamSettings"] = new JsonObject
                {
                    ["network"] = "tcp"
                }
            };

            ApplyFinalmask((JsonObject)outbound["streamSettings"]!, server);
            return outbound;
        }

        private static JsonObject BuildVmessOutbound(ServerEntry server, string tag)
        {
            var users = new JsonArray();
            AddNode(users, new JsonObject
            {
                ["id"] = server.Uuid,
                ["alterId"] = server.AlterId,
                ["security"] = "auto"
            });

            var vnext = new JsonArray();
            AddNode(vnext, new JsonObject
            {
                ["address"] = server.Host,
                ["port"] = server.Port,
                ["users"] = users
            });

            return new JsonObject
            {
                ["tag"] = tag,
                ["protocol"] = "vmess",
                ["settings"] = new JsonObject
                {
                    ["vnext"] = vnext
                },
                ["streamSettings"] = BuildStreamSettings(server)
            };
        }

        private static JsonObject BuildVlessOutbound(ServerEntry server, string tag)
        {
            var user = new JsonObject
            {
                ["id"] = server.Uuid,
                ["encryption"] = string.IsNullOrEmpty(server.VlessEncryption) ? "none" : server.VlessEncryption
            };

            if (!string.IsNullOrWhiteSpace(server.Flow))
            {
                user["flow"] = server.Flow;
            }

            var users = new JsonArray();
            AddNode(users, user);

            var vnext = new JsonArray();
            AddNode(vnext, new JsonObject
            {
                ["address"] = server.Host,
                ["port"] = server.Port,
                ["users"] = users
            });

            return new JsonObject
            {
                ["tag"] = tag,
                ["protocol"] = "vless",
                ["settings"] = new JsonObject
                {
                    ["vnext"] = vnext
                },
                ["streamSettings"] = BuildStreamSettings(server)
            };
        }

        private static JsonObject BuildHysteria2Outbound(ServerEntry server, string tag)
        {
            var sni = string.IsNullOrWhiteSpace(server.Sni) ? server.Host : server.Sni;

            var streamSettings = new JsonObject
            {
                ["network"] = "hysteria",
                ["security"] = "tls",
                ["tlsSettings"] = new JsonObject
                {
                    ["serverName"] = sni,
                    ["allowInsecure"] = server.AllowInsecure
                },
                ["hysteriaSettings"] = new JsonObject
                {
                    ["version"] = 2,
                    ["auth"] = server.Password
                }
            };
            ApplyFinalmask(streamSettings, server);

            return new JsonObject
            {
                ["tag"] = tag,
                ["protocol"] = "hysteria",
                ["settings"] = new JsonObject
                {
                    ["version"] = 2,
                    ["address"] = server.Host,
                    ["port"] = server.Port
                },
                ["streamSettings"] = streamSettings
            };
        }

        private static JsonObject BuildTrojanOutbound(ServerEntry server, string tag)
        {
            return new JsonObject
            {
                ["tag"] = tag,
                ["protocol"] = "trojan",
                ["settings"] = new JsonObject
                {
                    ["address"] = server.Host,
                    ["port"] = server.Port,
                    ["password"] = server.Password
                },
                ["streamSettings"] = BuildStreamSettings(server)
            };
        }

        private static JsonObject BuildSocksOutbound(ServerEntry server, string tag)
        {
            var serverObject = new JsonObject
            {
                ["address"] = server.Host,
                ["port"] = server.Port,
            };

            if (!string.IsNullOrWhiteSpace(server.Username)
                || !string.IsNullOrWhiteSpace(server.Password))
            {
                var users = new JsonArray();
                AddNode(users, new JsonObject
                {
                    ["user"] = server.Username,
                    ["pass"] = server.Password,
                });
                serverObject["users"] = users;
            }

            var servers = new JsonArray();
            AddNode(servers, serverObject);

            return new JsonObject
            {
                ["tag"] = tag,
                ["protocol"] = "socks",
                ["settings"] = new JsonObject
                {
                    ["servers"] = servers
                }
            };
        }

        private static JsonObject BuildStreamSettings(ServerEntry server)
        {
            var network = string.IsNullOrWhiteSpace(server.Network)
                ? "tcp"
                : server.Network.ToLowerInvariant();
            var security = string.IsNullOrWhiteSpace(server.Security)
                ? "none"
                : server.Security.ToLowerInvariant();

            var stream = new JsonObject
            {
                ["network"] = network,
                ["security"] = security
            };

            if (security == "tls")
            {
                var sni = string.IsNullOrWhiteSpace(server.Sni) ? server.Host : server.Sni;
                var fingerprint = string.IsNullOrWhiteSpace(server.Fingerprint) ? "chrome" : server.Fingerprint;
                var tlsSettings = new JsonObject
                {
                    ["serverName"] = sni,
                    ["fingerprint"] = fingerprint,
                    ["allowInsecure"] = server.AllowInsecure
                };

                if (string.Equals(server.Protocol, "vless", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(server.EchConfigList))
                {
                    tlsSettings["echConfigList"] = server.EchConfigList;

                    var echForceQuery = EchSettings.NormalizeForceQuery(server.EchForceQuery);
                    if (!string.IsNullOrEmpty(echForceQuery))
                    {
                        tlsSettings["echForceQuery"] = echForceQuery;
                    }
                }

                stream["tlsSettings"] = tlsSettings;
            }
            else if (security == "reality")
            {
                var sni = string.IsNullOrWhiteSpace(server.Sni) ? server.Host : server.Sni;
                var fingerprint = string.IsNullOrWhiteSpace(server.Fingerprint) ? "chrome" : server.Fingerprint;
                var spiderX = string.IsNullOrWhiteSpace(server.SpiderX) ? "/" : server.SpiderX;

                stream["realitySettings"] = new JsonObject
                {
                    ["serverName"] = sni,
                    ["fingerprint"] = fingerprint,
                    ["publicKey"] = server.PublicKey,
                    ["shortId"] = server.ShortId,
                    ["spiderX"] = spiderX
                };
            }

            if (network == "ws")
            {
                JsonObject headers;
                if (string.IsNullOrWhiteSpace(server.WsHost))
                {
                    headers = [];
                }
                else
                {
                    headers = new JsonObject
                    {
                        ["Host"] = server.WsHost
                    };
                }

                stream["wsSettings"] = new JsonObject
                {
                    ["path"] = server.Path,
                    ["headers"] = headers
                };
            }
            else if (network == "grpc")
            {
                stream["grpcSettings"] = new JsonObject
                {
                    ["serviceName"] = server.Path
                };
            }
            else if (network == "xhttp")
            {
                var settings = new JsonObject
                {
                    ["path"] = server.Path
                };

                if (!string.IsNullOrWhiteSpace(server.WsHost))
                {
                    settings["host"] = server.WsHost;
                }

                stream["xhttpSettings"] = settings;
            }

            ApplyFinalmask(stream, server);
            return stream;
        }

        private static void ApplyFinalmask(JsonObject streamSettings, ServerEntry server)
        {
            var finalmask = FinalmaskJson.Parse(server.Finalmask);
            if (finalmask is JsonObject)
            {
                streamSettings["finalmask"] = finalmask;
            }
        }

        private static JsonObject BuildRouting(AppSettings settings)
        {
            // Global mode bypasses both AdvancedRouting and the smart-mode default template;
            // it always force-routes everything to the proxy outbound after the TUN prefix.
            if (settings.RoutingMode == "global")
            {
                return BuildGlobalRouting(settings);
            }

            // Smart mode: AdvancedRouting (if set) replaces the default routing template.
            // TUN prefix rules and CustomRules are merged on top, so the user cannot lock
            // themselves out of TUN-required system traffic by writing a bad advanced JSON.
            var hasAdvancedRouting = settings.AdvancedRouting is not null;
            var baseRouting = hasAdvancedRouting
                ? (JsonObject)settings.AdvancedRouting!.DeepClone()
                : BuildDefaultRoutingTemplate(settings, includeFallback: false);

            // baseRouting is exclusively owned (fresh clone or fresh build), so mutate
            // its rules array in place. Final order:
            //   TUN prefix → CustomRules → AdvancedRouting/default rules → default fallback.
            // CustomRules sit *before* the base rules so a user rule like
            // "domain:*.cn → proxy" preempts the default geosite:cn → direct, and so any
            // terminal catch-all the user keeps in their advanced JSON does not shadow them.
            if (baseRouting["rules"] is not JsonArray rules)
            {
                rules = new JsonArray();
                baseRouting["rules"] = rules;
            }

            var insertIdx = PrependTunPrefixRules(rules, settings);

            if (settings.CustomRules is { } customRules)
            {
                foreach (var rule in customRules)
                {
                    if (!rule.IsEnabled || string.IsNullOrWhiteSpace(rule.Match))
                        continue;
                    rules.Insert(insertIdx++, CustomRuleToJsonObject(rule));
                }
            }

            if (!hasAdvancedRouting)
            {
                AddDefaultProxyFallbackRule(rules);
            }

            if (baseRouting["domainStrategy"] is null)
            {
                baseRouting["domainStrategy"] = "AsIs";
            }

            return baseRouting;
        }

        private static JsonObject BuildGlobalRouting(AppSettings settings)
        {
            var rules = new JsonArray();
            PrependTunPrefixRules(rules, settings);

            AddNode(rules, new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = ProxyOutboundTag,
                ["network"] = "tcp,udp"
            });

            return new JsonObject
            {
                ["domainStrategy"] = "AsIs",
                ["rules"] = rules
            };
        }

        /// <summary>
        /// Inserts the fixed TUN-prefix rules at the head of <paramref name="rules"/> and
        /// returns the number inserted, so callers know where the user-rule region starts.
        /// These keep system/self traffic out of the tunnel and suppress QUIC over UDP/443;
        /// never user-editable and must precede any user/advanced rules.
        /// </summary>
        private static int PrependTunPrefixRules(JsonArray rules, AppSettings settings)
        {
            if (!settings.IsTunMode) return 0;

            var i = 0;
            if (settings.FakeDnsEnabled)
            {
                // Must precede the self/xray direct rule so DNS queries from tun-in get
                // intercepted by xray's internal DNS handler (and the fakedns pool) rather
                // than being forwarded upstream.
                rules.Insert(i++, new JsonObject
                {
                    ["type"] = "field",
                    ["inboundTag"] = CreateStringArray(XrayConfigConstants.TunInboundTag),
                    ["port"] = "53",
                    ["outboundTag"] = XrayConfigConstants.DnsOutboundTag,
                });
            }

            rules.Insert(i++, new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = DirectOutboundTag,
                ["process"] = CreateStringArray("self/", "xray/")
            });

            rules.Insert(i++, new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = BlockOutboundTag,
                ["network"] = "udp",
                ["port"] = "443"
            });

            return i;
        }

        /// <summary>
        /// The default smart-mode routing object — proxy Google, direct geosite:cn / geoip:cn,
        /// fallback everything else to proxy. Returned as a fresh JsonObject so callers can
        /// either inject it into the live xray config or persist it as the seed of
        /// settings.AdvancedRouting (the "advanced editor" template).
        /// </summary>
        public static JsonObject BuildDefaultRoutingTemplate(AppSettings settings, bool includeFallback = true)
        {
            var rules = new JsonArray();

            AddNode(rules, new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = ProxyOutboundTag,
                ["domain"] = CreateStringArray("geosite:google")
            });
            AddNode(rules, new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = DirectOutboundTag,
                ["domain"] = CreateStringArray("geosite:cn", "geosite:private")
            });
            AddNode(rules, new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = DirectOutboundTag,
                ["ip"] = CreateStringArray("geoip:cn", "geoip:private")
            });
            if (includeFallback)
            {
                AddDefaultProxyFallbackRule(rules);
            }

            return new JsonObject
            {
                ["domainStrategy"] = "AsIs",
                ["rules"] = rules
            };
        }

        private static void AddDefaultProxyFallbackRule(JsonArray rules)
        {
            AddNode(rules, new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = ProxyOutboundTag,
                ["network"] = "tcp,udp"
            });
        }

        private static JsonObject CustomRuleToJsonObject(CustomRoutingRule rule)
        {
            var node = new JsonObject
            {
                ["type"] = "field",
                ["outboundTag"] = rule.OutboundTag,
            };
            switch (rule.Type)
            {
                case "ip":      node["ip"]      = CreateStringArray(rule.Match); break;
                case "process": node["process"] = CreateStringArray(rule.Match); break;
                default:        node["domain"]  = CreateStringArray(rule.Match); break;
            }
            return node;
        }

        private static JsonObject BuildDns(AppSettings settings)
        {
            var directDns = settings.DirectDnsServer
                ?? (settings.IsTunMode ? "223.5.5.5" : "114.114.114.114");
            var proxyDns = settings.ProxyDnsServer ?? "8.8.8.8";

            var directEntry = new JsonObject
            {
                ["address"]      = directDns,
                ["domains"]      = CreateStringArray("geosite:cn", "geosite:private"),
                ["skipFallback"] = true,
            };


            var proxyEntry = new JsonObject
            {
                ["address"] = proxyDns,
            };

            var servers = new JsonArray();
            if (IsFakeDnsActive(settings))
            {
                // FakeDNS must be first: it answers initial client lookups with fake IPs. The
                // real DNS entries below handle outbound-side resolution after sniffing recovers
                // the original domain.
                AddValue(servers, XrayConfigConstants.FakeDnsServerTag);
            }
            AddNode(servers, directEntry);
            AddNode(servers, proxyEntry);

            return new JsonObject
            {
                ["servers"]       = servers,
                ["queryStrategy"] = settings.DnsQueryStrategy,
                ["disableCache"]  = !settings.DnsCacheEnabled
            };
        }

        private static JsonArray CreateStringArray(params string[] values)
        {
            var array = new JsonArray();
            foreach (var value in values)
            {
                AddValue(array, value);
            }

            return array;
        }

        private static void AddNode(JsonArray array, JsonNode node)
        {
            array.Add(node);
        }

        private static void AddValue(JsonArray array, string value)
        {
            array.Add((JsonNode?)JsonValue.Create(value));
        }
    }
}
