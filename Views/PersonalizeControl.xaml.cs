using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using XrayUI.Helpers;
using XrayUI.ViewModels;

namespace XrayUI.Views
{
    public sealed partial class PersonalizeControl
    {
        public PersonalizeViewModel ViewModel { get; set; } = null!;

        public string OnOffLabel(bool isOn) => isOn ? L.Dialog_On : L.Dialog_Off;

        public PersonalizeControl()
        {
            this.InitializeComponent();
            AutomationProperties.SetName(AppLanguageExpander, L.Personalize_LanguageRegionExpanderAutomationName);
        }

        private async void LanguageRestartButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplyPendingChangesAsync();
            App.Restart();
        }
    }
}
