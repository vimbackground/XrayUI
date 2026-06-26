using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.ViewModels
{
    public partial class ServerDetailViewModel : ObservableObject
    {
        private static SolidColorBrush GetBrush(string key) =>
            (SolidColorBrush)Application.Current.Resources[key];

        private readonly LatencyProbeService _latencyProbe;
        private readonly AiUnlockCheckService _aiUnlockCheck;
        private CancellationTokenSource? _latencyTestCts;
        private CancellationTokenSource? _aiCheckCts;
        private int _latencyTestVersion;
        private AiUnlockStatus? _openAiStatus;
        private AiUnlockStatus? _claudeStatus;
        private AiUnlockStatus? _geminiStatus;

        public ServerDetailViewModel(LatencyProbeService latencyProbe, AiUnlockCheckService aiUnlockCheck)
        {
            _latencyProbe = latencyProbe;
            _aiUnlockCheck = aiUnlockCheck;
            LatencyText = L.ServerDetail_NotTested;
            ShowLatencyInDetails = true;
            ShowAiUnlockInDetails = true;
            ResetAiUnlockDisplay();
        }

        public Func<IEnumerable<ServerEntry>> GetAllServers { get; set; } = () => Array.Empty<ServerEntry>();

        private ServerEntry? ResolveChainServer(string id)
            => string.IsNullOrEmpty(id) ? null : GetAllServers().FirstOrDefault(s => s.Id == id);

        [ObservableProperty]
        public partial ServerEntry? SelectedServer { get; set; }

        [ObservableProperty]
        public partial ServerEntry? ActiveServer { get; set; }

        [ObservableProperty]
        public partial bool IsProxyRunning { get; private set; }

        public string SelectedName => SelectedServer?.Name ?? L.ServerDetail_NoServer;

        public string SelectedHostLabel
            => SelectedServer?.IsChain == true ? L.ServerDetail_Entry : L.ServerDetail_Address;

        public string SelectedHost
            => SelectedServer?.IsChain == true
                ? ResolveChainServer(SelectedServer.ChainEntryServerId)?.Name ?? L.ServerDetail_EntryMissing
                : SelectedServer?.Host ?? "-";

        public string SelectedPortLabel
            => SelectedServer?.IsChain == true ? L.ServerDetail_Exit : L.ServerDetail_Port;

        public string SelectedPort
            => SelectedServer?.IsChain == true
                ? ResolveChainServer(SelectedServer.ChainExitServerId)?.Name ?? L.ServerDetail_ExitMissing
                : SelectedServer?.Port.ToString() ?? "-";

        public string SelectedProtocol => SelectedServer?.DisplayProtocol ?? "-";

        public string SelectedSecurityLabel
            => (SelectedServer?.Protocol?.ToLowerInvariant()) switch
            {
                "ss"    => L.ServerDetail_Encryption,
                "socks" => L.ServerDetail_AuthLabel,
                "chain" => L.ServerDetail_ChainLabel,
                _       => L.ServerDetail_Security
            };

        public string SelectedEncryption
        {
            get
            {
                if (SelectedServer is null) return "-";
                switch (SelectedServer.Protocol?.ToLowerInvariant())
                {
                    case "socks":
                        return string.IsNullOrWhiteSpace(SelectedServer.Username)
                               && string.IsNullOrWhiteSpace(SelectedServer.Password)
                            ? L.ServerDetail_NoAuth
                            : L.ServerDetail_UserPass;
                    case "chain":
                        var entry = ResolveChainServer(SelectedServer.ChainEntryServerId)?.DisplayProtocol ?? "?";
                        var exit = ResolveChainServer(SelectedServer.ChainExitServerId)?.DisplayProtocol ?? "?";
                        return $"{entry} -> {exit}";
                    default:
                        return SelectedServer.Encryption ?? "-";
                }
            }
        }

        public string SelectedShareLink
            => SelectedServer is null ? string.Empty : (NodeLinkSerializer.ToLink(SelectedServer) ?? string.Empty);

        public string SelectedTransport
        {
            get
            {
                if (SelectedServer is null)
                {
                    return "-";
                }

                if (string.Equals(SelectedServer.Protocol, "hysteria2", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(SelectedServer.Protocol, "wireguard", StringComparison.OrdinalIgnoreCase))
                {
                    return "UDP";
                }

                return (SelectedServer.Network?.ToLowerInvariant()) switch
                {
                    "ws" => "WebSocket",
                    "grpc" => "gRPC",
                    "xhttp" => "XHTTP",
                    _ => "TCP"
                };
            }
        }

        public Visibility SelectedTransportVisibility
            => SelectedServer?.IsChain == true ? Visibility.Collapsed : Visibility.Visible;

        [ObservableProperty]
        public partial string LatencyText { get; set; }

        [ObservableProperty]
        public partial bool IsTestingLatency { get; set; }

        public bool CanTestLatency => !IsTestingLatency && SelectedServer is not null;

        public bool CanCopyShareLink => !string.IsNullOrWhiteSpace(SelectedShareLink);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LatencyVisibility))]
        public partial bool ShowLatencyInDetails { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AiUnlockVisibility))]
        public partial bool ShowAiUnlockInDetails { get; set; }

        public Visibility LatencyVisibility => ShowLatencyInDetails ? Visibility.Visible : Visibility.Collapsed;

        public Visibility AiUnlockVisibility => ShowAiUnlockInDetails ? Visibility.Visible : Visibility.Collapsed;

        // ── AI Unlock indicators ──────────────────────────────────────────────

        [ObservableProperty]
        public partial SolidColorBrush OpenAiStatusBrush { get; set; }

        [ObservableProperty]
        public partial SolidColorBrush ClaudeStatusBrush { get; set; }

        [ObservableProperty]
        public partial SolidColorBrush GeminiStatusBrush { get; set; }

        partial void OnSelectedServerChanged(ServerEntry? oldValue, ServerEntry? newValue)
        {
            if (oldValue is not null)
            {
                oldValue.PropertyChanged -= OnSelectedServerPropertyChanged;
            }

            if (newValue is not null)
            {
                newValue.PropertyChanged += OnSelectedServerPropertyChanged;
            }

            CancelPendingLatencyTest();
            NotifySelectedServerFieldsChanged();
            UpdateAiUnlockDisplay();

            if (newValue is null)
            {
                ResetLatencyDisplay();
                return;
            }

            _ = TestLatency();
        }

        partial void OnActiveServerChanged(ServerEntry? value) => UpdateAiUnlockDisplay();

        partial void OnIsProxyRunningChanged(bool value) => UpdateAiUnlockDisplay();

        partial void OnIsTestingLatencyChanged(bool value)
        {
            OnPropertyChanged(nameof(CanTestLatency));
            TestLatencyCommand.NotifyCanExecuteChanged();
        }

        private void OnSelectedServerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ServerEntry.Name):
                    OnPropertyChanged(nameof(SelectedName));
                    break;
                case nameof(ServerEntry.Host):
                    OnPropertyChanged(nameof(SelectedHostLabel));
                    OnPropertyChanged(nameof(SelectedHost));
                    CancelPendingLatencyTest();
                    ResetLatencyDisplay();
                    break;
                case nameof(ServerEntry.Port):
                    OnPropertyChanged(nameof(SelectedPortLabel));
                    OnPropertyChanged(nameof(SelectedPort));
                    CancelPendingLatencyTest();
                    ResetLatencyDisplay();
                    break;
                case nameof(ServerEntry.Protocol):
                    OnPropertyChanged(nameof(SelectedProtocol));
                    OnPropertyChanged(nameof(SelectedHostLabel));
                    OnPropertyChanged(nameof(SelectedHost));
                    OnPropertyChanged(nameof(SelectedPortLabel));
                    OnPropertyChanged(nameof(SelectedPort));
                    OnPropertyChanged(nameof(SelectedSecurityLabel));
                    OnPropertyChanged(nameof(SelectedTransport));
                    OnPropertyChanged(nameof(SelectedTransportVisibility));
                    OnPropertyChanged(nameof(SelectedShareLink));
                    OnPropertyChanged(nameof(CanCopyShareLink));
                    break;
                case nameof(ServerEntry.Encryption):
                case nameof(ServerEntry.Username):
                case nameof(ServerEntry.Password):
                    OnPropertyChanged(nameof(SelectedEncryption));
                    OnPropertyChanged(nameof(SelectedShareLink));
                    OnPropertyChanged(nameof(CanCopyShareLink));
                    break;
                case nameof(ServerEntry.ChainEntryServerId):
                    OnPropertyChanged(nameof(SelectedHost));
                    OnPropertyChanged(nameof(SelectedEncryption));
                    break;
                case nameof(ServerEntry.ChainExitServerId):
                    OnPropertyChanged(nameof(SelectedPort));
                    OnPropertyChanged(nameof(SelectedEncryption));
                    break;
                case nameof(ServerEntry.Security):
                case nameof(ServerEntry.VlessEncryption):
                case nameof(ServerEntry.EchConfigList):
                case nameof(ServerEntry.EchForceQuery):
                    OnPropertyChanged(nameof(SelectedShareLink));
                    OnPropertyChanged(nameof(CanCopyShareLink));
                    break;
                case nameof(ServerEntry.Network):
                    OnPropertyChanged(nameof(SelectedTransport));
                    break;
                case null:
                case "":
                    CancelPendingLatencyTest();
                    NotifySelectedServerFieldsChanged();
                    ResetLatencyDisplay();
                    break;
            }
        }

        private void NotifySelectedServerFieldsChanged()
        {
            OnPropertyChanged(nameof(SelectedName));
            OnPropertyChanged(nameof(SelectedHostLabel));
            OnPropertyChanged(nameof(SelectedHost));
            OnPropertyChanged(nameof(SelectedPortLabel));
            OnPropertyChanged(nameof(SelectedPort));
            OnPropertyChanged(nameof(SelectedProtocol));
            OnPropertyChanged(nameof(SelectedSecurityLabel));
            OnPropertyChanged(nameof(SelectedEncryption));
            OnPropertyChanged(nameof(SelectedTransport));
            OnPropertyChanged(nameof(SelectedTransportVisibility));
            OnPropertyChanged(nameof(SelectedShareLink));
            OnPropertyChanged(nameof(CanCopyShareLink));
            OnPropertyChanged(nameof(CanTestLatency));
            TestLatencyCommand.NotifyCanExecuteChanged();
        }

        private void ResetLatencyDisplay()
        {
            LatencyText = L.ServerDetail_NotTested;
        }

        private void ResetAiUnlockDisplay()
        {
            var neutral = GetBrush("StateNeutralBrush");
            OpenAiStatusBrush = neutral;
            ClaudeStatusBrush = neutral;
            GeminiStatusBrush = neutral;
        }

        private void ClearAiUnlockResults()
        {
            _openAiStatus = null;
            _claudeStatus = null;
            _geminiStatus = null;
        }

        private void UpdateAiUnlockDisplay()
        {
            if (!IsProxyRunning || SelectedServer is null || !ReferenceEquals(SelectedServer, ActiveServer))
            {
                ResetAiUnlockDisplay();
                return;
            }

            OpenAiStatusBrush = ResolveAiUnlockBrush(_openAiStatus);
            ClaudeStatusBrush = ResolveAiUnlockBrush(_claudeStatus);
            GeminiStatusBrush = ResolveAiUnlockBrush(_geminiStatus);
        }

        private static SolidColorBrush ResolveAiUnlockBrush(AiUnlockStatus? status) => status switch
        {
            AiUnlockStatus.Unlocked => GetBrush("StateSuccessBrush"),
            AiUnlockStatus.Blocked  => GetBrush("StateErrorBrush"),
            _                       => GetBrush("StateNeutralBrush")
        };

        private void CancelPendingLatencyTest()
        {
            _latencyTestVersion++;
            var cts = _latencyTestCts;
            _latencyTestCts = null;
            IsTestingLatency = false;

            if (cts is not null)
            {
                _ = CancelLatencyTestAsync(cts);
            }
        }

        private static Task CancelLatencyTestAsync(CancellationTokenSource cts)
        {
            return Task.Run(() =>
            {
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception)
                {
                }
            });
        }

        private void CancelPendingAiCheck()
        {
            _aiCheckCts?.Cancel();
            _aiCheckCts = null;
        }

        /// <summary>
        /// Called by the view / MainViewModel when the proxy starts or stops.
        /// </summary>
        public void OnProxyRunningChanged(bool isRunning, int httpProxyPort)
        {
            CancelPendingAiCheck();
            IsProxyRunning = isRunning;

            if (!isRunning)
            {
                ClearAiUnlockResults();
                UpdateAiUnlockDisplay();
                return;
            }

            ClearAiUnlockResults();
            UpdateAiUnlockDisplay();
            _ = RunAiUnlockChecksAsync(httpProxyPort);
        }

        private async Task RunAiUnlockChecksAsync(int httpProxyPort)
        {
            var cts = new CancellationTokenSource();
            _aiCheckCts = cts;

            // Update each indicator the moment its own check returns, instead of
            // batching with Task.WhenAll (which would gate every dot on the slowest
            // check — Gemini). The await resumes on the UI thread, so touching the
            // status brushes here is safe.
            async Task RunOne(Task<AiUnlockStatus> check, Action<AiUnlockStatus> assign)
            {
                var status = await check;
                if (cts.IsCancellationRequested) return;
                assign(status);
                UpdateAiUnlockDisplay();
            }

            try
            {
                // All three requests are kicked off before the first await, so they
                // still run in parallel.
                await Task.WhenAll(
                    RunOne(_aiUnlockCheck.CheckOpenAiAsync(httpProxyPort, cts.Token), s => _openAiStatus = s),
                    RunOne(_aiUnlockCheck.CheckClaudeAsync(httpProxyPort, cts.Token), s => _claudeStatus = s),
                    RunOne(_aiUnlockCheck.CheckGeminiAsync(httpProxyPort, cts.Token), s => _geminiStatus = s));
            }
            catch (OperationCanceledException)
            {
                // cancelled — leave as-is
            }
            finally
            {
                cts.Dispose();
                if (ReferenceEquals(_aiCheckCts, cts))
                    _aiCheckCts = null;
            }
        }

        [RelayCommand(CanExecute = nameof(CanTestLatency))]
        private async Task TestLatency()
        {
            var server = SelectedServer;
            if (server is null)
            {
                return;
            }

            var version = _latencyTestVersion;
            var cts = new CancellationTokenSource();
            _latencyTestCts = cts;

            IsTestingLatency = true;
            LatencyText = L.ServerDetail_Testing;

            try
            {
                var token = cts.Token;
                var result = await Task.Run(
                    () => _latencyProbe.ProbeAsync(server, TimeSpan.FromSeconds(3), token),
                    token);

                if (!IsCurrentLatencyTest(version, cts, server))
                {
                    return;
                }

                LatencyText = result.Status switch
                {
                    LatencyProbeStatus.Success => Loc.Format("ServerDetail_LatencyMs", result.Milliseconds ?? 0),
                    LatencyProbeStatus.Timeout => L.ServerDetail_Timeout,
                    _                          => L.ServerDetail_Failed
                };
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
            }
            finally
            {
                var isCurrentTest = IsCurrentLatencyTest(version, cts, server);

                cts.Dispose();

                if (ReferenceEquals(_latencyTestCts, cts))
                {
                    _latencyTestCts = null;
                }

                if (isCurrentTest)
                {
                    IsTestingLatency = false;
                }
            }
        }

        private bool IsCurrentLatencyTest(
            int version,
            CancellationTokenSource cts,
            ServerEntry server)
        {
            return version == _latencyTestVersion
                   && !cts.IsCancellationRequested
                   && ReferenceEquals(SelectedServer, server);
        }
    }
}


