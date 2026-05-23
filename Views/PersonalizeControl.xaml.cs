using System;
using Microsoft.UI.Xaml.Automation;
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

            AutomationProperties.SetName(ExportPresetButton, L.Personalize_ExportTooltip);
            AutomationProperties.SetName(ImportPresetButton, L.Personalize_ImportTooltip);
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

        private void ShowInfo(InfoBarSeverity severity, string title, string message)
        {
            OperationInfoBar.Severity = severity;
            OperationInfoBar.Title = title;
            OperationInfoBar.Message = message;
            OperationInfoBar.IsOpen = true;
        }

        private async void LanguageRestartButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplyLanguageAsync();
            App.Restart();
        }
    }
}
