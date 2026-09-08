using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.Tests;

public class AppSettingsRestoreStateTests
{
    [Fact]
    public void RestoreProxyStateOnStartup_DefaultsToFalse()
    {
        var settings = new AppSettings();
        Assert.False(settings.RestoreProxyStateOnStartup);
        Assert.Null(settings.LastRunningServerId);
        Assert.NotNull(settings.LastRunningAuxiliaryServerIds);
        Assert.Empty(settings.LastRunningAuxiliaryServerIds);
    }

    [Fact]
    public void AppSettings_RestoreState_RoundTripsJson()
    {
        var original = new AppSettings
        {
            RestoreProxyStateOnStartup = true,
            LastRunningServerId = "server-abc-123",
            LastRunningAuxiliaryServerIds = new List<string> { "aux-1", "aux-2" }
        };

        var json = JsonSerializer.Serialize(original, AppJsonSerializerContext.WriteReadable);
        var restored = JsonSerializer.Deserialize<AppSettings>(json, AppJsonSerializerContext.WriteReadable);

        Assert.NotNull(restored);
        Assert.True(restored.RestoreProxyStateOnStartup);
        Assert.Equal("server-abc-123", restored.LastRunningServerId);
        Assert.Equal(new[] { "aux-1", "aux-2" }, restored.LastRunningAuxiliaryServerIds);
    }
}
