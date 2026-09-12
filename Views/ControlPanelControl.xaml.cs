using System;
using Windows.System;
using XrayUI.Helpers;

namespace XrayUI.Views
{
    public sealed partial class ControlPanelControl
    {
        private LogWindow? _logWindow;
        private ProxyRuntimeWindow? _runtimeWindow;
        private ProxyStatusWindow? _proxyStatusWindow;
        private CustomRulesWindow? _customRulesWindow;

        public ControlPanelViewModel ViewModel { get; set; } = null!;

        public ControlPanelControl()
        {
            this.InitializeComponent();
            ToolTipService.SetToolTip(PersonalizeButton, L.ControlPanel_Personalize);
            ToolTipService.SetToolTip(ModeSettingsButton, L.ControlPanel_ModeSettings);
            ToolTipService.SetToolTip(AppSettingsButton, L.ControlPanel_AppSettings);
            ToolTipService.SetToolTip(RuntimeButton, "代理动态信息");
            ToolTipService.SetToolTip(ProxyStatusButton, "已启用代理");
        }

        // Called by MainWindow after ViewModel is assigned (via x:Bind the property is set before Loaded)
        // We wire the event in the Loaded handler to be safe.
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowLogsRequested         += OnShowLogsRequested;
            ViewModel.ShowRuntimeRequested      += OnShowRuntimeRequested;
            ViewModel.ShowProxyStatusRequested  += OnShowProxyStatusRequested;
            if ((Application.Current as App)?.Window is XrayUI.MainWindow mainWindow)
                mainWindow.ViewModel.ServerDetail.ShowRuntimeRequested += OnShowRuntimeRequested;
            ViewModel.ShowCustomRulesRequested  += OnShowCustomRulesRequested;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowLogsRequested         -= OnShowLogsRequested;
            ViewModel.ShowRuntimeRequested      -= OnShowRuntimeRequested;
            ViewModel.ShowProxyStatusRequested  -= OnShowProxyStatusRequested;
            if ((Application.Current as App)?.Window is XrayUI.MainWindow mainWindow)
                mainWindow.ViewModel.ServerDetail.ShowRuntimeRequested -= OnShowRuntimeRequested;
            ViewModel.ShowCustomRulesRequested  -= OnShowCustomRulesRequested;
        }

        public void CloseLogWindow()
        {
            var logWindow = _logWindow;
            if (logWindow is null)
            {
                return;
            }

            _logWindow = null;
            logWindow.Close();
        }

        public void CloseCustomRulesWindow()
        {
            var w = _customRulesWindow;
            if (w is null)
            {
                return;
            }

            _customRulesWindow = null;
            w.Close();
        }

        public void CloseProxyStatusWindow()
        {
            var w = _proxyStatusWindow;
            if (w is null)
                return;

            _proxyStatusWindow = null;
            w.Close();
        }

        private void OnShowLogsRequested(object? sender, EventArgs e)
        {
            if (_logWindow is null)
            {
                _logWindow = new LogWindow(
                    ViewModel.XrayService,
                    ViewModel.SettingsService,
                    ViewModel.ReapplyRoutingAsync);
                _logWindow.Closed += (_, _) => _logWindow = null;
            }
            _logWindow.Activate();
        }

        private void OnShowRuntimeRequested(object? sender, EventArgs e)
        {
            if (_runtimeWindow is null)
            {
                if ((Application.Current as App)?.Window is not XrayUI.MainWindow main) return;
                _runtimeWindow = new ProxyRuntimeWindow(
                    ViewModel.XrayService,
                    () => main.ViewModel.ServerList.Servers,
                    () => main.ViewModel.ServerDetail.ActiveServer,
                    main.ViewModel.ServerList.GetGroupDisplayName,
                    () => ViewModel.LocalPort,
                    async () =>
                    {
                        if (ViewModel.StartStopCommand.CanExecute(null))
                            await ViewModel.StartStopCommand.ExecuteAsync(null);
                    },
                    server => main.ViewModel.ServerList.ToggleDedicatedPort(server));
                _runtimeWindow.Closed += (_, _) => _runtimeWindow = null;
            }
            else
            {
                _runtimeWindow.RefreshTargets();
            }
            _runtimeWindow.Activate();
        }

        private void OnShowProxyStatusRequested(object? sender, EventArgs e)
        {
            if ((Application.Current as App)?.Window is not XrayUI.MainWindow main) return;

            if (_proxyStatusWindow is null)
            {
                _proxyStatusWindow = new ProxyStatusWindow(
                    ViewModel.XrayService,
                    () => main.ViewModel.ServerList.Servers,
                    () => main.ViewModel.ServerDetail.ActiveServer,
                    main.ViewModel.ServerList.GetGroupDisplayName,
                    () => ViewModel.LocalPort,
                    async () =>
                    {
                        if (ViewModel.StartStopCommand.CanExecute(null))
                            await ViewModel.StartStopCommand.ExecuteAsync(null);
                    },
                    server => main.ViewModel.ServerList.ToggleDedicatedPort(server));
                _proxyStatusWindow.Closed += (_, _) => _proxyStatusWindow = null;
            }
            else
            {
                _proxyStatusWindow.RefreshProxies();
            }

            _proxyStatusWindow.Activate();
        }

        private void OnShowCustomRulesRequested(object? sender, CustomRulesViewModel vm)
        {
            if (_customRulesWindow is null)
            {
                if ((Application.Current as App)?.Window is not { } mainWindow)
                    return;

                _customRulesWindow = new CustomRulesWindow(mainWindow, vm);
                _customRulesWindow.Closed += (_, _) => _customRulesWindow = null;
            }
            _customRulesWindow.Activate();
        }
    }
}
