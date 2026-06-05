using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XrayUI.Helpers;
using XrayUI.Models;
using XrayUI.Services;

namespace XrayUI.ViewModels
{
    public enum ServerSortMode
    {
        Default,
        Active,
        Protocol,
        Latency,
    }

    public partial class ServerListViewModel : ObservableObject, IDisposable
    {
        private const string AllChipKey            = "__all__";
        private const string UngroupedChipKey      = "__ungrouped__";
        private const string FavoritesChipKey      = "__favorites__";
        private const string SubscriptionUserAgent = "v2rayN/7.0";

        // Localized labels — looked up lazily so language changes apply at startup.
        private static string AllChipName     => L.ServerList_AllServers;
        private static string UngroupedName   => L.ServerList_Ungrouped;
        private static string FavoritesName   => L.ServerList_Favorites;
        private static string UnnamedSubLabel => L.ServerList_UnnamedSub;
        private static string OrphanSubLabel  => L.ServerList_OrphanSub;

        private static readonly HttpClient Http = CreateSubscriptionHttpClient();

        private static HttpClient CreateSubscriptionHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(SubscriptionUserAgent);
            return client;
        }

        private readonly IDialogService     _dialogs;
        private readonly SettingsService    _settings;
        private readonly LatencyProbeService _latencyProbe;
        private readonly RealLatencyProbeService _realLatencyProbe;
        private readonly SemaphoreSlim      _settingsWriteLock = new(1, 1);
        private const int MaxConcurrentProbes = 16;
        private ObservableCollection<ServerEntry> _servers = new();
        private ServerEntry? _selectedServer;
        private readonly List<ServerEntry> _selectedServers = new();
        private bool _isProxyRunning;
        private bool _disposed;

        // ── Grouping state ────────────────────────────────────────────────────
        private ServerGroupChip? _selectedChip;
        private string _searchQuery = string.Empty;
        private bool _isFilterPanelOpen;
        private bool _suppressRebuild;
        private ServerSortMode _sortMode = ServerSortMode.Default;
        private List<SubscriptionEntry> _knownSubscriptions = new();

        public ObservableCollection<ServerGroupChip> GroupChips { get; } = new();
        public ObservableCollection<ServerEntry>     VisibleServers { get; } = new();

        public ServerListViewModel(IDialogService dialogs, SettingsService settings, LatencyProbeService latencyProbe, RealLatencyProbeService realLatencyProbe)
        {
            _dialogs  = dialogs;
            _settings = settings;
            _latencyProbe = latencyProbe;
            _realLatencyProbe = realLatencyProbe;

            ProtocolColorStore.ColorsChanged += OnProtocolColorsChanged;
            _servers.CollectionChanged += OnServersCollectionChanged;
        }

        private void OnProtocolColorsChanged(object? sender, EventArgs e)
        {
            foreach (var s in Servers)
                s.RefreshProtocolColor();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ProtocolColorStore.ColorsChanged -= OnProtocolColorsChanged;
            _servers.CollectionChanged -= OnServersCollectionChanged;
            foreach (var server in _selectedServers)
            {
                server.PropertyChanged -= OnSelectedItemPropertyChanged;
            }
        }

        public ObservableCollection<ServerEntry> Servers
        {
            get => _servers;
            set
            {
                if (_servers != null)
                    _servers.CollectionChanged -= OnServersCollectionChanged;
                if (SetProperty(ref _servers, value) && _servers != null)
                {
                    _servers.CollectionChanged += OnServersCollectionChanged;
                    NotifySelectedServerRunStateChanged();
                }
            }
        }

        public ServerGroupChip? SelectedChip
        {
            get => _selectedChip;
            set
            {
                if (SetProperty(ref _selectedChip, value))
                {
                    OnPropertyChanged(nameof(CanSortByActive));

                    // When leaving the All chip while sorting by active server, fall back to default
                    // to avoid a disabled menu item remaining checked.
                    if (_sortMode == ServerSortMode.Active && !CanSortByActive)
                    {
                        SortMode = ServerSortMode.Default;
                        return;
                    }

                    RebuildGroupedView();
                    OnPropertyChanged(nameof(CanReorderInCurrentChip));
                }
            }
        }

        public bool CanReorderInCurrentChip =>
            string.IsNullOrWhiteSpace(_searchQuery)
            && _sortMode == ServerSortMode.Default
            && VisibleServers.Count > 1
            && !HasMultipleSelectedServers;

        public ServerSortMode SortMode
        {
            get => _sortMode;
            set
            {
                if (SetProperty(ref _sortMode, value))
                {
                    OnPropertyChanged(nameof(IsSortDefault));
                    OnPropertyChanged(nameof(IsSortActive));
                    OnPropertyChanged(nameof(IsSortProtocol));
                    OnPropertyChanged(nameof(IsSortLatency));
                    OnPropertyChanged(nameof(CanReorderInCurrentChip));
                    RebuildGroupedView();
                }
            }
        }

