using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.Tests
{
    public class ProxyTelemetryTests
    {
        [Fact]
        public void Parse_AcceptedConnection_ExtractsTargetAndRoute()
        {
            var result = ProxyConnectionLogParser.Parse(
                "2026/09/11 12:00:00 accepted tcp:example.com:443 [mixed-in -> proxy]");

            Assert.NotNull(result);
            Assert.Equal(ProxyConnectionEventKind.Accepted, result.Kind);
            Assert.Equal("mixed-in", result.Inbound);
            Assert.Equal("proxy", result.Outbound);
            Assert.Equal("example.com:443", result.Target);
        }

        [Fact]
        public void Observe_TracksActiveTotalsFailuresAndRecentEvents()
        {
            var telemetry = new ProxyTelemetry();
            telemetry.Observe("2026/09/11 12:00:00 accepted tcp:example.com:443 [mixed-in -> proxy]");
            telemetry.Observe("2026/09/11 12:00:01 connection ends");
            telemetry.Observe("2026/09/11 12:00:02 outbound connection failed: timeout");

            var snapshot = telemetry.Snapshot();

            Assert.Equal(0, snapshot.ActiveConnections);
            Assert.Equal(1, snapshot.TotalConnections);
            Assert.Equal(1, snapshot.FailedConnections);
            Assert.Equal(3, snapshot.RecentConnections.Count);
            Assert.Equal(ProxyConnectionEventKind.Failed, snapshot.RecentConnections[0].Kind);
        }
    }
}
