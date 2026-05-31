using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XrayUI.Helpers;

namespace XrayUI.Services
{
    public partial class XrayService
    {
        private static readonly string ExePath = Path.Combine(
            AppContext.BaseDirectory, "Assets", "engine", "xray.exe");

        public static readonly string RulesDir = Path.Combine(
            AppContext.BaseDirectory, "Assets", "rules");

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XrayUI", "xray_config.json");

        private const int LogBufferMax = 500;

        private Process? _process;

        // Kernel-level safety net: xray.exe is assigned to this Job Object with
        // KillOnJobClose, so if the UI process dies any way (taskkill /F, AV
        // crash, OOM), the kernel kills xray.exe synchronously. Lifetime = this
        // XrayService instance; reused across Start/Stop cycles.
        private IntPtr _jobHandle = IntPtr.Zero;

        private StringBuilder _startupLog = new();
        private bool _collectStartupLog;
        private readonly Lock _startupLogLock = new();

        // Fixed-size ring buffer: O(1) append + oldest-drop, no array shifting.
        private readonly string[] _logBuffer = new string[LogBufferMax];
        private int _logHead;    // index of the next write slot
        private int _logCount;   // number of valid entries (<= LogBufferMax)
        private readonly Lock _bufferLock = new();

        public bool IsRunning => _process is { HasExited: false };

        public string LastError { get; private set; } = string.Empty;

        public event EventHandler<string>? LogReceived;

        public event EventHandler<bool>? RunningChanged;

        public IReadOnlyList<string> GetLogBuffer()
        {
            lock (_bufferLock)
            {
                if (_logCount == 0)
                {
                    return Array.Empty<string>();
                }

                var snapshot = new string[_logCount];
                if (_logCount < LogBufferMax)
                {
                    // Not yet wrapped — data is contiguous in [0, _logCount)
                    Array.Copy(_logBuffer, 0, snapshot, 0, _logCount);
                }
                else
                {
                    // Wrapped — oldest at _logHead, newest at _logHead-1
                    int tailCount = LogBufferMax - _logHead;
                    Array.Copy(_logBuffer, _logHead, snapshot, 0, tailCount);
                    Array.Copy(_logBuffer, 0, snapshot, tailCount, _logHead);
                }
                return snapshot;
            }
        }

        public void ClearLogBuffer()
        {
            lock (_bufferLock)
            {
                Array.Clear(_logBuffer, 0, _logBuffer.Length);
                _logHead = 0;
                _logCount = 0;
            }
        }

        private void AppendLog(string line)
        {
            lock (_bufferLock)
            {
                _logBuffer[_logHead] = line;
                _logHead = (_logHead + 1) % LogBufferMax;
                if (_logCount < LogBufferMax)
                {
                    _logCount++;
                }
            }

            LogReceived?.Invoke(this, line);
        }

        private void BeginStartupLogCapture()
        {
            lock (_startupLogLock)
            {
                _startupLog = new StringBuilder();
                _collectStartupLog = true;
            }
        }

        private void AppendStartupLog(string line)
        {
            lock (_startupLogLock)
            {
                if (!_collectStartupLog)
                {
                    return;
                }

                _startupLog.AppendLine(line);
            }
        }

        private string StopStartupLogCaptureAndRead()
        {
            lock (_startupLogLock)
            {
                _collectStartupLog = false;
                var text = _startupLog.Length > 0
                    ? _startupLog.ToString().Trim()
                    : string.Empty;
                _startupLog = new StringBuilder();
                return text;
            }
        }

        private void StopStartupLogCapture()
        {
            lock (_startupLogLock)
            {
                _collectStartupLog = false;
                _startupLog = new StringBuilder();
            }
        }

        public async Task<bool> StartAsync(string configJson)
        {
            if (IsRunning)
            {
                // Restart path (e.g. ReapplyRoutingAsync): skip the DNS flush — the new xray
                // session is about to repopulate the resolver cache anyway, and flushing
                // adds avoidable latency to every routing/DNS/proxy-mode toggle.
                await StopCoreAsync();
            }

            LastError = string.Empty;

            if (!File.Exists(ExePath))
            {
                LastError = Loc.Format("Xray_ExeNotFound", ExePath);
                AppendLog(Loc.Format("XrayLog_Error", LastError));
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                await File.WriteAllTextAsync(ConfigPath, configJson);

                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Arguments = $"run -config \"{ConfigPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(ExePath)!,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                psi.EnvironmentVariables["XRAY_LOCATION_ASSET"] = RulesDir;

                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                _process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data is null) return;
                    AppendStartupLog(e.Data);
                    AppendLog(e.Data);
                };

