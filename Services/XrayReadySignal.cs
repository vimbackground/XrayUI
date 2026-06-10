using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace XrayUI.Services
{
    /// <summary>
    /// Resolves as soon as a freshly launched xray core reports readiness, replacing the old
    /// fixed startup delays. The primary signal is the "[Warning] core: Xray x.y.z started"
    /// log line, which xray prints only after every inbound (mixed socks/http, TUN device,
    /// the speed-test core's N test ports) has come up. The line goes through xray's logger
    /// at Warning level, so the config loglevel must be debug/info/warning for it to appear —
    /// XrayConfigBuilder guarantees that (see DefaultLogLevel). Verified against the bundled
    /// Xray 26.6.1; if a future core rewords the line, WaitAsync's cap degrades gracefully
    /// to the previous fixed-delay behavior instead of breaking.
    /// </summary>
    internal sealed class XrayReadySignal
    {
        public enum Outcome { Ready, Exited, TimedOut }

        private readonly TaskCompletionSource<Outcome> _outcome =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private XrayReadySignal()
        {
        }

        /// <summary>
        /// Wires a signal onto <paramref name="process"/>: subscribes its own stdout/stderr/
        /// Exited handlers (multicast, so the caller's logging handlers are unaffected) and
        /// turns on <see cref="Process.EnableRaisingEvents"/>. Call before Start().
        /// </summary>
        public static XrayReadySignal Attach(Process process)
        {
            var signal = new XrayReadySignal();
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, e) => signal.OnOutputLine(e.Data);
            process.ErrorDataReceived += (_, e) => signal.OnOutputLine(e.Data);
            process.Exited += (_, _) => signal._outcome.TrySetResult(Outcome.Exited);
            return signal;
        }

        private void OnOutputLine(string? line)
        {
            // Matches "2026/06/10 09:00:54 [Warning] core: Xray 26.6.1 started". Only the
            // startup window awaits this signal, so user traffic in the access log can
            // never race a false positive.
            if (line is not null
                && line.EndsWith(" started", StringComparison.Ordinal)
                && line.Contains("core:", StringComparison.Ordinal))
            {
                _outcome.TrySetResult(Outcome.Ready);
            }
        }

        /// <summary>
        /// Waits for the first of: readiness line (Ready), process death (Exited), or
        /// <paramref name="cap"/> elapsing (TimedOut). Cancellation propagates as
        /// <see cref="OperationCanceledException"/>.
        /// </summary>
        public async Task<Outcome> WaitAsync(TimeSpan cap, CancellationToken ct = default)
        {
            try
            {
                return await _outcome.Task.WaitAsync(cap, ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return Outcome.TimedOut;
            }
        }
    }
}
