using System;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using CommunityToolkit.Mvvm.Input;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.ViewModels;

namespace XrayUI.Views
{
    public sealed partial class ServerListControl
    {
        public ServerListViewModel ViewModel { get; set; } = null!;
        public IAsyncRelayCommand? SwitchToSelectedServerCommand { get; set; }
        public IAsyncRelayCommand<ServerEntry?>? ToggleServerConnectionCommand { get; set; }
        public IAsyncRelayCommand<ServerEntry?>? SetPrimaryProxyCommand { get; set; }

        public ServerListControl()
        {
            this.InitializeComponent();

            // Localize attached properties that x:Uid does not address cleanly.
            AutomationProperties.SetName(FilterToggle, L.ServerList_FilterTooltip);
            AutomationProperties.SetName(TestLatencyButton, L.ServerList_TestLatencyTooltip);
            AutomationProperties.SetName(SortButton,   L.ServerList_SortTooltip);
            ToolTipService.SetToolTip(FilterToggle, L.ServerList_FilterTooltip);
            ToolTipService.SetToolTip(TestLatencyButton, L.ServerList_TestLatencyTooltip);
            ToolTipService.SetToolTip(SortActiveItem,  L.ServerList_SortActiveHint);
        }

        private void ServerSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            var query = sender.Text.Trim();
            ViewModel.SearchQuery = query;

            if (string.IsNullOrEmpty(query))
            {
                sender.ItemsSource = null;
                return;
            }

            sender.ItemsSource = ViewModel.SearchServers(query);
        }

