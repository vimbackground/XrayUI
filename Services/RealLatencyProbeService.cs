using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XrayUI.Helpers;
using XrayUI.Models;

namespace XrayUI.Services
{
    /// <summary>
    /// "Real delay" latency test (v2rayN-style): routes a real HTTP request through each server
    /// and times the round trip. Spins up a dedicated throwaway xray core with its own ports and
    /// config — completely separate from the live connection owned by <see cref="XrayService"/> —
    /// so it works even when nothing is connected and never disturbs the user's active session.
    ///
    /// Chain servers are not supported here (they need a second outbound + proxySettings); the
    /// caller must filter them out before calling.
    /// </summary>
    public sealed class RealLatencyProbeService
    {
        private const string TestUrl = "http://www.gstatic.com/generate_204";

        // Overall budget for one server's whole warm-up-then-measure cycle (matches v2rayN's 10s).
        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

        // Probe each server twice on a *reused* (keep-alive) connection and keep the fastest:
        // the first request pays the one-time tunnel + TLS handshake (very pricey for reality),
        // the second rides the warm connection and reflects steady-state RTT (≈ a TCP ping).
        // This is exactly what v2rayN's GetRealPingTime does, so the numbers line up with it.
        private const int ProbeIterations = 2;
        private const int InterProbeDelayMs = 100;
        private const int MaxConcurrency = 16;

        // Give the throwaway core a moment to bind every socks inbound before we probe.
        private const int CoreReadyDelayMs = 800;

        // Dedicated config path, separate from XrayService's xray_config.json so a test never
        // clobbers the live session's config file.
        private static readonly string ConfigPath = Path.Combine(AppPaths.LocalAppDataDir, "xray_speedtest.json");

        /// <summary>Populated when the test core fails to start; empty on success.</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>
        /// Probes every server in <paramref name="servers"/> and reports each result through
        /// <paramref name="onResult"/> (latency in ms, or -1 for timeout/failure) as it lands.
        /// The HTTP timing runs off the UI thread for accuracy; <paramref name="onResult"/> is
        /// marshalled back to the calling (UI) thread, so it may touch bound state directly.
        /// </summary>
        public async Task ProbeAllAsync(
            IReadOnlyList<ServerEntry> servers,
            Action<ServerEntry, int> onResult,
            CancellationToken ct = default)
        {
            LastError = string.Empty;
            if (servers.Count == 0)
                return;

            // Capture the caller's context (the UI thread) so results marshal back to it for safe
            // binding updates, while the timing itself runs off-thread (ConfigureAwait(false)) —
            // UI-thread scheduling delay must not inflate the measured RTT.
            var ui = SynchronizationContext.Current;
            void Report(ServerEntry s, int ms)
            {
                if (ui is not null)
                    ui.Post(_ => onResult(s, ms), null);
                else
                    onResult(s, ms);
            }

            if (!File.Exists(XrayService.ExePath))
            {
                LastError = Loc.Format("Xray_ExeNotFound", XrayService.ExePath);
                foreach (var s in servers)
                    Report(s, -1);
                return;
            }

            // 1. Allocate one free loopback port per server.
            var entries = new List<(ServerEntry server, int port)>(servers.Count);
            foreach (var s in servers)
                entries.Add((s, GetFreeLoopbackPort()));

            // 2. Build + write the dedicated multi-inbound speed-test config.
            string configJson = XrayConfigBuilder.BuildSpeedtestConfig(entries);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            await File.WriteAllTextAsync(ConfigPath, configJson, ct).ConfigureAwait(false);

            Process? process = null;
            JobObjectGuard? jobGuard = null;
            var output = new StringBuilder();

            try
            {
                // 3. Launch the throwaway core.
                var psi = new ProcessStartInfo
                {
                    FileName = XrayService.ExePath,
                    Arguments = $"run -config \"{ConfigPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(XrayService.ExePath)!,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                psi.EnvironmentVariables["XRAY_LOCATION_ASSET"] = XrayService.RulesDir;

                process = new Process { StartInfo = psi };
                // xray logs to both stdout and stderr; capture both so a failed start has a reason.
                void Capture(string? line)
                {
                    if (line is null) return;
                    lock (output) output.AppendLine(line);
                }
                process.OutputDataReceived += (_, e) => Capture(e.Data);
                process.ErrorDataReceived += (_, e) => Capture(e.Data);

                process.Start();
                jobGuard = JobObjectGuard.Assign(process);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 4. Wait for the inbounds to come up.
                await Task.Delay(CoreReadyDelayMs, ct).ConfigureAwait(false);

                if (process.HasExited)
                {
                    string log;
                    lock (output) log = output.ToString().Trim();
                    LastError = log.Length > 0
                        ? log
                        : Loc.Format("Xray_ExitedImmediately", process.ExitCode);
                    foreach (var s in servers)
                        Report(s, -1);
                    return;
                }

                // 5. Probe each server through its own socks port (capped concurrency).
                using var throttle = new SemaphoreSlim(MaxConcurrency);
                var tasks = new List<Task>(entries.Count);
                foreach (var (server, port) in entries)
                    tasks.Add(ProbeOneAsync(server, port, throttle, Report, ct));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                foreach (var s in servers)
                    Report(s, -1);
            }
            finally
            {
                // 6. Tear down the throwaway core and its temp config.
                try
                {
                    if (process is not null && !process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch { }

                jobGuard?.Dispose();
                process?.Dispose();

                try
                {
                    if (File.Exists(ConfigPath))
                        File.Delete(ConfigPath);
                }
                catch { }
            }
        }

        private static async Task ProbeOneAsync(
            ServerEntry server,
            int port,
            SemaphoreSlim throttle,
            Action<ServerEntry, int> report,
            CancellationToken ct)
        {
            await throttle.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                using var handler = new SocketsHttpHandler
                {
                    Proxy = new WebProxy($"socks5://127.0.0.1:{port}"),
                    UseProxy = true
                };
                using var client = new HttpClient(handler);
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(ProbeTimeout);

                // Warm up, then measure: reuse the same client so later requests ride the
                // kept-alive connection (no repeated tunnel/TLS handshake). Keep the fastest —
                // that's the steady-state RTT, mirroring v2rayN's real-ping.
                var best = -1;
                for (var i = 0; i < ProbeIterations; i++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    using (await client.GetAsync(TestUrl, timeoutCts.Token).ConfigureAwait(false))
                    {
                        stopwatch.Stop();
                    }

                    var ms = (int)Math.Round(stopwatch.Elapsed.TotalMilliseconds);
                    if (ms >= 0 && (best < 0 || ms < best))
                        best = ms;

                    if (i < ProbeIterations - 1)
                        await Task.Delay(InterProbeDelayMs, timeoutCts.Token).ConfigureAwait(false);
                }

                report(server, best);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Timeout, connection refused, proxy/handshake failure → unreachable.
                report(server, -1);
            }
            finally
            {
                throttle.Release();
            }
        }

        /// <summary>
        /// Asks the OS for a free loopback TCP port by binding to port 0 and reading the assigned
        /// port back. There is a tiny race between releasing it here and xray binding it, but it is
        /// the standard ephemeral-port allocation trick and good enough for a short-lived test.
        /// </summary>
        private static int GetFreeLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
