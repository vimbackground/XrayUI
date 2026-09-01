using System;
using System.IO;

namespace XrayUI.Helpers
{
    public static class AppPaths
    {
        public static string DataDir { get; } = Path.Combine(AppContext.BaseDirectory, "Data");

        // Keep LocalAppDataDir pointing to DataDir for full backward compatibility across the codebase
        public static string LocalAppDataDir => DataDir;

        public static string UpdatesDir { get; } = Path.Combine(DataDir, "Updates");

        public static string SettingsJsonPath { get; } = Path.Combine(DataDir, "settings.json");

        static AppPaths()
        {
            try
            {
                if (!Directory.Exists(DataDir))
                {
                    Directory.CreateDirectory(DataDir);

                    // Smooth migration: if local Data/ directory was just created, check if %LOCALAPPDATA%\XrayUI exists
                    var oldDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "XrayUI");

                    if (Directory.Exists(oldDir))
                    {
                        var oldSettings = Path.Combine(oldDir, "settings.json");
                        var oldServers = Path.Combine(oldDir, "servers.json");
                        var newSettings = Path.Combine(DataDir, "settings.json");
                        var newServers = Path.Combine(DataDir, "servers.json");

                        if (File.Exists(oldSettings) && !File.Exists(newSettings))
                        {
                            File.Copy(oldSettings, newSettings, overwrite: false);
                        }

                        if (File.Exists(oldServers) && !File.Exists(newServers))
                        {
                            File.Copy(oldServers, newServers, overwrite: false);
                        }
                    }
                }
            }
            catch
            {
                // Best effort initialization
            }
        }
    }
}