                _process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data is null) return;
                    AppendStartupLog(e.Data);
                    AppendLog(e.Data);
                };

                _process.Exited += OnProcessExited;

                BeginStartupLogCapture();
                _process.Start();
                AttachToJobObject(_process);
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                AppendLog(Loc.Format("XrayLog_Started", ExePath));
                AppendLog(Loc.Format("XrayLog_Config", ConfigPath));

                await Task.Delay(800);

                if (_process.HasExited)
                {
                    var startupLog = StopStartupLogCaptureAndRead();
                    LastError = startupLog.Length > 0
                        ? startupLog
                        : Loc.Format("Xray_ExitedImmediately", _process.ExitCode);
                    AppendLog(Loc.Format("XrayLog_StartFailed", LastError));
                    return false;
                }

                StopStartupLogCapture();
                RunningChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                StopStartupLogCapture();
                LastError = ex.Message;
                AppendLog(Loc.Format("XrayLog_Exception", ex.Message));
                return false;
            }
        }

        public async Task StopAsync()
        {
            await StopCoreAsync();
            FlushSystemDnsCache();
        }

        /// <summary>
        /// Kills the xray process and tears down state, without flushing the OS DNS cache.
        /// Used by StartAsync on the restart path so reapply doesn't pay DNS flush latency
        /// on every routing/DNS/proxy-mode toggle. No-op if not running.
        /// </summary>
        private async Task StopCoreAsync()
        {
            if (_process is null)
            {
                return;
            }

            _process.Exited -= OnProcessExited;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }

                await _process.WaitForExitAsync();
            }
            catch
            {
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }

            AppendLog(L.XrayLog_Stopped);
            RunningChanged?.Invoke(this, false);
        }

        /// <summary>
        /// Best-effort DNS resolver cache flush to clear any cached fake IPs (198.18.0.0/15) that
        /// might linger in the Windows resolver cache after a FakeDNS-enabled run. Harmless
        /// when FakeDNS was not used. Runs unconditionally on every stop to keep XrayService
        /// stateless w.r.t. last-run config.
        /// </summary>
        private void FlushSystemDnsCache()
        {
            try
            {
                if (!DnsFlushResolverCache())
                {
                    Debug.WriteLine($"[DNS] DnsFlushResolverCache failed: {Marshal.GetLastWin32Error()}");
                }
            }
            catch (Exception ex)
            {
                // best-effort, never block stop on this
                Debug.WriteLine($"[DNS] DnsFlushResolverCache exception: {ex.Message}");
            }
        }

        [DllImport("dnsapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DnsFlushResolverCache();

        public void StopForShutdown()
        {
            var process = _process;
            if (process is null)
            {
                return;
            }

            process.Exited -= OnProcessExited;
            _process = null;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(500);
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }

            AppendLog(L.XrayLog_Shutdown);
            RunningChanged?.Invoke(this, false);
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            AppendLog(L.XrayLog_ProcessExited);
            RunningChanged?.Invoke(this, false);
        }

        // ─────────── Job Object: orphan-xray safety net ───────────

        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
        private const int JobObjectExtendedLimitInformation = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr CreateJobObjectW(IntPtr lpJobAttributes, IntPtr lpName);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetInformationJobObject(
            IntPtr hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(IntPtr hObject);

        private void AttachToJobObject(Process process)
        {
            try
            {
                if (_jobHandle == IntPtr.Zero)
                {
                    _jobHandle = CreateKillOnCloseJob();
                    if (_jobHandle == IntPtr.Zero)
                    {
                        AppendLog(Loc.Format("XrayLog_JobCreateFailed", Marshal.GetLastWin32Error()));
                        return;
                    }
                }

                if (!AssignProcessToJobObject(_jobHandle, process.Handle))
                {
                    AppendLog(Loc.Format("XrayLog_JobAssignFailed", Marshal.GetLastWin32Error()));
                }
            }
            catch (Exception ex)
            {
                AppendLog(Loc.Format("XrayLog_JobBindException", ex.Message));
            }
        }

        private static IntPtr CreateKillOnCloseJob()
        {
            var handle = CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
            if (handle == IntPtr.Zero) return IntPtr.Zero;

            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };

            int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            IntPtr buf = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(info, buf, fDeleteOld: false);
                if (!SetInformationJobObject(handle, JobObjectExtendedLimitInformation, buf, (uint)size))
                {
                    _ = CloseHandle(handle);
                    return IntPtr.Zero;
                }
                return handle;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }
    }
}
