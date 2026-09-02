using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using XrayUI.Helpers;
using XrayUI.Models;

namespace XrayUI.Services
{
    public static class FirstRunWizardService
    {
        public static async Task RunWizardIfNeededAsync(IDialogService dialogs, SettingsService settings)
        {
            var dataDir = AppPaths.DataDir;
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            var settingsPath = AppPaths.SettingsJsonPath;
            var serversPath = AppPaths.ServersJsonPath;

            // If settings.json or servers.json already exists in Data/, the app is already initialized
            if (File.Exists(settingsPath) || File.Exists(serversPath))
            {
                return;
            }

            try
            {
                var oldDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "XrayUI");

                var oldSettings = Path.Combine(oldDir, "settings.json");
                var oldServers = Path.Combine(oldDir, "servers.json");
                bool hasLegacy = Directory.Exists(oldDir) && (File.Exists(oldSettings) || File.Exists(oldServers));

                if (hasLegacy)
                {
                    bool shouldImport = await dialogs.ShowFirstRunImportPromptAsync("检测到本机原版 XrayUI 配置数据");
                    if (shouldImport)
                    {
                        int randomPort = PortHelper.GenerateRandomAvailablePort(10000, 65000);
                        var portResult = await dialogs.ShowEditPortDialogAsync(randomPort, false);
                        int finalPort = portResult.HasValue ? portResult.Value.port : randomPort;
                        bool allowLan = portResult.HasValue && portResult.Value.allowLan;

                        if (File.Exists(oldSettings))
                        {
                            try
                            {
                                var json = await File.ReadAllTextAsync(oldSettings);
                                var importedSettings = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppSettings) ?? new AppSettings();
                                importedSettings.LocalMixedPort = finalPort;
                                importedSettings.AllowLanConnections = allowLan;
                                await settings.SaveSettingsAsync(importedSettings);
                            }
                            catch
                            {
                                var fallback = new AppSettings
                                {
                                    LocalMixedPort = finalPort,
                                    AllowLanConnections = allowLan
                                };
                                await settings.SaveSettingsAsync(fallback);
                            }
                        }
                        else
                        {
                            var s = new AppSettings
                            {
                                LocalMixedPort = finalPort,
                                AllowLanConnections = allowLan
                            };
                            await settings.SaveSettingsAsync(s);
                        }

                        if (File.Exists(oldServers))
                        {
                            try
                            {
                                var json = await File.ReadAllTextAsync(oldServers);
                                var list = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListServerEntry) ?? new List<ServerEntry>();
                                await settings.SaveServersAsync(list);
                            }
                            catch
                            {
                                await settings.SaveServersAsync(new List<ServerEntry>());
                            }
                        }
                        else
                        {
                            await settings.SaveServersAsync(new List<ServerEntry>());
                        }
                        return;
                    }
                }

                // If user chose "不需要" OR no legacy config was found:
                // Generate a clean configuration with random available port >= 10000
                int cleanPort = PortHelper.GenerateRandomAvailablePort(10000, 65000);
                var cleanSettings = new AppSettings
                {
                    LocalMixedPort = cleanPort
                };
                await settings.SaveSettingsAsync(cleanSettings);
                await settings.SaveServersAsync(new List<ServerEntry>());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirstRunWizard] Exception during first run wizard: {ex}");
            }
        }
    }
}
