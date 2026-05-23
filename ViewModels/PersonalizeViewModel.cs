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

        private Color _ssColor;
        private Color _vlessColor;
        private Color _vmessColor;
        private Color _hysteria2Color;
        private Color _fallbackColor;

        private int _selectedThemeIndex;
        private int _selectedBackdropIndex;
        private int _selectedLanguageIndex;
        private int _initialLanguageIndex = -1;
        private bool _showLanguageRestartHint;
        private bool _showLatencyInDetails = true;
        private bool _showAiUnlockInDetails = true;

        public event EventHandler? CloseRequested;
        public event EventHandler? PresetImported;

        public PersonalizeViewModel(IDialogService dialogs, SettingsService settings)
        {
            _dialogs = dialogs;
            _settings = settings;
        }

        // ── Colors ────────────────────────────────────────────────────────────

        public Color SsColor
        {
            get => _ssColor;
            set
            {
                if (SetProperty(ref _ssColor, value))
                {
                    ProtocolColorStore.Ss = value;
                    ProtocolColorStore.NotifyColorsChanged();
                }
            }
        }

        public Color VlessColor
        {
            get => _vlessColor;
            set
            {
                if (SetProperty(ref _vlessColor, value))
                {
                    ProtocolColorStore.Vless = value;
                    ProtocolColorStore.NotifyColorsChanged();
                }
            }
        }

        public Color VmessColor
        {
            get => _vmessColor;
            set
            {
                if (SetProperty(ref _vmessColor, value))
                {
                    ProtocolColorStore.Vmess = value;
                    ProtocolColorStore.NotifyColorsChanged();
                }
            }
        }

        public Color Hysteria2Color
        {
            get => _hysteria2Color;
            set
            {
                if (SetProperty(ref _hysteria2Color, value))
                {
                    ProtocolColorStore.Hysteria2 = value;
                    ProtocolColorStore.NotifyColorsChanged();
                }
            }
        }

        public Color FallbackColor
        {
            get => _fallbackColor;
            set
            {
                if (SetProperty(ref _fallbackColor, value))
                {
                    ProtocolColorStore.Fallback = value;
                    ProtocolColorStore.NotifyColorsChanged();
                }
            }
        }

        // ── Theme ─────────────────────────────────────────────────────────────
        // Bound TwoWay to CommunityToolkit Segmented.SelectedIndex.
        // 0 = Light, 1 = Dark, 2 = System/Default

        public int SelectedThemeIndex
        {
            get => _selectedThemeIndex;
            set
            {
                if (!SetProperty(ref _selectedThemeIndex, value)) return;
                var theme = value switch
                {
                    0 => ElementTheme.Light,
                    1 => ElementTheme.Dark,
                    _ => ElementTheme.Default,
                };
                ThemeHelper.ApplyTheme(theme);
            }
        }

        // ── Backdrop ──────────────────────────────────────────────────────────

        public int SelectedBackdropIndex
        {
            get => _selectedBackdropIndex;
            set
            {
                if (!SetProperty(ref _selectedBackdropIndex, value)) return;
                ThemeHelper.ApplyBackdrop(value == 1 ? "Acrylic" : "Mica");
            }
        }

        // ── Language ──────────────────────────────────────────────────────────

        /// <summary>Bound to the language ComboBox's ItemsSource — single source of truth
        /// for the dropdown contents. Adding a language is a one-line edit in LanguageHelper.</summary>
        public LanguageInfo[] SupportedLanguages => LanguageHelper.SupportedLanguages;

        public int SelectedLanguageIndex
        {
            get => _selectedLanguageIndex;
            set
            {
                if (!SetProperty(ref _selectedLanguageIndex, value)) return;
                // Hint visibility tracks divergence from the loaded value, not "has the
                // user touched the dropdown" — flipping back to the initial choice means
                // no restart is needed, so the hint should disappear too. The -1 guard
                // suppresses the side effect during the initial LoadLanguage call.
                if (_initialLanguageIndex >= 0)
                    ShowLanguageRestartHint = value != _initialLanguageIndex;
            }
        }

        public bool ShowLanguageRestartHint
        {
            get => _showLanguageRestartHint;
            set => SetProperty(ref _showLanguageRestartHint, value);
        }

        /// <summary>Persist the currently-selected language. Call right before <see cref="App.Restart"/>.</summary>
        public async Task ApplyLanguageAsync()
        {
            var tag = LanguageHelper.TagAt(_selectedLanguageIndex);
            var s = await _settings.LoadSettingsAsync();
            s.Language = tag;
            await _settings.SaveSettingsAsync(s);
        }

        public bool ShowLatencyInDetails
        {
            get => _showLatencyInDetails;
            set => SetProperty(ref _showLatencyInDetails, value);
        }

        public bool ShowAiUnlockInDetails
        {
            get => _showAiUnlockInDetails;
            set => SetProperty(ref _showAiUnlockInDetails, value);
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
            s.Language = LanguageHelper.TagAt(_selectedLanguageIndex);
            await _settings.SaveSettingsAsync(s);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // ── Initialization ────────────────────────────────────────────────────

        public void LoadFromStore()
        {
            _ssColor        = ProtocolColorStore.Ss;
            _vlessColor     = ProtocolColorStore.Vless;
            _vmessColor     = ProtocolColorStore.Vmess;
            _hysteria2Color = ProtocolColorStore.Hysteria2;
            _fallbackColor  = ProtocolColorStore.Fallback;

            OnPropertyChanged(nameof(SsColor));
            OnPropertyChanged(nameof(VlessColor));
            OnPropertyChanged(nameof(VmessColor));
            OnPropertyChanged(nameof(Hysteria2Color));
            OnPropertyChanged(nameof(FallbackColor));

            _selectedThemeIndex = ThemeHelper.CurrentTheme switch
            {
                ElementTheme.Light => 0,
                ElementTheme.Dark  => 1,
                _                  => 2,
            };
            OnPropertyChanged(nameof(SelectedThemeIndex));

            _selectedBackdropIndex = ThemeHelper.CurrentBackdrop == "Acrylic" ? 1 : 0;
            OnPropertyChanged(nameof(SelectedBackdropIndex));
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
            _selectedLanguageIndex = index;
            _initialLanguageIndex = index;
            OnPropertyChanged(nameof(SelectedLanguageIndex));
        }
    }
}
