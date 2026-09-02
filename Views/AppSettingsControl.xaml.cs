using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.Services;
using XrayUI.ViewModels;

namespace XrayUI.Views
{
    public sealed partial class AppSettingsControl
    {
        public AppSettingsViewModel ViewModel { get; set; } = null!;

        public AppSettingsControl()
        {
            this.InitializeComponent();
        }

        private async void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            int id = GlobalHotkeyStore.ToggleId;
            if (ReferenceEquals(sender, RestoreHotkeyButton)) id = GlobalHotkeyStore.RestoreId;
            else if (ReferenceEquals(sender, SystemProxyHotkeyButton)) id = GlobalHotkeyStore.SystemProxyId;
            else if (ReferenceEquals(sender, TunHotkeyButton)) id = GlobalHotkeyStore.TunId;
            else if (ReferenceEquals(sender, RoutingHotkeyButton)) id = GlobalHotkeyStore.RoutingId;

            var (mods, vk) = GlobalHotkeyStore.GetCombo(id);
            var result = await ViewModel.Dialogs.ShowHotkeyRecorderDialogAsync("快捷键设置", mods, vk);
            if (result is null) return; // cancelled

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(ThemeHelper.MainWindow);

            if (result.Value.cleared)
            {
                HotkeyInterop.UnregisterHotKey(hWnd, id);
                ViewModel.ClearHotkey(id);
                ShowHotkeySaved("快捷键已清除，点击下方「完成」保存");
                return;
            }

            HotkeyInterop.UnregisterHotKey(hWnd, id);
            if (!HotkeyInterop.RegisterHotKey(hWnd, id, result.Value.mods | GlobalHotkeyStore.ModNoRepeat, result.Value.vk))
            {
                await ViewModel.Dialogs.ShowErrorAsync("快捷键设置失败", "该组合键已被系统或其他程序占用，请更换一个组合。");
                GlobalHotkeyStore.NotifyHotkeysChanged();
                return;
            }

            ViewModel.SetHotkey(id, result.Value.mods, result.Value.vk);
            ShowHotkeySaved("快捷键已生效，点击下方「完成」保存");
        }

        private void ShowHotkeySaved(string message)
        {
            HotkeySavedInfoBar.Message = message;
            HotkeySavedInfoBar.IsOpen = true;
        }

        private async void ExportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var savePicker = new FileSavePicker();
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(ThemeHelper.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("JSON 文件", new[] { ".json" });
                savePicker.SuggestedFileName = $"xrayui_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                var file = await savePicker.PickSaveFileAsync();
                if (file is null) return;

                var serversPath = AppPaths.ServersJson;
                if (!File.Exists(serversPath))
                {
                    ShowOperationMessage("没有可导出的节点配置", InfoBarSeverity.Warning);
                    return;
                }

                var content = await File.ReadAllTextAsync(serversPath);
                await File.WriteAllTextAsync(file.Path, content);
                ShowOperationMessage("配置已成功导出", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowOperationMessage($"导出失败: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async void ImportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openPicker = new FileOpenPicker();
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(ThemeHelper.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".json");

                var file = await openPicker.PickSingleFileAsync();
                if (file is null) return;

                var json = await File.ReadAllTextAsync(file.Path);
                var count = await ViewModel.ImportServersBackupAsync(json);
                if (count == 0)
                {
                    ShowOperationMessage("文件中未找到有效的节点配置", InfoBarSeverity.Warning);
                    return;
                }

                ShowOperationMessage($"已成功导入 {count} 个节点", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowOperationMessage($"导入失败: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async void ImportClashConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openPicker = new FileOpenPicker();
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(ThemeHelper.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".yaml");
                openPicker.FileTypeFilter.Add(".yml");

                var file = await openPicker.PickSingleFileAsync();
                if (file is null) return;

                var yaml = await File.ReadAllTextAsync(file.Path);
                var (imported, skipped) = await ViewModel.ImportClashConfigAsync(yaml);

                if (imported == 0)
                {
                    ShowOperationMessage("文件中未解析到有效代理节点", InfoBarSeverity.Warning);
                    return;
                }

                ShowOperationMessage($"已从 Clash 配置中导入 {imported} 个节点", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowOperationMessage($"解析 Clash 配置失败: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void ShowOperationMessage(string message, InfoBarSeverity severity)
        {
            OperationInfoBar.Message = message;
            OperationInfoBar.Severity = severity;
            OperationInfoBar.IsOpen = true;
        }
    }
}
