using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.ViewModels
{
    public partial class AppSettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settings;
        private readonly StartupService _startupService;
        private readonly IDialogService _dialogs;
        private readonly IUpdateService _update;
        private readonly XrayService _xray;

        public event EventHandler? CloseRequested;
        public event EventHandler? PresetImported;
        public event EventHandler? ShowLogsRequested;

        public IDialogService Dialogs => _dialogs;

        public AppSettingsViewModel(
            SettingsService settings,
            StartupService startupService,
            IDialogService dialogs,
            IUpdateService update,
            XrayService xray)
        {
            _settings = settings;
            _startupService = startupService;
            _dialogs = dialogs;
            _update = update;
            _xray = xray;

            try
            {
                var v = AppVersion.Current;
                AppVersionText = $"v{v.Major}.{v.Minor}.{Math.Max(v.Build, 0)}";
            }
            catch
            {
                AppVersionText = "v1.0.0";
            }
        }

        // ── App Version & Update ──────────────────────────────────────────────
        [ObservableProperty]
        public partial string AppVersionText { get; set; } = "v1.0.0";

        // ── Local Port & LAN ───────────────────────────────────────────────────
        [ObservableProperty]
        public partial int LocalPort { get; set; } = 16891;

        [ObservableProperty]
        public partial string LocalPortText { get; set; } = "16891";

        partial void OnLocalPortTextChanged(string value)
        {
            if (int.TryParse(value?.Trim(), out var p) && p >= 1 && p <= 65535)
            {
                LocalPort = p;
            }
        }

        [RelayCommand]
        private void GenerateRandomPort()
        {
            int p = PortHelper.GenerateRandomAvailablePort(10000, 65000);
            LocalPort = p;
            LocalPortText = p.ToString();
        }

        [ObservableProperty]
        public partial bool AllowLanConnections { get; set; }

        // ── Startup ───────────────────────────────────────────────────────────
        [ObservableProperty]
        public partial bool IsStartupEnabled { get; set; }

        // ── Hotkeys ───────────────────────────────────────────────────────────
        [ObservableProperty]
        public partial string HotkeyToggleDisplay { get; set; } = "";

        [ObservableProperty]
        public partial string HotkeyRestoreDisplay { get; set; } = "";

        [ObservableProperty]
        public partial string HotkeySystemProxyDisplay { get; set; } = "";

        [ObservableProperty]
        public partial string HotkeyTunDisplay { get; set; } = "";

        [ObservableProperty]
        public partial string HotkeyRoutingDisplay { get; set; } = "";

        [ObservableProperty]
        public partial bool HotkeyToggleIsSet { get; set; }

        [ObservableProperty]
        public partial bool HotkeyRestoreIsSet { get; set; }

        [ObservableProperty]
        public partial bool HotkeySystemProxyIsSet { get; set; }

        [ObservableProperty]
        public partial bool HotkeyTunIsSet { get; set; }

        [ObservableProperty]
        public partial bool HotkeyRoutingIsSet { get; set; }

        public void SetHotkey(int id, uint mods, uint vk)
        {
            GlobalHotkeyStore.SetCombo(id, mods, vk);
            RefreshDisplay(id);
            GlobalHotkeyStore.NotifyHotkeysChanged();
        }

        public void ClearHotkey(int id) => SetHotkey(id, 0, 0);

        private void RefreshDisplay(int id)
        {
            var (mods, vk) = GlobalHotkeyStore.GetCombo(id);
            var text = GlobalHotkeyStore.FormatDisplay(mods, vk);
            var isSet = !string.IsNullOrEmpty(text);
            var display = isSet ? text : L.Personalize_HotkeyNotSet;

            switch (id)
            {
                case GlobalHotkeyStore.ToggleId:
                    HotkeyToggleIsSet = isSet;
                    HotkeyToggleDisplay = display;
                    break;
                case GlobalHotkeyStore.RestoreId:
                    HotkeyRestoreIsSet = isSet;
                    HotkeyRestoreDisplay = display;
                    break;
                case GlobalHotkeyStore.SystemProxyId:
                    HotkeySystemProxyIsSet = isSet;
                    HotkeySystemProxyDisplay = display;
                    break;
                case GlobalHotkeyStore.TunId:
                    HotkeyTunIsSet = isSet;
                    HotkeyTunDisplay = display;
                    break;
                case GlobalHotkeyStore.RoutingId:
                    HotkeyRoutingIsSet = isSet;
                    HotkeyRoutingDisplay = display;
                    break;
            }
        }

        // ── Initialization & State ───────────────────────────────────────────
        public async Task LoadStateAsync()
        {
            var s = await _settings.LoadSettingsAsync();
            LocalPort = s.LocalMixedPort;
            LocalPortText = LocalPort.ToString();
            AllowLanConnections = s.AllowLanConnections;

            try
            {
                IsStartupEnabled = _startupService.IsStartupEnabled();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] Failed to check startup: {ex.Message}");
            }

            foreach (var id in GlobalHotkeyStore.AllIds)
                RefreshDisplay(id);
        }

        // ── Import / Export ────────────────────────────────────────────────────
        public async Task<int> ImportServersBackupAsync(string json)
        {
            var servers = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListServerEntry);
            if (servers != null && servers.Count > 0)
            {
                var current = await _settings.LoadServersAsync();
                current.AddRange(servers);
                await _settings.SaveServersAsync(current);
                PresetImported?.Invoke(this, EventArgs.Empty);
                return servers.Count;
            }
            return 0;
        }

        public async Task<(int Imported, int Skipped)> ImportClashConfigAsync(string yamlText)
        {
            var parsed = ClashConfigParser.Parse(yamlText);
            if (parsed.Nodes.Count > 0)
            {
                var servers = await _settings.LoadServersAsync();
                servers.AddRange(parsed.Nodes);
                await _settings.SaveServersAsync(servers);
                PresetImported?.Invoke(this, EventArgs.Empty);
            }
            return (parsed.Nodes.Count, parsed.Skipped);
        }

        // ── Commands ──────────────────────────────────────────────────────────
        [RelayCommand]
        private void OpenDataFolder()
        {
            try
            {
                if (!Directory.Exists(AppPaths.DataDir))
                    Directory.CreateDirectory(AppPaths.DataDir);

                Process.Start(new ProcessStartInfo
                {
                    FileName = AppPaths.DataDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] OpenDataFolder failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ShowLogs()
        {
            ShowLogsRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task DoneAsync()
        {
            var s = await _settings.LoadSettingsAsync();

            if (LocalPort > 0 && LocalPort <= 65535)
            {
                s.LocalMixedPort = LocalPort;
            }
            s.AllowLanConnections = AllowLanConnections;

            GlobalHotkeyStore.SaveTo(s);
            await _settings.SaveSettingsAsync(s);

            try
            {
                _startupService.SetStartupEnabled(IsStartupEnabled);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] Failed to set startup: {ex.Message}");
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
