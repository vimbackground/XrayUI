using Xunit;
using XrayUI.Helpers;
using System.Net;
using System.Net.Sockets;

namespace XrayUI.Tests;

public class PortHelperTests
{
    [Fact]
    public void GenerateRandomAvailablePort_ReturnsPortInValidRange()
    {
        int min = 10000;
        int max = 65000;
        int port = PortHelper.GenerateRandomAvailablePort(min, max);

        Assert.InRange(port, min, max);
        Assert.True(PortHelper.IsPortAvailable(port));
    }

    [Fact]
    public void IsPortAvailable_InvalidPortReturnsFalse()
    {
        Assert.False(PortHelper.IsPortAvailable(0));
        Assert.False(PortHelper.IsPortAvailable(-1));
        Assert.False(PortHelper.IsPortAvailable(70000));
    }

    [Fact]
    public void GenerateRandomAvailablePort_GeneratesMultipleDistinctPorts()
    {
        var ports = new HashSet<int>();
        for (int i = 0; i < 20; i++)
        {
            int port = PortHelper.GenerateRandomAvailablePort(10000, 65000);
            ports.Add(port);
            Assert.True(PortHelper.IsPortAvailable(port));
        }

        // Within 20 random choices in a 55000-size range, there should be multiple unique ports
        Assert.True(ports.Count > 1);
    }

    [Fact]
    public async Task WaitForPortAvailableAsync_WaitsForListenerToRelease()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var wait = PortHelper.WaitForPortAvailableAsync(port, TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await Task.Delay(120, TestContext.Current.CancellationToken);
        listener.Stop();

        Assert.True(await wait);
    }

    [Fact]
    public async Task WaitForPortAvailableAsync_StopsAtItsTimeoutForOccupiedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        Assert.False(await PortHelper.WaitForPortAvailableAsync(port, TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GetTcpListenerProcessIds_ReturnsTheListenerOwner()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        Assert.Contains(Environment.ProcessId, PortHelper.GetTcpListenerProcessIds(port));
    }

    [Fact]
    public void PathsAreEqual_NormalisesButDoesNotBroadenProcessOwnership()
    {
        var expectedPath = Path.Combine("engine", "xray.exe");
        var equivalentPath = Path.Combine("engine", ".", "xray.exe");
        var otherPath = Path.Combine("engine", "other-xray.exe");

        Assert.True(PortHelper.PathsAreEqual(equivalentPath, expectedPath));
        Assert.False(PortHelper.PathsAreEqual(otherPath, expectedPath));
        Assert.False(PortHelper.PathsAreEqual(null, expectedPath));
    }
}
