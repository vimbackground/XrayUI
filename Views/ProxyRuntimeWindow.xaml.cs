using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.Views
{
    /// <summary>Small, read-only runtime view over Xray's bounded in-memory log buffer.</summary>
    public sealed partial class ProxyRuntimeWindow
    {
        private readonly XrayService _xray;
        private readonly Func<IEnumerable<ServerEntry>> _servers;
        private readonly Func<ServerEntry?> _primary;
        private readonly Func<ServerEntry?, string> _subscription;
        private readonly Func<int> _localPort;
        private readonly Func<Task> _stopPrimary;
        private readonly Func<ServerEntry, Task> _stopAuxiliary;

        public ProxyRuntimeWindow(
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
            Title = "代理动态信息";
            AppWindow.Resize(new Windows.Graphics.SizeInt32(820, 560));
            ThemeHelper.FollowAppTheme(this, WindowRoot);
            _xray.LogReceived += OnLogReceived;
            _xray.RunningChanged += OnRunningChanged;
            Closed += (_, _) =>
            {
                _xray.LogReceived -= OnLogReceived;
                _xray.RunningChanged -= OnRunningChanged;
            };
            RefreshTargets();
            Render();
        }

        public void RefreshTargets()
        {
            var selected = TargetSelector.SelectedItem as RuntimeTarget;
            var primary = _primary();
            var targets = new List<RuntimeTarget>();
            if (_xray.IsRunning)
                targets.Add(RuntimeTarget.Primary(primary, _subscription(primary), _localPort()));
            targets.AddRange(_servers()
                .Where(s => s.DedicatedPort is > 0 && s.IsDedicatedPortActive)
                .Select(s => RuntimeTarget.Auxiliary(s, _subscription(s))));
            TargetSelector.ItemsSource = targets;
            TargetSelector.SelectedItem = targets.FirstOrDefault(t => t.Key == selected?.Key)
                ?? targets.FirstOrDefault();
        }

        private void TargetSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) => Render();

        private void OnLogReceived(object? sender, string line)
            => DispatcherQueue.TryEnqueue(Render);

        private void OnRunningChanged(object? sender, bool running)
            => DispatcherQueue.TryEnqueue(Render);

        private void Render()
        {
            var target = TargetSelector.SelectedItem as RuntimeTarget;
            if (target is null)
            {
                LogText.Text = "当前没有正在运行的代理。";
                StatusText.Text = "无运行中的代理";
                StopTargetButton.IsEnabled = false;
                return;
            }

            var lines = _xray.GetLogBuffer()
                .Where(line => target.IsRelevant(line))
                .TakeLast(300)
                .ToArray();
            LogText.Text = lines.Length == 0 ? "暂无可显示的动态信息。" : string.Join(Environment.NewLine, lines);
            StatusText.Text = _xray.IsRunning
                ? $"运行中 · {target.Label} · 本地端口 {target.Port} · 最近 {lines.Length} 条信息"
                : $"未运行 · {target.Label} · 最近 {lines.Length} 条信息";
            StopTargetButton.Content = target.IsPrimary ? "关闭当前主代理" : "关闭当前辅助代理";
            StopTargetButton.IsEnabled = target.IsPrimary ? _xray.IsRunning : target.Server?.IsDedicatedPortActive == true;
            LogScrollViewer.ScrollTo(LogScrollViewer.HorizontalOffset, LogScrollViewer.ScrollableHeight);
        }

        private async void StopTargetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (TargetSelector.SelectedItem is not RuntimeTarget target) return;

            StopTargetButton.IsEnabled = false;
            if (target.IsPrimary)
                await _stopPrimary();
            else if (target.Server is not null)
                await _stopAuxiliary(target.Server);

            RefreshTargets();
            Render();
        }

        private void CopyButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText($"{StatusText.Text}{Environment.NewLine}{LogText.Text}");
            Clipboard.SetContent(package);
        }

        private sealed class RuntimeTarget
        {
            public required string Key { get; init; }
            public required string Label { get; init; }
            public required int Port { get; init; }
            public bool IsPrimary => Key == "primary";
            public ServerEntry? Server { get; init; }
            public string? InboundTag { get; init; }
            public string? OutboundTag { get; init; }
            public override string ToString() => Label;

            public bool IsRelevant(string line)
            {
                if (Key == "primary")
                    return !line.Contains("inbound_dedicated_", StringComparison.Ordinal)
                        && !line.Contains("outbound_dedicated_", StringComparison.Ordinal);
                return line.Contains(InboundTag!, StringComparison.Ordinal)
                    || line.Contains(OutboundTag!, StringComparison.Ordinal);
            }

            public static RuntimeTarget Primary(ServerEntry? server, string subscription, int port) => new()
            {
                Key = "primary",
                Label = $"主代理 · {server?.Name ?? "未知节点"} · 订阅：{subscription} · 端口：{port}",
                Port = port
            };

            public static RuntimeTarget Auxiliary(ServerEntry server, string subscription) => new()
            {
                Key = server.Id,
                Label = $"辅助代理 · {server.Name} · 订阅：{subscription} · 端口：{server.DedicatedPort}",
                Port = server.DedicatedPort!.Value,
                Server = server,
                InboundTag = $"inbound_dedicated_{server.DedicatedPort}",
                OutboundTag = $"outbound_dedicated_{server.Id}"
            };
        }
    }
}
