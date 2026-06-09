using System;
using System.Threading.Tasks;
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

        private int _initialLanguageIndex = -1;
        private bool _suppressLanguageRestartHint;

        public event EventHandler? CloseRequested;
        public event EventHandler? PresetImported;

        public PersonalizeViewModel(IDialogService dialogs, SettingsService settings)
        {
            _dialogs = dialogs;
            _settings = settings;
            ShowLatencyInDetails = true;
            ShowAiUnlockInDetails = true;
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
        // Bound TwoWay to CommunityToolkit Segmented.SelectedIndex.
        // 0 = Light, 1 = Dark, 2 = System/Default

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

        partial void OnSelectedBackdropIndexChanged(int value) =>
            ThemeHelper.ApplyBackdrop(value == 1 ? "Acrylic" : "Mica");

        // ── Language ──────────────────────────────────────────────────────────

        /// <summary>Bound to the language ComboBox's ItemsSource — single source of truth
        /// for the dropdown contents. Adding a language is a one-line edit in LanguageHelper.</summary>
        public LanguageInfo[] SupportedLanguages => LanguageHelper.SupportedLanguages;

        [ObservableProperty]
        public partial int SelectedLanguageIndex { get; set; }

        partial void OnSelectedLanguageIndexChanged(int value)
        {
            // Hint visibility tracks divergence from the loaded value, not whether the
                // user touched the dropdown" — flipping back to the initial choice means
            // no restart is needed, so the hint should disappear too.
            if (!_suppressLanguageRestartHint && _initialLanguageIndex >= 0)
                ShowLanguageRestartHint = value != _initialLanguageIndex;
        }

        [ObservableProperty]
        public partial bool ShowLanguageRestartHint { get; set; }

        /// <summary>Persist the currently-selected language. Call right before <see cref="App.Restart"/>.</summary>
        public async Task ApplyLanguageAsync()
        {
            var tag = LanguageHelper.TagAt(SelectedLanguageIndex);
            var s = await _settings.LoadSettingsAsync();
            s.Language = tag;
            await _settings.SaveSettingsAsync(s);
        }

        [ObservableProperty]
        public partial bool ShowLatencyInDetails { get; set; }

        [ObservableProperty]
        public partial bool ShowAiUnlockInDetails { get; set; }

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

        public Task<string> ExportPresetAsync() =>
            new PresetExportService(_settings).ExportAsync();

        public static bool PresetExists() => PresetImportService.PresetExists();

        public async Task<PresetImportResult?> ConfirmAndImportPresetAsync()
        {
            var confirmed = await _dialogs.ShowConfirmationAsync(
                L.Confirm_ReplaceTitle,
                L.Confirm_ReplaceMsg,
                L.Dialog_Replace,
                L.Dialog_Cancel,
                isDanger: true);
            if (!confirmed)
                return null;

            var result = await new PresetImportService(_settings).ApplyAsync();
            PresetImported?.Invoke(this, EventArgs.Empty);
            return result;
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
            // Language doesn't take effect until the next process start, but Done still
            // persists it — otherwise the user would have to click the restart hint to
            // save at all, which is surprising compared to how Theme / Backdrop behave.
            s.Language = LanguageHelper.TagAt(SelectedLanguageIndex);
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
        }

        public void LoadLanguage(AppSettings settings)
        {
            // Assign through the field to bypass the setter's InfoBar side effect, then
            // record this as the baseline so divergence-from-baseline drives the hint.
            var index = LanguageHelper.IndexOf(settings.Language);
            _suppressLanguageRestartHint = true;
            SelectedLanguageIndex = index;
            _suppressLanguageRestartHint = false;
            _initialLanguageIndex = index;
        }
    }
}
