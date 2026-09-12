using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using XrayUI.Models;

namespace XrayUI.Services
{
    /// <summary>
    /// Reads Xray's inbound traffic counters through the bundled core's statsquery command.
    /// The counters are cumulative; this service converts them to a per-second rate between
    /// successive polls without adding a gRPC/Protobuf dependency to the AOT app.
    /// </summary>
    public sealed class XrayStatsService
    {
        private readonly object _gate = new();
        private long _lastUpload;
        private long _lastDownload;
        private long _lastTimestamp;

        public async Task<ProxyTrafficSnapshot?> QueryInboundTrafficAsync(
            int apiPort,
            CancellationToken ct = default)
        {
            if (apiPort is < 1 or > 65535)
                return null;

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = XrayService.ExePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            process.StartInfo.ArgumentList.Add("api");
            process.StartInfo.ArgumentList.Add("statsquery");
            process.StartInfo.ArgumentList.Add($"--server=127.0.0.1:{apiPort}");
            process.StartInfo.ArgumentList.Add("--pattern=inbound>>>");
            process.StartInfo.ArgumentList.Add("--timeout=1");

            try
            {
                if (!process.Start())
                    return null;

                var outputTask = process.StandardOutput.ReadToEndAsync(ct);
                var errorTask = process.StandardError.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct).ConfigureAwait(false);
                var output = await outputTask.ConfigureAwait(false);
                _ = await errorTask.ConfigureAwait(false);

                if (process.ExitCode != 0)
                    return null;

                return ParseAndCalculate(output);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return null;
            }
            finally
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                _lastUpload = 0;
                _lastDownload = 0;
                _lastTimestamp = 0;
            }
        }

        private ProxyTrafficSnapshot? ParseAndCalculate(string output)
        {
            try
            {
                using var document = JsonDocument.Parse(output);
                if (!document.RootElement.TryGetProperty("stat", out var stats)
                    || stats.ValueKind != JsonValueKind.Array)
                    return null;

                long upload = 0;
                long download = 0;
                foreach (var stat in stats.EnumerateArray())
                {
                    if (!stat.TryGetProperty("name", out var nameElement)
                        || !stat.TryGetProperty("value", out var valueElement)
                        || !valueElement.TryGetInt64(out var value))
                        continue;

                    var name = nameElement.GetString() ?? string.Empty;
                    if (name.EndsWith(">>>traffic>>>uplink", StringComparison.Ordinal))
                        upload = checked(upload + Math.Max(0, value));
                    else if (name.EndsWith(">>>traffic>>>downlink", StringComparison.Ordinal))
                        download = checked(download + Math.Max(0, value));
                }

                var now = Stopwatch.GetTimestamp();
                lock (_gate)
                {
                    long uploadRate = 0;
                    long downloadRate = 0;
                    if (_lastTimestamp != 0)
                    {
                        var elapsed = (now - _lastTimestamp) / (double)Stopwatch.Frequency;
                        if (elapsed > 0)
                        {
                            uploadRate = Rate(upload, _lastUpload, elapsed);
                            downloadRate = Rate(download, _lastDownload, elapsed);
                        }
                    }

                    _lastUpload = upload;
                    _lastDownload = download;
                    _lastTimestamp = now;
                    return new ProxyTrafficSnapshot(upload, download, uploadRate, downloadRate);
                }
            }
            catch (JsonException)
            {
                return null;
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        private static long Rate(long current, long previous, double elapsed)
        {
            if (current < previous)
                return 0;

            var rate = (current - previous) / elapsed;
            return rate >= long.MaxValue ? long.MaxValue : (long)Math.Round(rate);
        }
    }
}
