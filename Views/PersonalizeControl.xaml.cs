using System;
using Microsoft.UI.Xaml.Automation;
using Windows.Storage;
using Windows.Storage.Pickers;
using XrayUI.Helpers;
using XrayUI.Services;

namespace XrayUI.Views
{
    public sealed partial class PersonalizeControl
    {
        public PersonalizeViewModel ViewModel { get; set; } = null!;

        public PersonalizeControl()
        {
            this.InitializeComponent();

            AutomationProperties.SetName(AppLanguageExpander, L.Personalize_LanguageRegionExpanderAutomationName);
            AutomationProperties.SetName(ExportPresetButton, L.Personalize_ExportTooltip);
            AutomationProperties.SetName(ImportDropDownButton, L.Personalize_ImportTooltip);
        }

        private async void ExportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportDir = await ViewModel.ExportPresetAsync();
                ShowInfo(InfoBarSeverity.Success,
                    L.Personalize_ExportSuccess,
                    Loc.Format("Personalize_ExportSuccessMsgFmt", exportDir));
            }
            catch (Exception ex)
            {
                ShowInfo(InfoBarSeverity.Error, L.Error_ExportFailed, ex.Message);
            }
        }

        private async void ImportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PersonalizeViewModel.PresetExists())
                {
                    ShowInfo(InfoBarSeverity.Warning,
                        L.Personalize_PresetMissingTitle,
                        L.Personalize_PresetMissingMsg);
                    return;
                }

                var result = await ViewModel.ConfirmAndImportPresetAsync();
                if (result is null) return;

                var advanced = result.ImportedAdvancedRouting ? L.Personalize_ImportAdvancedSuffix : "";
                ShowInfo(InfoBarSeverity.Success,
                    L.Personalize_ImportSuccess,
                    Loc.Format("Personalize_ImportSuccessMsg",
                        result.ImportedServers,
                        result.ImportedSubscriptions,
                        result.ImportedCustomRules,
                        advanced));
            }
            catch (Exception ex)
            {
                ShowInfo(InfoBarSeverity.Error, L.Personalize_ImportFailed, ex.Message);
            }
        }

        private async void ImportClashConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.ComputerFolder,
                };
                picker.FileTypeFilter.Add(".yaml");
                picker.FileTypeFilter.Add(".yml");

                // Unpackaged app: the picker must be associated with the host window's HWND.
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(ThemeHelper.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file is null) return;

                var text = await FileIO.ReadTextAsync(file);
                var (imported, skipped) = await ViewModel.ImportClashConfigAsync(text);

                if (imported == 0)
                {
                    ShowInfo(InfoBarSeverity.Warning,
                        L.Personalize_ClashImportNoNodesTitle,
                        L.Personalize_ClashImportNoNodesMsg);
                    return;
                }

                ShowInfo(InfoBarSeverity.Success,
                    L.Personalize_ClashImportSuccess,
                    Loc.Format("Personalize_ClashImportSuccessMsg", imported, skipped));
            }
            catch (Exception ex)
            {
                ShowInfo(InfoBarSeverity.Error, L.Personalize_ClashImportFailed, ex.Message);
            }
        }

        private void ShowInfo(InfoBarSeverity severity, string title, string message)
        {
            OperationInfoBar.Severity = severity;
            OperationInfoBar.Title = title;
            OperationInfoBar.Message = message;
            OperationInfoBar.IsOpen = true;
        }

        private async void LanguageRestartButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplyPendingChangesAsync();
            App.Restart();
        }
    }
}
