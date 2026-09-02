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
        public static string ServersJsonPath { get; } = Path.Combine(DataDir, "servers.json");
        public static string ServersJson => ServersJsonPath;

        static AppPaths()
        {
            try
            {
                if (!Directory.Exists(DataDir))
                {
                    Directory.CreateDirectory(DataDir);
                }
            }
            catch
            {
                // Best effort directory creation
            }
        }
    }
}
