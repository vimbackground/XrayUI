using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Windows.UI;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.ViewModels
{
    public partial class PersonalizeViewModel : ObservableObject
    {
        private readonly SettingsService _settings;
        private readonly IDialogService _dialogs;

        public IDialogService Dialogs => _dialogs;

        private int _initialLanguageIndex = -1;
        private bool _suppressLanguageRestartHint;
        private int _initialRegionIndex = -1;
        private bool _suppressRegionRestartHint;

        public event EventHandler? CloseRequested;

        public PersonalizeViewModel(IDialogService dialogs, SettingsService settings)
        {
            _dialogs = dialogs;
            _settings = settings;
            ShowLatencyInDetails = true;
            ShowAiUnlockInDetails = true;
            ShowGroupInDetails = true;
        }

        // ── Colors ────────────────────────────────────────────────────────────

        [ObservableProperty]
        public partial Color SsColor { get; set; }

        [ObservableProperty]
        public partial Color VlessColor { get; set; }

        [ObservableProperty]
        public partial Color VmessColor { get; set; }

        [ObservableProperty]
        public partial Color Hysteria2Color { get; set; }

        [ObservableProperty]
        public partial Color FallbackColor { get; set; }

        partial void OnSsColorChanged(Color value)
        {
            ProtocolColorStore.Ss = value;
            ProtocolColorStore.NotifyColorsChanged();
        }

        partial void OnVlessColorChanged(Color value)
        {
            ProtocolColorStore.Vless = value;
            ProtocolColorStore.NotifyColorsChanged();
        }

        partial void OnVmessColorChanged(Color value)
        {
            ProtocolColorStore.Vmess = value;
            ProtocolColorStore.NotifyColorsChanged();
        }

        partial void OnHysteria2ColorChanged(Color value)
        {
            ProtocolColorStore.Hysteria2 = value;
            ProtocolColorStore.NotifyColorsChanged();
        }

        partial void OnFallbackColorChanged(Color value)
        {
            ProtocolColorStore.Fallback = value;
            ProtocolColorStore.NotifyColorsChanged();
        }

        // ── Theme ─────────────────────────────────────────────────────────────
        [ObservableProperty]
        public partial int SelectedThemeIndex { get; set; }

        partial void OnSelectedThemeIndexChanged(int value)
        {
            var theme = value switch
            {
                0 => ElementTheme.Light,
                1 => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
            ThemeHelper.ApplyTheme(theme);
        }

        // ── Backdrop ──────────────────────────────────────────────────────────
        [ObservableProperty]
        public partial int SelectedBackdropIndex { get; set; }

        partial void OnSelectedBackdropIndexChanged(int value)
        {
            var backdrop = value switch
            {
                1 => "Acrylic",
                _ => "Mica",
            };
            ThemeHelper.ApplyBackdrop(backdrop);
        }

        // ── Internationalization ──────────────────────────────────────────────
        public IReadOnlyList<LanguageInfo> SupportedLanguages => LanguageHelper.SupportedLanguages;

        [ObservableProperty]
        public partial int SelectedLanguageIndex { get; set; }

        [ObservableProperty]
        public partial bool ShowRestartHint { get; set; }

        partial void OnSelectedLanguageIndexChanged(int value) => UpdateRestartHint();

        public static readonly string[] RegionCodes = ["cn", "ru", "ir"];

        [ObservableProperty]
        public partial int SelectedRegionIndex { get; set; }

        public string SelectedRegionCode =>
            (SelectedRegionIndex >= 0 && SelectedRegionIndex < RegionCodes.Length)
                ? RegionCodes[SelectedRegionIndex]
                : "cn";

        partial void OnSelectedRegionIndexChanged(int value) => UpdateRestartHint();

        private void UpdateRestartHint()
        {
            if (_suppressLanguageRestartHint && _suppressRegionRestartHint) return;
            var langChanged = _initialLanguageIndex >= 0 && SelectedLanguageIndex != _initialLanguageIndex;
            var regionChanged = _initialRegionIndex >= 0 && SelectedRegionIndex != _initialRegionIndex;
            ShowRestartHint = langChanged || regionChanged;
        }

        public async Task ApplyPendingChangesAsync()
        {
            var s = await _settings.LoadSettingsAsync();
            s.Language = LanguageHelper.TagAt(SelectedLanguageIndex);
            s.RoutingRegion = SelectedRegionCode;
            await _settings.SaveSettingsAsync(s);
        }

        // ── Display options ───────────────────────────────────────────────────
        private (bool Latency, bool AiUnlock, bool Group, bool OpenFilter) _displaySettingsBaseline;

        [ObservableProperty]
        public partial bool ShowDisplaySettingsUnsavedHint { get; set; }

        [ObservableProperty]
        public partial bool ShowLatencyInDetails { get; set; }

        [ObservableProperty]
        public partial bool ShowAiUnlockInDetails { get; set; }

        [ObservableProperty]
        public partial bool ShowGroupInDetails { get; set; }

        [ObservableProperty]
        public partial bool OpenServerFilterPanelOnStartup { get; set; }

        partial void OnShowLatencyInDetailsChanged(bool value) => UpdateDisplaySettingsHint();
        partial void OnShowAiUnlockInDetailsChanged(bool value) => UpdateDisplaySettingsHint();
        partial void OnShowGroupInDetailsChanged(bool value) => UpdateDisplaySettingsHint();
        partial void OnOpenServerFilterPanelOnStartupChanged(bool value) => UpdateDisplaySettingsHint();

        private void UpdateDisplaySettingsHint()
        {
            var changed = ShowLatencyInDetails != _displaySettingsBaseline.Latency
                       || ShowAiUnlockInDetails != _displaySettingsBaseline.AiUnlock
                       || ShowGroupInDetails != _displaySettingsBaseline.Group
                       || OpenServerFilterPanelOnStartup != _displaySettingsBaseline.OpenFilter;
            ShowDisplaySettingsUnsavedHint = changed;
        }

        // ── Commands ──────────────────────────────────────────────────────────
        [RelayCommand]
        private void ResetColors()
        {
            SsColor        = Color.FromArgb(255,  96, 165, 250);
            VlessColor     = Color.FromArgb(255,  52, 211, 153);
            VmessColor     = Color.FromArgb(255, 167, 139, 250);
            Hysteria2Color = Color.FromArgb(255, 251, 146,  60);
            FallbackColor  = Color.FromArgb(255, 148, 163, 184);
        }

        [RelayCommand]
        private async Task Done()
        {
            var s = await _settings.LoadSettingsAsync();
            ProtocolColorStore.SaveTo(s);
            s.ThemeSetting = ThemeHelper.CurrentTheme switch
            {
                ElementTheme.Light   => "Light",
                ElementTheme.Dark    => "Dark",
                _                    => "Default"
            };
            s.BackdropSetting = ThemeHelper.CurrentBackdrop;
            s.ShowLatencyInDetails = ShowLatencyInDetails;
            s.ShowAiUnlockInDetails = ShowAiUnlockInDetails;
            s.ShowGroupInDetails = ShowGroupInDetails;
            s.OpenServerFilterPanelOnStartup = OpenServerFilterPanelOnStartup;

            _displaySettingsBaseline = (ShowLatencyInDetails, ShowAiUnlockInDetails, ShowGroupInDetails, OpenServerFilterPanelOnStartup);
            ShowDisplaySettingsUnsavedHint = false;

            s.Language = LanguageHelper.TagAt(SelectedLanguageIndex);
            s.RoutingRegion = SelectedRegionCode;
            await _settings.SaveSettingsAsync(s);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // ── Initialization ────────────────────────────────────────────────────
        public void LoadFromStore()
        {
            SsColor        = ProtocolColorStore.Ss;
            VlessColor     = ProtocolColorStore.Vless;
            VmessColor     = ProtocolColorStore.Vmess;
            Hysteria2Color = ProtocolColorStore.Hysteria2;
            FallbackColor  = ProtocolColorStore.Fallback;

            SelectedThemeIndex = ThemeHelper.CurrentTheme switch
            {
                ElementTheme.Light => 0,
                ElementTheme.Dark  => 1,
                _                  => 2,
            };

            SelectedBackdropIndex = ThemeHelper.CurrentBackdrop == "Acrylic" ? 1 : 0;
        }

        public void LoadDisplayOptions(AppSettings settings)
        {
            ShowLatencyInDetails = settings.ShowLatencyInDetails;
            ShowAiUnlockInDetails = settings.ShowAiUnlockInDetails;
            ShowGroupInDetails = settings.ShowGroupInDetails;
            OpenServerFilterPanelOnStartup = settings.OpenServerFilterPanelOnStartup;
            _displaySettingsBaseline = (ShowLatencyInDetails, ShowAiUnlockInDetails, ShowGroupInDetails, OpenServerFilterPanelOnStartup);
        }

        public void LoadLanguage(AppSettings settings)
        {
            var index = LanguageHelper.IndexOf(settings.Language);
            _suppressLanguageRestartHint = true;
            SelectedLanguageIndex = index;
            _suppressLanguageRestartHint = false;
            _initialLanguageIndex = index;
        }

        public void LoadRegion(AppSettings settings)
        {
            var index = Array.IndexOf(RegionCodes, settings.RoutingRegion);
            if (index < 0) index = 0;
            _suppressRegionRestartHint = true;
            SelectedRegionIndex = index;
            _suppressRegionRestartHint = false;
            _initialRegionIndex = index;
        }
    }
}