        // Active-server sorting is only available for chip = All; other chips should not
        // promote a single active server to the top of the subset.
        public bool CanSortByActive => _selectedChip?.Kind == ServerGroupChip.ChipKind.All;

        // Latency sorting is only meaningful once at least one server has been probed;
        // before the first "test all" sweep there is nothing to order by, so the menu
        // item stays disabled (with an explanatory tooltip), mirroring CanSortByActive.
        public bool CanSortByLatency => Servers.Any(s => s.LatencyMs.HasValue);

        // Shadow props for RadioMenuFlyoutItem.IsChecked TwoWay binding.
        public bool IsSortDefault
        {
            get => _sortMode == ServerSortMode.Default;
            set { if (value) SortMode = ServerSortMode.Default; }
        }

        public bool IsSortActive
        {
            get => _sortMode == ServerSortMode.Active;
            set { if (value) SortMode = ServerSortMode.Active; }
        }

        public bool IsSortProtocol
        {
            get => _sortMode == ServerSortMode.Protocol;
            set { if (value) SortMode = ServerSortMode.Protocol; }
        }

        public bool IsSortLatency
        {
            get => _sortMode == ServerSortMode.Latency;
            set { if (value) SortMode = ServerSortMode.Latency; }
        }

        public bool SelectAllGroup()
        {
            var allChip = GroupChips.FirstOrDefault(c => c.Kind == ServerGroupChip.ChipKind.All);
            if (allChip == null || _selectedChip?.Kind == ServerGroupChip.ChipKind.All)
                return false;

            SelectedChip = allChip;
            return true;
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value ?? string.Empty))
                {
                    if (!string.IsNullOrWhiteSpace(_searchQuery) && SelectAllGroup())
                        return;

                    RebuildGroupedView();
                }
            }
        }

        public bool IsChipBarVisible =>
            GroupChips.Count > 0;

        public bool IsFilterPanelOpen
        {
            get => _isFilterPanelOpen;
            set
            {
                if (SetProperty(ref _isFilterPanelOpen, value))
                    OnPropertyChanged(nameof(IsFilterBarVisible));
            }
        }

        public bool IsFilterBarVisible =>
            IsChipBarVisible && _isFilterPanelOpen;

        public ServerEntry? SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (SetProperty(ref _selectedServer, value))
                    SetSelectedServers(value is null
                        ? Array.Empty<ServerEntry>()
                        : new[] { value });
            }
        }

        public bool IsProxyRunning
        {
            get => _isProxyRunning;
            set
            {
                if (SetProperty(ref _isProxyRunning, value))
                {
                    NotifyServerActionStateChanged();
                    if (_sortMode == ServerSortMode.Active)
                        RebuildGroupedView();
                }
            }
        }

        public bool IsSelectedServerLocked => IsProxyRunning && SelectedServer?.IsActive == true;

        public int SelectedServerCount =>
            _selectedServers.Count > 0 ? _selectedServers.Count : (SelectedServer is null ? 0 : 1);

        public bool HasMultipleSelectedServers => _selectedServers.Count > 1;

        public bool HasLockedSelectedServer => IsProxyRunning && (
            _selectedServers.Count > 0
                ? _selectedServers.Any(s => s.IsActive)
                : SelectedServer?.IsActive == true);

        public bool CanEditSelectedServer => SelectedServer != null
            && !HasMultipleSelectedServers
            && !IsSelectedServerLocked;

        public bool CanRemoveSelectedServer => SelectedServerCount > 0 && !HasLockedSelectedServer;

        public bool CanRunSelectedServer => CanRunServer(SelectedServer);

        public bool CanRunServer(ServerEntry? server)
        {
            if (server is null) return false;
            if (!server.IsChain) return true;

            return IsResolvableChainEndpoint(server.ChainEntryServerId)
                   && IsResolvableChainEndpoint(server.ChainExitServerId)
                   && !string.Equals(
                       server.ChainEntryServerId,
                       server.ChainExitServerId,
                       StringComparison.Ordinal);
        }

        private bool IsResolvableChainEndpoint(string serverId)
        {
            return !string.IsNullOrWhiteSpace(serverId)
                   && Servers.Any(server =>
                       !server.IsChain
                       && string.Equals(server.Id, serverId, StringComparison.Ordinal));
        }

        private List<ServerEntry> GetSelectedServersSnapshot() => _selectedServers.Count > 0
            ? _selectedServers.ToList()
            : SelectedServer is null ? new List<ServerEntry>() : new List<ServerEntry> { SelectedServer };

        public void SetSelectedServers(IReadOnlyList<ServerEntry> selectedServers)
        {
            foreach (var server in _selectedServers)
                server.PropertyChanged -= OnSelectedItemPropertyChanged;

            _selectedServers.Clear();
            _selectedServers.AddRange(selectedServers);

            foreach (var server in _selectedServers)
                server.PropertyChanged += OnSelectedItemPropertyChanged;

            NotifyServerActionStateChanged();
        }

        // ── Search ────────────────────────────────────────────────────────────

        private const int MaxSearchSuggestions = 20;

        public IReadOnlyList<ServerEntry> SearchServers(string query)
        {
            return Servers
                .Where(s => !string.IsNullOrEmpty(s.Name) &&
                            s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(MaxSearchSuggestions)
                .ToArray();
        }

        // ── Persistence ───────────────────────────────────────────────────────

        public async Task LoadServersAsync()
        {
            var listTask     = _settings.LoadServersAsync();
            var settingsTask = _settings.LoadSettingsAsync();
            await Task.WhenAll(listTask, settingsTask);

            _knownSubscriptions = settingsTask.Result.Subscriptions != null
                ? new List<SubscriptionEntry>(settingsTask.Result.Subscriptions)
                : new List<SubscriptionEntry>();

            MutateServersInBatch(() =>
            {
                foreach (var s in listTask.Result)
                    Servers.Add(s);
            });

            if (Servers.Count > 0 && SelectedServer == null)
                SelectedServer = Servers[0];
        }

        private Task SaveAsync() => _settings.SaveServersAsync(Servers);

        public async Task SaveOrderAsync()
        {
            // Search results are not reorderable, so VisibleServers maps cleanly to either
            // all servers or the currently selected group.
            if (CanReorderInCurrentChip)
            {
                var newOrder = VisibleServers.ToList();
                if (newOrder.Count > 0)
                {
                    var positions = new Dictionary<ServerEntry, int>(Servers.Count);
                    for (int i = 0; i < Servers.Count; i++)
                        positions[Servers[i]] = i;

                    var slots = newOrder
                        .Where(positions.ContainsKey)
                        .Select(s => positions[s])
                        .OrderBy(i => i)
                        .ToList();

                    if (slots.Count == newOrder.Count)
                    {
                        MutateServersInBatch(() =>
                        {
                            for (int i = 0; i < newOrder.Count; i++)
                            {
                                var entry      = newOrder[i];
                                var currentIdx = Servers.IndexOf(entry);
                                var targetIdx  = slots[i];
                                if (currentIdx >= 0 && currentIdx != targetIdx)
                                    Servers.Move(currentIdx, targetIdx);
                            }
                        }, rebuild: false);
                    }
                }
            }

            await SaveAsync();
        }

        private void MutateServersInBatch(Action mutate, bool rebuild = true)
        {
            _suppressRebuild = true;
            try
            {
                mutate();
            }
            finally
            {
                _suppressRebuild = false;
                if (rebuild) RebuildAll();
                NotifySelectedServerRunStateChanged();
            }
        }

        private void RebuildAll()
        {
            // If the server set changed out from under a latency sort (e.g. a subscription
            // refresh or preset import replaced entries with fresh, untested ones), the
            // sort has nothing to order by — fall back to Default so the menu doesn't keep
            // a now-disabled item checked, mirroring the Active-chip guard.
            if (_sortMode == ServerSortMode.Latency && !CanSortByLatency)
                _sortMode = ServerSortMode.Default;

            OnPropertyChanged(nameof(CanSortByLatency));
            OnPropertyChanged(nameof(IsSortDefault));
            OnPropertyChanged(nameof(IsSortLatency));

            RebuildGroupChips();
            RebuildGroupedView();
        }

        // ── Grouping logic ────────────────────────────────────────────────────

        private void OnServersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressRebuild) return;
            // Move events come from intra-group drag-reorder; membership is unchanged.
            if (e.Action == NotifyCollectionChangedAction.Move) return;

            RebuildAll();
            NotifySelectedServerRunStateChanged();
        }

        private void RebuildGroupChips()
        {
            // Single pass: count servers grouped by SubscriptionId (empty/null → ungrouped bucket).
            var countsBySub = new Dictionary<string, int>(StringComparer.Ordinal);
            int ungroupedCount = 0;
            bool hasFavorites = false;
            foreach (var s in Servers)
            {
                hasFavorites |= s.IsFavorite;
                if (string.IsNullOrEmpty(s.SubscriptionId))
                    ungroupedCount++;
                else
                    countsBySub[s.SubscriptionId] = countsBySub.GetValueOrDefault(s.SubscriptionId) + 1;
            }

            var knownIds = new HashSet<string>(
                _knownSubscriptions.Where(k => !string.IsNullOrEmpty(k.Id)).Select(k => k.Id!),
                StringComparer.Ordinal);

            var previouslySelectedKey = ChipKey(_selectedChip);
            GroupChips.Clear();

            GroupChips.Add(new ServerGroupChip
            {
                Kind        = ServerGroupChip.ChipKind.All,
                DisplayName = AllChipName,
            });

            if (hasFavorites)
            {
                GroupChips.Add(new ServerGroupChip
                {
                    Kind        = ServerGroupChip.ChipKind.Favorites,
                    DisplayName = FavoritesName,
                });
            }

            foreach (var sub in _knownSubscriptions)
            {
                if (string.IsNullOrEmpty(sub.Id)) continue;
                if (!countsBySub.TryGetValue(sub.Id, out var count)) continue;
                GroupChips.Add(new ServerGroupChip
                {
                    Kind           = ServerGroupChip.ChipKind.Subscription,
                    DisplayName    = string.IsNullOrWhiteSpace(sub.Name) ? UnnamedSubLabel : sub.Name,
                    SubscriptionId = sub.Id,
                    Subscription   = sub,
                });
            }

            // Surface orphan subscription IDs (present on nodes but not in _knownSubscriptions)
            // so users can find and clean up nodes left behind by a deleted subscription.
            foreach (var (id, count) in countsBySub)
            {
                if (knownIds.Contains(id)) continue;
                GroupChips.Add(new ServerGroupChip
                {
                    Kind           = ServerGroupChip.ChipKind.Subscription,
                    DisplayName    = OrphanSubLabel,
                    SubscriptionId = id,
                    Subscription   = null,
                });
            }

            if (ungroupedCount > 0)
            {
                GroupChips.Add(new ServerGroupChip
                {
                    Kind        = ServerGroupChip.ChipKind.Ungrouped,
                    DisplayName = UngroupedName,
                });
            }

            ServerGroupChip? toSelect = null;
            if (previouslySelectedKey != null)
                toSelect = GroupChips.FirstOrDefault(c => ChipKey(c) == previouslySelectedKey);
            toSelect ??= GroupChips.FirstOrDefault();

            // Set backing field directly to avoid SelectedChip's setter triggering a second rebuild.
            if (!ReferenceEquals(_selectedChip, toSelect))
            {
                _selectedChip = toSelect;
                OnPropertyChanged(nameof(SelectedChip));
                OnPropertyChanged(nameof(CanReorderInCurrentChip));
            }

            OnPropertyChanged(nameof(IsChipBarVisible));
            OnPropertyChanged(nameof(IsFilterBarVisible));
        }

        private static string? ChipKey(ServerGroupChip? chip) => chip?.Kind switch
        {
            ServerGroupChip.ChipKind.All          => AllChipKey,
            ServerGroupChip.ChipKind.Ungrouped    => UngroupedChipKey,
            ServerGroupChip.ChipKind.Favorites    => FavoritesChipKey,
            ServerGroupChip.ChipKind.Subscription => chip!.SubscriptionId,
            _                                     => null,
        };

        private void RebuildGroupedView()
        {
            VisibleServers.Clear();

            var query = _searchQuery.Trim();
            bool MatchesSearch(ServerEntry s) =>
                string.IsNullOrEmpty(query) ||
                (!string.IsNullOrEmpty(s.Name) &&
                 s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            IEnumerable<ServerEntry> candidates = _selectedChip?.Kind switch
            {
                ServerGroupChip.ChipKind.Subscription =>
                    Servers.Where(s => s.SubscriptionId == (_selectedChip.SubscriptionId ?? string.Empty)),
                ServerGroupChip.ChipKind.Ungrouped =>
                    Servers.Where(s => string.IsNullOrEmpty(s.SubscriptionId)),
                ServerGroupChip.ChipKind.Favorites =>
                    Servers.Where(s => s.IsFavorite),
                _ => Servers,
            };

            var filtered = candidates.Where(MatchesSearch);
            IEnumerable<ServerEntry> ordered = _sortMode switch
            {
                ServerSortMode.Active =>
                    filtered.OrderBy(s => s.IsActive ? 0 : 1),
                ServerSortMode.Protocol =>
                    filtered.OrderBy(s => s.Protocol ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                ServerSortMode.Latency =>
                    // Ascending by measured ms; untested (null) and failed/timeout
                    // (negative sentinel) are bucketed last, then kept stable by ms.
                    filtered
                        .OrderBy(s => LatencySortBucket(s.LatencyMs))
                        .ThenBy(s => s.LatencyMs ?? int.MaxValue),
                _ => filtered,
            };

            foreach (var server in ordered)
                VisibleServers.Add(server);

            OnPropertyChanged(nameof(CanReorderInCurrentChip));
        }

        // Latency sort ordering buckets: 0 = a real measurement (sorted by ms),
        // 1 = failed/timeout (negative sentinel), 2 = never tested (null). Failures and
        // untested both sink below real results, with untested last.
        private static int LatencySortBucket(int? latencyMs) => latencyMs switch
        {
            null   => 2,
            < 0    => 1,
            _      => 0,
        };

        private async Task ReloadKnownSubscriptionsAsync()
        {
            var settings = await _settings.LoadSettingsAsync();
            _knownSubscriptions = settings.Subscriptions != null
                ? new List<SubscriptionEntry>(settings.Subscriptions)
                : new List<SubscriptionEntry>();
        }

        private void OnSelectedItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ServerEntry.Protocol)
                or nameof(ServerEntry.ChainEntryServerId)
                or nameof(ServerEntry.ChainExitServerId))
            {
                NotifySelectedServerRunStateChanged();
            }

            if (e.PropertyName == nameof(ServerEntry.IsActive))
            {
                NotifyServerActionStateChanged();
                if (_sortMode == ServerSortMode.Active)
                    RebuildGroupedView();
            }
        }

        private void NotifyServerActionStateChanged()
        {
            OnPropertyChanged(nameof(IsSelectedServerLocked));
            OnPropertyChanged(nameof(SelectedServerCount));
            OnPropertyChanged(nameof(HasMultipleSelectedServers));
            OnPropertyChanged(nameof(HasLockedSelectedServer));
            OnPropertyChanged(nameof(CanEditSelectedServer));
            OnPropertyChanged(nameof(CanRemoveSelectedServer));
            OnPropertyChanged(nameof(CanReorderInCurrentChip));
            NotifySelectedServerRunStateChanged();
        }

        private void NotifySelectedServerRunStateChanged()
        {
            OnPropertyChanged(nameof(CanRunSelectedServer));
        }

        // ── Latency batch test ────────────────────────────────────────────────

        private bool _isTestingLatencies;
        // Flat property (not a nested Command.IsRunning x:Bind) so the button's spinner
        // binding stays WUI2010-safe.
        public bool IsTestingLatencies
        {
            get => _isTestingLatencies;
            private set => SetProperty(ref _isTestingLatencies, value);
        }

        private string _latencyTestMode = "connect";
        // Business code (CLAUDE.md convention), set from the test button's right-click menu:
        //   "connect" → TCP handshake to the server endpoint (no core needed)
        //   "real"    → HTTP round-trip routed through a throwaway xray core (v2rayN "real delay")
        public string LatencyTestMode
        {
            get => _latencyTestMode;
            set => SetProperty(ref _latencyTestMode, value);
        }

        // Probes every server's latency and writes the result onto each ServerEntry: the
        // round-trip ms on success, or -1 for any failure (timeout/unreachable, shown as a
        // single red label). The async command auto-disables while running, so the button is
        // inert until the sweep finishes; results stream into the rows as each probe completes.
        // LatencyTestMode picks the probe: "connect" = direct TCP handshake to the endpoint;
        // "real" = a real HTTP round-trip through a throwaway xray core (v2rayN "real delay").
        [RelayCommand]
        private async Task TestAllLatencies()
        {
            var servers = Servers.ToList();
            if (servers.Count == 0) return;

            IsTestingLatencies = true;
            _latencySortUnlocked = false;
            try
            {
                if (LatencyTestMode == "real")
                    await TestRealLatenciesAsync(servers);
                else
                    await TestConnectLatenciesAsync(servers);
            }
            finally
            {
                IsTestingLatencies = false;
            }
        }

        // "connect" mode: direct TCP-handshake probe to each server endpoint, in parallel
        // (capped concurrency). No proxy core involved.
        private async Task TestConnectLatenciesAsync(List<ServerEntry> servers)
        {
            using var throttle = new SemaphoreSlim(MaxConcurrentProbes);
            var timeout = TimeSpan.FromSeconds(3);

            var tasks = servers.Select(async server =>
            {
                await throttle.WaitAsync();
                try
                {
                    // ProbeAsync is already async I/O, awaited directly (no Task.Run hop). No
                    // ConfigureAwait(false): the continuation resumes on the UI thread (WinUI
                    // sync context), so ApplyLatencyResult's bound updates are safe.
                    var result = await _latencyProbe.ProbeAsync(server, timeout);
                    ApplyLatencyResult(server, result.Status == LatencyProbeStatus.Success
                        ? result.Milliseconds ?? -1
                        : -1);
                }
                finally
                {
                    throttle.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        // "real" mode: route a real HTTP request through each server via a dedicated throwaway
        // xray core. Works even when nothing is connected and never touches the live session.
        // Chain nodes aren't supported by the speed-test core yet, so they're skipped.
        private Task TestRealLatenciesAsync(List<ServerEntry> servers)
        {
            var testable = servers.Where(s => !s.IsChain).ToList();
            if (testable.Count == 0) return Task.CompletedTask;

            // ProbeAllAsync marshals ApplyLatencyResult back onto the UI thread.
            return _realLatencyProbe.ProbeAllAsync(testable, ApplyLatencyResult);
        }

        // Per-result bookkeeping shared by both probe modes (always invoked on the UI thread,
        // serially): record the latency, unlock the latency-sort option once the first result
        // lands, and restream the grouped view live if latency sort is already active. The
        // _latencySortUnlocked flag is reset at the start of each TestAllLatencies run.
        private bool _latencySortUnlocked;
        private void ApplyLatencyResult(ServerEntry server, int latencyMs)
        {
            server.LatencyMs = latencyMs;

            if (!_latencySortUnlocked)
            {
                _latencySortUnlocked = true;
                OnPropertyChanged(nameof(CanSortByLatency));
            }

            if (_sortMode == ServerSortMode.Latency)
                RebuildGroupedView();
        }

        // ── Import via link ───────────────────────────────────────────────────

        [RelayCommand]
        private async Task ImportFromLink()
        {
            var text = await _dialogs.ShowImportLinkDialogAsync();
            if (text == null) return;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int added = 0;
            ServerEntry? lastAdded = null;

            MutateServersInBatch(() =>
            {
                foreach (var line in lines)
                {
                    var entry = NodeLinkParser.Parse(line.Trim());
                    if (entry == null) continue;

                    Servers.Add(entry);
                    lastAdded = entry;
                    added++;
                }
            });

            if (added == 0)
            {
                await _dialogs.ShowErrorAsync(L.Import_ParseFailed, L.Import_ParseFailedMsg);
                return;
            }

            SelectedServer = lastAdded;
            await SaveAsync();
        }

        // ── Subscriptions ─────────────────────────────────────────────────────

        [RelayCommand]
        private async Task OpenSubscriptions()
        {
            var settings = await _settings.LoadSettingsAsync();
            var vm = new ManageSubscriptionsViewModel(
                settings.Subscriptions ?? new List<SubscriptionEntry>(),
                RefreshSubscriptionAsync,
                DeleteSubscriptionAsync);

            var sub = await _dialogs.ShowSubscriptionsDialogAsync(vm);
            if (sub == null) return;

            sub.Id = Guid.NewGuid().ToString("N");

            var (entries, error) = await FetchSubscriptionNodesAsync(sub);

            if (entries != null)
            {
                MutateServersInBatch(() =>
                {
                    foreach (var e in entries) Servers.Add(e);
                }, rebuild: false);
                sub.LastUpdated = DateTimeOffset.Now;
                sub.LastError   = null;
            }
            else
            {
                sub.LastError = error;
            }

            await UpsertSubscriptionAsync(sub);
            await ReloadKnownSubscriptionsAsync();
            RebuildAll();

            if (entries != null && SelectedServer == null && Servers.Count > 0)
                SelectedServer = Servers[^1];

            await SaveAsync();

            if (entries == null)
            {
                await _dialogs.ShowErrorAsync(L.Subscription_FetchFailed, error ?? L.Subscription_UnknownError);
            }
        }

        private static async Task<(List<ServerEntry>? entries, string? error)> FetchSubscriptionNodesAsync(SubscriptionEntry sub)
        {
            string raw;
            try
            {
                raw = await Http.GetStringAsync(sub.Url);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }

            var trimmed = raw.Trim();
            var decoded = new byte[trimmed.Length];
            var text = Convert.TryFromBase64String(trimmed, decoded, out var written)
                ? Encoding.UTF8.GetString(decoded, 0, written)
                : raw;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var entries = new List<ServerEntry>();
            foreach (var line in lines)
            {
                var entry = NodeLinkParser.Parse(line.Trim());
                if (entry == null) continue;
                if (string.IsNullOrEmpty(entry.Name))
                    entry.Name = $"{sub.Name} #{entries.Count + 1}";
                entry.SubscriptionId = sub.Id;
                entries.Add(entry);
            }

            if (entries.Count == 0)
                return (null, L.Subscription_NoParsed);

            return (entries, null);
        }

        private async Task RefreshSubscriptionAsync(SubscriptionEntry sub)
        {
            if (IsSubscriptionLocked(sub.Id))
            {
                sub.LastError = L.Subscription_StopFirst_Refresh;
                return;
            }

            sub.IsBusy = true;
            try
            {
                var (newEntries, error) = await FetchSubscriptionNodesAsync(sub);
                if (newEntries == null)
                {
                    sub.LastError = Loc.Format("Subscription_UpdateFailed", error);
                    return;
                }

                var removed = Servers.Where(s => s.SubscriptionId == sub.Id).ToList();
                var wasSelectedId = SelectedServer?.Id;

                // Preserve Ids for nodes that survived the refresh so LastAutoConnectServerId
                // (and any other Id-based reference) keeps pointing at the same logical node.
                var oldByIdentity = new Dictionary<string, Queue<ServerEntry>>(StringComparer.Ordinal);
                foreach (var s in removed)
                {
                    var key = BuildNodeIdentityKey(s);
                    if (!oldByIdentity.TryGetValue(key, out var matches))
                    {
                        matches = new Queue<ServerEntry>();
                        oldByIdentity[key] = matches;
                    }
                    matches.Enqueue(s);
                }

                var reusedIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var e in newEntries)
                {
                    if (oldByIdentity.TryGetValue(BuildNodeIdentityKey(e), out var matches)
                        && matches.Count > 0)
                    {
                        var match = matches.Dequeue();
                        if (!string.IsNullOrWhiteSpace(match.Id) && reusedIds.Add(match.Id))
                            e.Id = match.Id;
                        e.IsFavorite = match.IsFavorite;
                    }
                }

                MutateServersInBatch(() =>
                {
                    foreach (var s in removed) Servers.Remove(s);
                    foreach (var e in newEntries) Servers.Add(e);
                });

                if (wasSelectedId != null && Servers.All(s => s.Id != wasSelectedId))
                    SelectedServer = newEntries.FirstOrDefault() ?? Servers.FirstOrDefault();

                sub.LastUpdated = DateTimeOffset.Now;
                sub.LastError   = null;

                await SaveAsync();
            }
            finally
            {
                sub.IsBusy = false;
                await UpsertSubscriptionAsync(sub);
            }
        }

        // Case-insensitive identifiers (host, protocol, uuid, etc.) are lowercased so a
        // subscription that re-emits "Example.com" still matches a stored "example.com".
        // Case-sensitive material (credentials, paths, base64/hex keys) is only trimmed.
        private static string BuildNodeIdentityKey(ServerEntry s) =>
            string.Join("|",
                NormalizeIdentityPart(s.Protocol),
                NormalizeIdentityPart(s.Host),
                s.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                NormalizeIdentityPart(s.Network),
                NormalizeIdentityPart(s.Security),
                s.Path?.Trim() ?? string.Empty,
                NormalizeIdentityPart(s.WsHost),
                NormalizeIdentityPart(s.Sni),
                NormalizeIdentityPart(s.Encryption),
                s.Username?.Trim() ?? string.Empty,
                s.Password?.Trim() ?? string.Empty,
                NormalizeIdentityPart(s.Uuid),
                s.AlterId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                s.VlessEncryption?.Trim() ?? string.Empty,
                s.Flow?.Trim() ?? string.Empty,
                s.PublicKey?.Trim() ?? string.Empty,
                s.ShortId?.Trim() ?? string.Empty);

        private static string NormalizeIdentityPart(string? value) =>
            value?.Trim().ToLowerInvariant() ?? string.Empty;

        private async Task<bool> DeleteSubscriptionAsync(SubscriptionEntry sub)
        {
            if (IsSubscriptionLocked(sub.Id))
            {
                sub.LastError = L.Subscription_StopFirst_Delete;
                return false;
            }

            var removed = Servers.Where(s => s.SubscriptionId == sub.Id).ToList();
            MutateServersInBatch(() =>
            {
                foreach (var s in removed) Servers.Remove(s);
            }, rebuild: false);

            await RemoveSubscriptionAsync(sub.Id);
            await ReloadKnownSubscriptionsAsync();
            RebuildAll();

            if (SelectedServer != null && !Servers.Contains(SelectedServer))
                SelectedServer = Servers.FirstOrDefault();

            await SaveAsync();
            return true;
        }

        private bool IsSubscriptionLocked(string subscriptionId)
        {
            return IsProxyRunning && Servers.Any(s =>
                s.IsActive &&
                string.Equals(s.SubscriptionId, subscriptionId, StringComparison.Ordinal));
        }

        private async Task UpsertSubscriptionAsync(SubscriptionEntry sub)
        {
            await _settingsWriteLock.WaitAsync();
            try
            {
                var settings = await _settings.LoadSettingsAsync();
                settings.Subscriptions ??= new List<SubscriptionEntry>();

                var idx = settings.Subscriptions.FindIndex(s => s.Id == sub.Id);
                var snapshot = CloneForPersistence(sub);
                if (idx >= 0) settings.Subscriptions[idx] = snapshot;
                else          settings.Subscriptions.Add(snapshot);

                await _settings.SaveSettingsAsync(settings);
            }
            finally
            {
                _settingsWriteLock.Release();
            }
        }

        private async Task RemoveSubscriptionAsync(string subId)
        {
            await _settingsWriteLock.WaitAsync();
            try
            {
                var settings = await _settings.LoadSettingsAsync();
                if (settings.Subscriptions == null) return;
                settings.Subscriptions.RemoveAll(s => s.Id == subId);
                if (settings.Subscriptions.Count == 0) settings.Subscriptions = null;
                await _settings.SaveSettingsAsync(settings);
            }
            finally
            {
                _settingsWriteLock.Release();
            }
        }

        private static SubscriptionEntry CloneForPersistence(SubscriptionEntry sub) => new()
        {
            Id          = sub.Id,
            Name        = sub.Name,
            Url         = sub.Url,
            LastUpdated = sub.LastUpdated,
            LastError   = sub.LastError,
        };

        // ── Add manual ────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task AddManual()
        {
            var entry = await _dialogs.ShowEditServerDialogAsync(null);
            if (entry == null) return;

            Servers.Add(entry);
            SelectedServer = entry;
            await SaveAsync();
        }

        [RelayCommand]
        private async Task AddChainProxy()
        {
            var entry = await _dialogs.ShowChainProxyDialogAsync(Servers);
            if (entry == null) return;

            Servers.Add(entry);
            SelectedServer = entry;
            await SaveAsync();
        }

        // ── Edit ──────────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task EditServer()
        {
            if (SelectedServer is null) return;
            if (HasMultipleSelectedServers) return;

            if (SelectedServer.IsChain)
            {
                var chainResult = await _dialogs.ShowChainProxyDialogAsync(Servers, SelectedServer);
                if (chainResult == null) return;

                await SaveAsync();
                return;
            }

            // Pass existing so dialog can pre-populate; dialog mutates and returns same ref on Primary
            var result = await _dialogs.ShowEditServerDialogAsync(SelectedServer);
            if (result == null) return;

            // result is the same object (mutated in-place by DialogService)
            await SaveAsync();
        }

        // ── Share ─────────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task ShareServer()
        {
            if (SelectedServer is null) return;

            var link = NodeLinkSerializer.ToLink(SelectedServer);
            if (string.IsNullOrEmpty(link))
            {
                await _dialogs.ShowErrorAsync(L.Share_NotSupported, L.Share_NotSupportedMsg);
                return;
            }

            await _dialogs.ShowShareLinkDialogAsync(SelectedServer.Name, link);
        }

        // ── Favorite ─────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task ToggleFavorite()
        {
            if (SelectedServer is null) return;
            var server = SelectedServer;
            var isFavoritesChip = _selectedChip?.Kind == ServerGroupChip.ChipKind.Favorites;
            server.IsFavorite = !server.IsFavorite;
            var justFavorited = server.IsFavorite;

            if (isFavoritesChip && !server.IsFavorite)
            {
                RebuildAll();
                // The current node is no longer in Favorites; select the first node after rebuilding.
                SelectedServer = VisibleServers.FirstOrDefault();
            }
            else
            {
                SyncFavoritesChipPresence(justFavorited);
            }

            await SaveAsync();
        }

        private void SyncFavoritesChipPresence(bool justFavorited)
        {
            var favoritesChip = GroupChips.FirstOrDefault(c => c.Kind == ServerGroupChip.ChipKind.Favorites);
            var hasFavorites = justFavorited || Servers.Any(s => s.IsFavorite);

            if (hasFavorites && favoritesChip == null)
            {
                // RebuildGroupChips always places the All chip at index 0, with Favorites immediately after it.
                GroupChips.Insert(1, new ServerGroupChip
                {
                    Kind        = ServerGroupChip.ChipKind.Favorites,
                    DisplayName = FavoritesName,
                });
            }
            else if (!hasFavorites && favoritesChip != null)
            {
                GroupChips.Remove(favoritesChip);
            }
        }

        // ── Remove ────────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task RemoveServer()
        {
            var selectedServers = GetSelectedServersSnapshot();
            if (selectedServers.Count == 0) return;
            if (IsProxyRunning && selectedServers.Any(s => s.IsActive)) return;

            var isBatchDelete = selectedServers.Count > 1;
            var message = isBatchDelete
                ? Loc.Format("Confirm_DeleteBatchMsg", selectedServers.Count)
                : Loc.Format("Confirm_DeleteMsg", selectedServers[0].Name);

            var confirmed = await _dialogs.ShowConfirmationAsync(
                L.Confirm_DeleteTitle,
                message,
                L.Dialog_Delete,
                L.Dialog_Cancel,
                isDanger: true);
            if (!confirmed) return;

            ServerEntry? nextSelected;
            if (isBatchDelete)
            {
                var firstVisibleIdx = selectedServers
                    .Select(s => VisibleServers.IndexOf(s))
                    .Where(i => i >= 0)
                    .DefaultIfEmpty(0)
                    .Min();

                MutateServersInBatch(() =>
                {
                    foreach (var server in selectedServers)
                        Servers.Remove(server);
                });

                nextSelected = VisibleServers.Count > 0
                    ? VisibleServers[Math.Min(firstVisibleIdx, VisibleServers.Count - 1)]
                    : Servers.FirstOrDefault();
            }
            else
            {
                var toRemove = selectedServers[0];
                var idx      = Servers.IndexOf(toRemove);
                Servers.Remove(toRemove);

                nextSelected = Servers.Count > 0
                    ? Servers[Math.Max(0, idx - 1)]
                    : null;
            }

            SelectedServer = nextSelected;

            await SaveAsync();
        }
    }
}
