using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using XrayUI.Helpers;
using XrayUI.Models;

namespace XrayUI.Views
{
    public sealed partial class AddRuleDialog
    {
        public CustomRoutingRule? Result { get; private set; }

        private readonly IntPtr _hostHwnd;

        public AddRuleDialog(IntPtr hostHwnd, CustomRoutingRule? existing = null)
        {
            _hostHwnd = hostHwnd;
            this.InitializeComponent();
            this.RequestedTheme = ThemeHelper.ActualTheme;

            Title             = L.AddRule_Title;
            PrimaryButtonText = L.Dialog_Add;
            CloseButtonText   = L.Dialog_Cancel;
            ErrorText.Text    = L.AddRule_ErrorEmpty;

            // Wire up event handlers in code-behind (NOT via XAML markup).
            // The XAML-compiler-generated Connect path for SelectionChanged on a
            // ContentDialog can fail to fire under AOT in WinUI 3; explicit
            // subscription here is the AOT-safe pattern (cf. DialogService.cs).
            TypeComboBox.SelectionChanged         += TypeComboBox_SelectionChanged;
            BrowseFormatComboBox.SelectionChanged += BrowseFormatComboBox_SelectionChanged;
            BrowseButton.Click                    += BrowseButton_Click;

            if (existing != null)
            {
                Title             = L.AddRule_EditTitle;
                PrimaryButtonText = L.Dialog_Save;

                TypeComboBox.SelectedIndex = existing.Type switch
                {
                    "ip"      => 1,
                    "process" => 2,
                    _         => 0,
                };
                if (existing.IsProcess)
                {
                    ProcessMatchesTextBox.Text = string.Join(Environment.NewLine, existing.EffectiveMatches);
                }
                else
                {
                    MatchTextBox.Text = existing.Match;
                }
                OutboundComboBox.SelectedIndex = existing.OutboundTag switch
                {
                    "direct" => 1,
                    "block"  => 2,
                    _        => 0,   // proxy
                };
            }

            // Sync BrowsePanel + placeholder + hint for the initial Type selection
            // (SelectionChanged may not fire for SelectedIndex set above pre-load).
            ApplyTypeUiState();
            ApplyBrowseFormatUiState();

            this.PrimaryButtonClick += OnPrimaryClick;
        }

        // ── Type changes: toggle BrowsePanel, swap placeholder + hint ─────────

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyTypeUiState();

        private void ApplyTypeUiState()
        {
            var tag = GetSelectedType();
            var isProcess = tag == "process";

            if (BrowsePanel is not null)
                BrowsePanel.Visibility = isProcess ? Visibility.Visible : Visibility.Collapsed;

            if (MatchTextBox is not null)
            {
                MatchTextBox.Visibility = isProcess ? Visibility.Collapsed : Visibility.Visible;
                MatchTextBox.PlaceholderText = tag switch
                {
                    "ip"      => L.AddRule_PlaceholderIp,
                    _         => L.AddRule_PlaceholderDomain,
                };
            }

            if (ProcessMatchesTextBox is not null)
            {
                ProcessMatchesTextBox.Visibility = isProcess ? Visibility.Visible : Visibility.Collapsed;
                ProcessMatchesTextBox.PlaceholderText = L.AddRule_PlaceholderProcess;
            }

            if (HintTextBlock is not null)
            {
                HintTextBlock.Text = tag switch
                {
                    "ip"      => L.AddRule_HintIp,
                    "process" => L.AddRule_HintProcess,
                    _         => L.AddRule_HintDomain,
                };
            }
        }

        // ── Browse format changes: swap button label between exe / folder ────

        private void BrowseFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyBrowseFormatUiState();

        private void ApplyBrowseFormatUiState()
        {
            if (BrowseButtonText is null) return;
            var isFolder = GetSelectedBrowseFormat() == "folder";
            BrowseButtonText.Text = isFolder ? L.AddRule_BrowseFolder : L.AddRule_BrowseExe;
            if (BrowseButtonIcon is not null)
            {
                BrowseButtonIcon.Glyph = isFolder ? "\uE8DA" : "\uE8E5";
            }
        }

        // Native AOT can be brittle around object-valued ComboBoxItem.Tag from XAML.
        // These lists are static, so index mapping keeps dialog state deterministic.
        private string GetSelectedType() => TypeComboBox.SelectedIndex switch
        {
            1 => "ip",
            2 => "process",
            _ => "domain",
        };

        private string GetSelectedBrowseFormat() => BrowseFormatComboBox.SelectedIndex switch
        {
            1 => "path",
            2 => "folder",
            _ => "name",
        };

        private string GetSelectedOutboundTag() => OutboundComboBox.SelectedIndex switch
        {
            1 => "direct",
            2 => "block",
            _ => "proxy",
        };

        // ── Browse click: file picker for name/path, folder picker for folder ──

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var format = GetSelectedBrowseFormat();

            if (format == "folder")
            {
                var folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.ComputerFolder,
                };
                folderPicker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, _hostHwnd);

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder is null) return;

                // Trailing backslash makes xray treat this as a folder match for
                // all executables under the directory.
                AppendProcessMatches([folder.Path.TrimEnd('\\') + "\\"]);
                return;
            }

            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };
            picker.FileTypeFilter.Add(".exe");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hostHwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files.Count == 0) return;

            AppendProcessMatches(files.Select(file => format == "path" ? file.Path : file.Name));
        }

        private void AppendProcessMatches(IEnumerable<string> additions)
        {
            // Split the existing text raw and normalize once over the merged set —
            // CustomRoutingRule.Normalize owns the trim/dedup contract.
            var matches = CustomRoutingRule.Normalize(
                SplitLines(ProcessMatchesTextBox.Text).Concat(additions));

            ProcessMatchesTextBox.Text = string.Join(Environment.NewLine, matches);
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private static string[] ParseProcessMatches(string? text)
            => CustomRoutingRule.Normalize(SplitLines(text));

        private static string[] SplitLines(string? text) => (text ?? "").Split(
            ["\r\n", "\n", "\r"],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private void OnPrimaryClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var typeTag = GetSelectedType();
            var processMatches = typeTag == "process"
                ? ParseProcessMatches(ProcessMatchesTextBox.Text)
                : [];
            var match = typeTag == "process"
                ? processMatches.FirstOrDefault() ?? ""
                : MatchTextBox.Text?.Trim() ?? "";

            if (match.Length == 0)
            {
                ErrorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            var outboundTag = GetSelectedOutboundTag();

            Result = new CustomRoutingRule
            {
                Type        = typeTag,
                Match       = match,
                Matches     = processMatches.Length > 1 ? processMatches.ToList() : null,
                OutboundTag = outboundTag,
                IsEnabled   = true,
            };
        }
    }
}