        private void ServerSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is ServerEntry server)
            {
                ViewModel.SelectedServer = server;
                sender.Text = server.Name;
            }
        }

        private void ServerSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is ServerEntry chosenServer)
            {
                ViewModel.SelectedServer = chosenServer;
                return;
            }

            var query = args.QueryText?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            var match = ViewModel.Servers.FirstOrDefault(s =>
                string.Equals(s.Name, query, StringComparison.OrdinalIgnoreCase));

            match ??= ViewModel.Servers.FirstOrDefault(s =>
                !string.IsNullOrEmpty(s.Name) &&
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                ViewModel.SelectedServer = match;
                sender.Text = match.Name;
            }
        }

        private async void ServersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            await ViewModel.SaveOrderAsync();
        }

        private bool _initialScrollDone;

        private void ServersListView_Loaded(object sender, RoutedEventArgs e)
        {
            QueueInitialScroll();
        }

        private void ServersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            QueueInitialScroll();
            ViewModel.SetSelectedServers(ServersListView.SelectedItems.OfType<ServerEntry>().ToArray());
        }

        private void QueueInitialScroll()
        {
            if (_initialScrollDone || ServersListView.SelectedItem is null)
                return;

            ServersListView.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                () =>
                {
                    // Startup can restore the selection before the virtualized list has
                    // completed its first layout pass (more likely with Native AOT's
                    // faster startup), where ScrollIntoView silently no-ops. Bail without
                    // consuming the one-time scroll until the control is loaded and the
                    // current item is known — the next Loaded/SelectionChanged retries.
                    if (_initialScrollDone ||
                        !ServersListView.IsLoaded ||
                        ServersListView.SelectedItem is not { } selectedItem)
                    {
                        return;
                    }

                    ServersListView.UpdateLayout();
                    ServersListView.ScrollIntoView(
                        selectedItem,
                        ScrollIntoViewAlignment.Leading);
                    _initialScrollDone = true;
                });
        }

        private void ActiveBadge_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is UIElement element)
                element.CenterPoint = new Vector3((float)e.NewSize.Width / 2f, (float)e.NewSize.Height / 2f, 0f);
        }

        private async void ServerItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            if (element.DataContext is not ServerEntry server)
                return;

            e.Handled = true;

            if (ToggleServerConnectionCommand is not null)
            {
                await ToggleServerConnectionCommand.ExecuteAsync(server);
                return;
            }

            if (!ReferenceEquals(ViewModel.SelectedServer, server))
                ViewModel.SelectedServer = server;

            var command = SwitchToSelectedServerCommand;
            if (command is null || !command.CanExecute(null))
                return;

            await command.ExecuteAsync(null);
        }

        private async void ProxyActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: ServerEntry server })
                return;

            if (server.UsesAuxiliaryProxyAction)
            {
                await ViewModel.ToggleDedicatedPort(server);
                return;
            }

            if (ToggleServerConnectionCommand is null) return;
            await ToggleServerConnectionCommand.ExecuteAsync(server);
        }

        private void ServerItem_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
        {
            if (ViewModel.HasMultipleSelectedServers)
            {
                e.Handled = true;
                return;
            }

            if (sender is not FrameworkElement element)
            {
                e.Handled = true;
                return;
            }

            if (!ReferenceEquals(element.DataContext, ViewModel.SelectedServer))
            {
                e.Handled = true;
                return;
            }

            var flyout = CreateSelectedServerContextFlyout();

            if (e.TryGetPosition(element, out Point point))
            {
                flyout.ShowAt(element, new FlyoutShowOptions
                {
                    Position = point
                });
            }
            else
            {
                flyout.ShowAt(element);
            }

            e.Handled = true;
        }

        private MenuFlyout CreateSelectedServerContextFlyout()
        {
            var flyout = new MenuFlyout();
            var server = ViewModel.SelectedServer;

            if (server != null)
            {
                var primaryItem = CreateMenuItem("设为主代理", "");
                primaryItem.Click += async (_, _) =>
                {
                    if (SetPrimaryProxyCommand is not null)
                        await SetPrimaryProxyCommand.ExecuteAsync(server);
                };
                flyout.Items.Add(primaryItem);

                if (ViewModel.EnableMultiNodeRouting && server.IsActive != true)
                {
                    var isDedicatedActive = server.IsDedicatedPortActive;
                    var toggleDedicatedText = isDedicatedActive ? "停止辅助代理" : "启动辅助代理";
                    var toggleDedicatedItem = CreateMenuItem(toggleDedicatedText, "");
                    toggleDedicatedItem.Click += (_, _) => ViewModel.ToggleDedicatedPortCommand.Execute(server);

                    var editDedicatedItem = CreateMenuItem("设置辅助代理端口...", "");
                    editDedicatedItem.Click += (_, _) => ViewModel.EditDedicatedPortCommand.Execute(server);

                    flyout.Items.Add(toggleDedicatedItem);
                    flyout.Items.Add(editDedicatedItem);

                    if (server.DedicatedPort.HasValue && server.DedicatedPort.Value > 0)
                    {
                        var copyAddressItem = CreateMenuItem($"复制代理地址 (127.0.0.1:{server.DedicatedPort.Value})", "");
                        copyAddressItem.Click += (_, _) => ViewModel.CopyDedicatedPortAddressCommand.Execute(server);
                        flyout.Items.Add(copyAddressItem);
                    }
                }

                flyout.Items.Add(new MenuFlyoutSeparator());
            }

            var editItem = CreateMenuItem(L.ServerList_Edit, "");
            editItem.IsEnabled = ViewModel.CanEditSelectedServer;
            editItem.Click += (_, _) => ViewModel.EditServerCommand.Execute(null);

            var isFavorite = ViewModel.SelectedServer?.IsFavorite == true;
            var favoriteItem = CreateMenuItem(
                isFavorite ? L.ServerList_RemoveFavorite : L.ServerList_AddFavorite,
                isFavorite ? "\uE8D9" : "\uE734");
            favoriteItem.Click += (_, _) => ViewModel.ToggleFavoriteCommand.Execute(null);

            var deleteItem = CreateMenuItem(L.ServerList_Delete, "");
            deleteItem.IsEnabled = ViewModel.CanRemoveSelectedServer;
            deleteItem.Click += (_, _) => ViewModel.RemoveServerCommand.Execute(null);

            var shareItem = CreateMenuItem(L.ServerList_Share, "");
            shareItem.Click += (_, _) => ViewModel.ShareServerCommand.Execute(null);

            flyout.Items.Add(editItem);
            flyout.Items.Add(favoriteItem);
            flyout.Items.Add(deleteItem);
            flyout.Items.Add(shareItem);

            return flyout;
        }

        public static string ProxyActionText(bool auxiliary, bool isActive) => auxiliary
            ? (isActive ? "关闭辅代理" : "启用辅代理")
            : (isActive ? "关闭主代理" : "启用主代理");

        public static string PrimaryPortText(int port) => $"主端口 :{port}";

        public static string AuxiliaryPortText(int? port) =>
            port.HasValue ? $"辅端口 :{port.Value}" : "辅端口 未配置";

        public static string ConnectionStatusText(bool isActive) => isActive ? "已连接" : "未连接";

        private static MenuFlyoutItem CreateMenuItem(string text, string glyph)
        {
            return new MenuFlyoutItem
            {
                Text = text,
                Icon = new FontIcon { Glyph = glyph }
            };
        }

        // Right-click latency-test mode menu pushes the chosen mode into the VM; the toolbar icon
        // follows automatically via x:Bind on LatencyTestMode (see *IconVisibility below).
        private void TestModeConnectItem_Click(object sender, RoutedEventArgs e)
            => ViewModel.LatencyTestMode = "connect";

        private void TestModeRealItem_Click(object sender, RoutedEventArgs e)
            => ViewModel.LatencyTestMode = "real";

        // Toolbar icon visibility derived from the latency-test mode (bound from XAML).
        public static Visibility ConnectModeIconVisibility(string mode)
            => mode == "real" ? Visibility.Collapsed : Visibility.Visible;

        public static Visibility RealModeIconVisibility(string mode)
            => mode == "real" ? Visibility.Visible : Visibility.Collapsed;

        public static double ActiveBadgeOpacity(bool isActive)
            => isActive ? 1.0 : 0.0;

        public static Vector3 ActiveBadgeScale(bool isActive)
            => isActive ? Vector3.One : new Vector3(0.92f, 0.92f, 1f);

        // Foreground for the per-row latency number, keyed off the measured value:
        // failed probe (negative, e.g. -1) → critical, ≥200 ms → caution, else success.
        public static Brush LatencyForeground(int? milliseconds)
        {
            var key = milliseconds switch
            {
                < 0   => "LatencyFailBrush",
                < 200 => "LatencyGoodBrush",
                _     => "LatencyHighBrush",
            };
            return (Brush)Application.Current.Resources[key];
        }
    }
}
