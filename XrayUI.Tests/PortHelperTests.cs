using Xunit;
using XrayUI.Helpers;

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
}
