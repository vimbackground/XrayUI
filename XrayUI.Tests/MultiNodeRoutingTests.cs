using System.Text.Json.Nodes;
using XrayUI.Models;
using XrayUI.Services;
using Xunit;

namespace XrayUI.Tests;

public class MultiNodeRoutingTests
{
    [Fact]
    public void Build_WhenMultiNodeDisabled_OnlyBuildsPrimaryInboundAndOutbound()
    {
        var mainServer = new ServerEntry
        {
            Name = "Hong Kong Main",
            Host = "hk.example.com",
            Port = 443,
            Protocol = "vless",
            Uuid = "11111111-1111-1111-1111-111111111111"
        };

        var auxServer = new ServerEntry
        {
            Name = "Japan Aux",
            Host = "jp.example.com",
            Port = 443,
            Protocol = "trojan",
            Password = "pass",
            DedicatedPort = 10809,
            IsDedicatedPortActive = true
        };

        var settings = new AppSettings
        {
            LocalMixedPort = 10808,
            EnableMultiNodeRouting = false, // Disabled
            RoutingMode = "smart"
        };

        var json = XrayConfigBuilder.Build(mainServer, settings, new[] { mainServer, auxServer });
        var doc = JsonNode.Parse(json)!.AsObject();

        var inbounds = doc["inbounds"]!.AsArray();
        var outbounds = doc["outbounds"]!.AsArray();
        var routing = doc["routing"]!.AsObject();
        var rules = routing["rules"]!.AsArray();

        // Only 1 inbound (mixed)
        Assert.Single(inbounds);
        Assert.Equal(10808, inbounds[0]!["port"]!.GetValue<int>());

        // Only primary outbound (proxy) + direct
        Assert.Contains(outbounds, o => o!["tag"]!.GetValue<string>() == "proxy");
        Assert.DoesNotContain(outbounds, o => o!["tag"]!.GetValue<string>().StartsWith("outbound_dedicated_"));

        // No dedicated inbound routing rules
        Assert.DoesNotContain(rules, r => r!["inboundTag"] != null && r["inboundTag"]!.ToString().Contains("inbound_dedicated_"));
    }

    [Fact]
    public void Build_WhenMultiNodeEnabled_BuildsDedicatedInboundsOutboundsAndRoutingRules()
    {
        var mainServer = new ServerEntry
        {
            Name = "Hong Kong Main",
            Host = "hk.example.com",
            Port = 443,
            Protocol = "vless",
            Uuid = "11111111-1111-1111-1111-111111111111"
        };

        var jpServer = new ServerEntry
        {
            Name = "Japan Dedicated",
            Host = "jp.example.com",
            Port = 443,
            Protocol = "trojan",
            Password = "pass",
            DedicatedPort = 10809,
            IsDedicatedPortActive = true
        };

        var sgServer = new ServerEntry
        {
            Name = "Singapore Dedicated",
            Host = "sg.example.com",
            Port = 443,
            Protocol = "shadowsocks",
            Password = "pass",
            DedicatedPort = 10810,
            IsDedicatedPortActive = true,
            AllowDedicatedLan = true
        };

        var inactiveServer = new ServerEntry
        {
            Name = "US Inactive",
            Host = "us.example.com",
            Port = 443,
            Protocol = "vless",
            DedicatedPort = 10811,
            IsDedicatedPortActive = false // Not active
        };

        var settings = new AppSettings
        {
            LocalMixedPort = 10808,
            EnableMultiNodeRouting = true, // Enabled
            RoutingMode = "smart"
        };

        var json = XrayConfigBuilder.Build(mainServer, settings, new[] { mainServer, jpServer, sgServer, inactiveServer });
        var doc = JsonNode.Parse(json)!.AsObject();

        var inbounds = doc["inbounds"]!.AsArray();
        var outbounds = doc["outbounds"]!.AsArray();
        var rules = doc["routing"]!["rules"]!.AsArray();

        // 3 inbounds: main (10808) + JP (10809) + SG (10810)
        Assert.Equal(3, inbounds.Count);
        Assert.Contains(inbounds, i => i!["port"]!.GetValue<int>() == 10808 && i["listen"]!.GetValue<string>() == "127.0.0.1");
        Assert.Contains(inbounds, i => i!["port"]!.GetValue<int>() == 10809 && i["listen"]!.GetValue<string>() == "127.0.0.1");
        Assert.Contains(inbounds, i => i!["port"]!.GetValue<int>() == 10810 && i["listen"]!.GetValue<string>() == "0.0.0.0");

        // Outbounds contains proxy (main), outbound_dedicated_{jp.Id}, outbound_dedicated_{sg.Id}
        Assert.Contains(outbounds, o => o!["tag"]!.GetValue<string>() == "proxy");
        Assert.Contains(outbounds, o => o!["tag"]!.GetValue<string>() == $"outbound_dedicated_{jpServer.Id}");
        Assert.Contains(outbounds, o => o!["tag"]!.GetValue<string>() == $"outbound_dedicated_{sgServer.Id}");
        Assert.DoesNotContain(outbounds, o => o!["tag"]!.GetValue<string>() == $"outbound_dedicated_{inactiveServer.Id}");

        // Top rules in routing table must be the 1:1 dedicated port rules
        var rule0 = rules[0]!.AsObject();
        var rule1 = rules[1]!.AsObject();

        Assert.Equal($"outbound_dedicated_{jpServer.Id}", rule0["outboundTag"]!.GetValue<string>());
        Assert.Equal("inbound_dedicated_10809", rule0["inboundTag"]![0]!.GetValue<string>());

        Assert.Equal($"outbound_dedicated_{sgServer.Id}", rule1["outboundTag"]!.GetValue<string>());
        Assert.Equal("inbound_dedicated_10810", rule1["inboundTag"]![0]!.GetValue<string>());
    }
}
