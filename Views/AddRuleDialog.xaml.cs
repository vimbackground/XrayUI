using System;
using System.IO;
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

            // Wire up event handlers in code-behind (NOT via XAML markup).
            // The XAML-compiler-generated Connect path for SelectionChanged on a
            // ContentDialog can fail to fire under AOT in WinUI 3; explicit
            // subscription here is the AOT-safe pattern (cf. DialogService.cs).
            TypeComboBox.SelectionChanged         += TypeComboBox_SelectionChanged;
            BrowseFormatComboBox.SelectionChanged += BrowseFormatComboBox_SelectionChanged;
            BrowseButton.Click                    += BrowseButton_Click;

            if (existing != null)
            {
                Title             = "编辑规则";
                PrimaryButtonText = "保存";

                TypeComboBox.SelectedIndex = existing.Type switch
                {
                    "ip"      => 1,
                    "process" => 2,
                    _         => 0,
                };
                MatchTextBox.Text              = existing.Match;
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

            if (BrowsePanel is not null)
                BrowsePanel.Visibility = tag == "process" ? Visibility.Visible : Visibility.Collapsed;

            if (MatchTextBox is not null)
            {
                MatchTextBox.PlaceholderText = tag switch
                {
                    "ip"      => "192.168.0.0/16 或 geoip:cn",
                    "process" => "可手填进程名 / 路径 / 文件夹，或点浏览选取",
                    _         => "youtube.com 或 geosite:cn",
                };
            }

            if (HintTextBlock is not null)
            {
                HintTextBlock.Text = tag switch
                {
                    "ip"      => "支持 CIDR 与 geoip: 前缀",
                    "process" => "同名进程：匹配任意路径下的同名 exe；完整路径：只匹配此 exe；整个目录：匹配该目录下所有 exe",
                    _         => "支持精确匹配、regexp:、geosite: 前缀",
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
            BrowseButtonText.Text = isFolder ? "浏览文件夹..." : "浏览 exe...";
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
                MatchTextBox.Text = folder.Path.TrimEnd('\\') + "\\";
                return;
            }

            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };
            picker.FileTypeFilter.Add(".exe");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hostHwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            MatchTextBox.Text = format == "path" ? file.Path : file.Name;
        }

        private void OnPrimaryClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var match = MatchTextBox.Text?.Trim() ?? "";
            if (match.Length == 0)
            {
                ErrorText.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            var typeTag     = GetSelectedType();
            var outboundTag = GetSelectedOutboundTag();

            Result = new CustomRoutingRule
            {
                Type        = typeTag,
                Match       = match,
                OutboundTag = outboundTag,
                IsEnabled   = true,
            };
        }
    }
}
