using System;
using XrayUI.Services;

namespace XrayUI.Views
{
    public sealed partial class PersonalizeControl
    {
        public PersonalizeViewModel ViewModel { get; set; } = null!;

        public PersonalizeControl()
        {
            this.InitializeComponent();
        }

        private async void ExportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportDir = await ViewModel.ExportPresetAsync();
                ShowInfo(InfoBarSeverity.Success, "导出成功", $"已导出至 {exportDir}");
            }
            catch (Exception ex)
            {
                ShowInfo(InfoBarSeverity.Error, "导出失败", ex.Message);
            }
        }

        private async void ImportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PersonalizeViewModel.PresetExists())
                {
                    ShowInfo(InfoBarSeverity.Warning, "未找到预置文件",
                        "请先准备 Import 文件夹下的 servers.json / settings.json。");
                    return;
                }

                var result = await ViewModel.ConfirmAndImportPresetAsync();
                if (result is null) return;

                var advanced = result.ImportedAdvancedRouting ? "、含高级路由" : "";
                ShowInfo(InfoBarSeverity.Success, "导入成功",
                    $"已导入 {result.ImportedServers} 个节点、{result.ImportedSubscriptions} 条订阅、{result.ImportedCustomRules} 条规则{advanced}。");
            }
            catch (Exception ex)
            {
                ShowInfo(InfoBarSeverity.Error, "导入失败", ex.Message);
            }
        }

        private void ShowInfo(InfoBarSeverity severity, string title, string message)
        {
            OperationInfoBar.Severity = severity;
            OperationInfoBar.Title = title;
            OperationInfoBar.Message = message;
            OperationInfoBar.IsOpen = true;
        }
    }
}
