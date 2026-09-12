using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.Views
{
    public sealed partial class ProxyStatusWindow
    {
        private readonly XrayService _xray;
        private readonly Func<IEnumerable<ServerEntry>> _servers;
        private readonly Func<ServerEntry?> _primary;
        private readonly Func<ServerEntry?, string> _subscription;
        private readonly Func<int> _localPort;
        private readonly Func<Task> _stopPrimary;
        private readonly Func<ServerEntry, Task> _stopAuxiliary;
        private readonly ObservableCollection<ProxyStatusItem> _items = new();
        private readonly ObservableCollection<ProxyConnectionItem> _connections = new();
        private readonly List<ServerEntry> _observedServers = new();
        private readonly DispatcherQueueTimer _telemetryTimer;
        private int _trafficQueryRunning;

        public ProxyStatusWindow(
            XrayService xray,
            Func<IEnumerable<ServerEntry>> servers,
            Func<ServerEntry?> primary,
            Func<ServerEntry?, string> subscription,
            Func<int> localPort,
            Func<Task> stopPrimary,
            Func<ServerEntry, Task> stopAuxiliary)
        {
            InitializeComponent();
            _xray = xray;
            _servers = servers;
            _primary = primary;
            _subscription = subscription;
            _localPort = localPort;
            _stopPrimary = stopPrimary;
            _stopAuxiliary = stopAuxiliary;
            Title = "已启用的代理";
            AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 700));
            ThemeHelper.FollowAppTheme(this, WindowRoot);
            ProxyList.ItemsSource = _items;
            ConnectionList.ItemsSource = _connections;

            _xray.RunningChanged += OnRunningChanged;
            _xray.Telemetry.EventReceived += OnTelemetryEvent;
            _telemetryTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _telemetryTimer.Interval = TimeSpan.FromSeconds(1);
            _telemetryTimer.IsRepeating = true;
            _telemetryTimer.Tick += (_, _) => RefreshTelemetry();
            _telemetryTimer.Start();
            Closed += (_, _) =>
            {
                _telemetryTimer.Stop();
                _xray.RunningChanged -= OnRunningChanged;
                _xray.Telemetry.EventReceived -= OnTelemetryEvent;
                foreach (var server in _observedServers)
                    server.PropertyChanged -= OnServerPropertyChanged;
                _observedServers.Clear();
            };
            RefreshProxies();
            RefreshTelemetry();
        }

        public void RefreshProxies()
        {
            foreach (var server in _observedServers)
                server.PropertyChanged -= OnServerPropertyChanged;
            _observedServers.Clear();

            var items = new List<ProxyStatusItem>();
            var primary = _primary();
            if (_xray.IsRunning)
            {
                items.Add(ProxyStatusItem.Primary(
                    primary,
                    _subscription(primary),
                    _localPort()));
            }

            var auxiliary = _servers()
                .Where(server => server.DedicatedPort is > 0 && server.IsDedicatedPortActive)
                .ToList();
            items.AddRange(auxiliary.Select(server => ProxyStatusItem.Auxiliary(
                server,
                _subscription(server))));

            foreach (var server in _servers())
            {
                server.PropertyChanged += OnServerPropertyChanged;
                _observedServers.Add(server);
            }

            _items.Clear();
            foreach (var item in items)
                _items.Add(item);
            StatusText.Text = items.Count == 0
                ? "当前没有已启用的代理。"
                : $"共 {items.Count} 个代理正在运行";
            RefreshTelemetry();
        }

        private void OnRunningChanged(object? sender, bool running)
            => DispatcherQueue.TryEnqueue(() =>
            {
                RefreshProxies();
                RefreshTelemetry();
            });

        private void OnTelemetryEvent(object? sender, ProxyConnectionEvent e)
            => DispatcherQueue.TryEnqueue(RefreshTelemetry);

        private void RefreshTelemetry()
        {
            var snapshot = _xray.Telemetry.Snapshot();
            TelemetryText.Text = $"活动连接 {snapshot.ActiveConnections}    累计连接 {snapshot.TotalConnections}    失败 {snapshot.FailedConnections}";
            if (!_xray.IsRunning)
                TrafficText.Text = "代理未运行";
            else if (_xray.StatsApiPort > 0)
                _ = RefreshTrafficAsync();

            _connections.Clear();
            foreach (var connection in snapshot.RecentConnections.Take(40))
                _connections.Add(ProxyConnectionItem.From(connection));
        }

        private async Task RefreshTrafficAsync()
        {
            if (Interlocked.Exchange(ref _trafficQueryRunning, 1) != 0)
                return;

            try
            {
                var traffic = await _xray.Stats.QueryInboundTrafficAsync(_xray.StatsApiPort);
                if (traffic is null)
                    return;

                TrafficText.Text = $"上传 {FormatBytes(traffic.UploadBytesPerSecond)}/s · 下载 {FormatBytes(traffic.DownloadBytesPerSecond)}/s"
                                 + $"    累计上传 {FormatBytes(traffic.UploadBytes)} · 下载 {FormatBytes(traffic.DownloadBytes)}";
            }
            finally
            {
                Volatile.Write(ref _trafficQueryRunning, 0);
            }
        }

        private static string FormatBytes(long bytes)
        {
            var value = Math.Max(0, bytes);
            var units = new[] { "B", "KB", "MB", "GB", "TB" };
            var unit = 0;
            double display = value;
            while (display >= 1024 && unit < units.Length - 1)
            {
                display /= 1024;
                unit++;
            }

            var number = display >= 100 ? display.ToString("0")
                       : display >= 10  ? display.ToString("0.0")
                                       : display.ToString("0.00");
            return number + units[unit];
        }

        private void OnServerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ServerEntry.IsDedicatedPortActive)
                or nameof(ServerEntry.DedicatedPort)
                or nameof(ServerEntry.Name)
                or nameof(ServerEntry.SubscriptionId))
            {
                DispatcherQueue.TryEnqueue(RefreshProxies);
            }
        }

        private async void DisableProxyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: ProxyStatusItem item }) return;

            if (sender is not Button button) return;
            button.IsEnabled = false;
            if (item.IsPrimary)
                await _stopPrimary();
            else if (item.Server is not null)
                await _stopAuxiliary(item.Server);

            RefreshProxies();
        }

    }

    public sealed class ProxyStatusItem
    {
        public required string Name { get; init; }
        public required string Details { get; init; }
        public required bool IsPrimary { get; init; }
        public ServerEntry? Server { get; init; }

        public static ProxyStatusItem Primary(ServerEntry? server, string subscription, int port) => new()
        {
            Name = $"主代理 · {server?.Name ?? "未知节点"}",
            Details = $"订阅：{subscription}    端口：{port}",
            IsPrimary = true
        };

        public static ProxyStatusItem Auxiliary(ServerEntry server, string subscription) => new()
        {
            Name = $"辅助代理 · {server.Name}",
            Details = $"订阅：{subscription}    端口：{server.DedicatedPort}",
            IsPrimary = false,
            Server = server
        };
    }

    public sealed class ProxyConnectionItem
    {
        public required string Time { get; init; }
        public required string Status { get; init; }
        public required string Route { get; init; }
        public required string Target { get; init; }
        public required string Detail { get; init; }

        public static ProxyConnectionItem From(ProxyConnectionEvent connection)
        {
            var route = string.IsNullOrWhiteSpace(connection.Inbound) && string.IsNullOrWhiteSpace(connection.Outbound)
                ? "路由信息未知"
                : $"{connection.Inbound} -> {connection.Outbound}";
            var status = connection.Kind switch
            {
                ProxyConnectionEventKind.Accepted => "已接受",
                ProxyConnectionEventKind.Closed => "已关闭",
                _ => "失败"
            };

            return new ProxyConnectionItem
            {
                Time = connection.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Status = status,
                Route = route,
                Target = string.IsNullOrWhiteSpace(connection.Target) ? "-" : connection.Target,
                Detail = connection.Kind == ProxyConnectionEventKind.Failed ? connection.Detail : string.Empty
            };
        }
    }
}