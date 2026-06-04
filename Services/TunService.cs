using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using XrayUI.Helpers;

namespace XrayUI.Services;

/// <summary>
/// TUN mode service.
/// Currently mainly handles wintun.dll detection and fallback route cleanup:
/// xray adds routes itself at startup with elevated permissions through autoSystemRoutingTable,
/// so this only acts as a fallback for clearing stale routes after an abnormal xray exit.
/// </summary>
public class TunService
{
    private readonly string _engineDirectory;

    private const string TunInterfaceName = XrayConfigConstants.TunInterfaceName;

    public TunService()
    {
        _engineDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "engine");
    }

    /// <summary>Checks whether wintun.dll exists.</summary>
    public bool IsWintunAvailable()
    {
        var wintunPath = Path.Combine(_engineDirectory, "wintun.dll");
        var exists = File.Exists(wintunPath);
        Debug.WriteLine($"[TunService] wintun.dll 路径: {wintunPath}, 存在: {exists}");
        return exists;
    }

    /// <summary>Gets the expected wintun.dll path for error messages.</summary>
    public string GetExpectedWintunPath() => Path.Combine(_engineDirectory, "wintun.dll");

    /// <summary>
    /// Best-effort reset of stale DNS server entries that Windows can persist on the
    /// xray-tun adapter between runs. Uses netsh (fast, ~10ms) so it doesn't stall
    /// startup or shutdown. Safe to call when the adapter doesn't exist — netsh emits
    /// a non-zero exit code but doesn't abort the batch (commands are chained with `&`).
    /// Skipped silently when not elevated: a DNS reset isn't worth a UAC prompt, and
    /// the cleanup-path callers already pay UAC via <see cref="CleanupTunRoutes"/>.
    /// </summary>
    public void ResetTunDnsServers()
    {
        if (!AdminHelper.IsAdministrator())
            return;

        try
        {
            RunElevatedBatch(BuildDnsResetBatch());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TunService] TUN DNS 重置失败: {ex.Message}");
        }
    }

    private static List<string> BuildDnsResetBatch() =>
    [
        $"netsh interface ipv4 set dnsservers \"{TunInterfaceName}\" source=dhcp",
        $"netsh interface ipv6 set dnsservers \"{TunInterfaceName}\" source=dhcp",
    ];

    /// <summary>
    /// Fallback cleanup: xray removes its own routes on normal exit, so this is only used
    /// after an abnormal xray exit or when routes remain after exit. Removes the 0.0.0.0/0
    /// fallback route plus the direct route to the server, and resets any stale DNS entries
    /// on the xray-tun adapter — all in one elevated batch so the user sees at most one UAC.
    /// </summary>
    public void CleanupTunRoutes(string? serverAddress)
    {
        try
        {
            // Older versions left direct routes for these public DNS resolvers; clean
            // them up if they happen to be there. xray no longer adds them.
            string[] legacyDnsServers = ["223.5.5.5", "119.29.29.29"];

            var batch = new List<string>
            {
                // 0.0.0.0/0 is what current xray adds; the /1 split-routes are residue
                // from earlier routing schemes that may still be lying around.
                $"netsh interface ipv4 delete route {XrayConfigConstants.TunAutoRouteV4} \"{TunInterfaceName}\" store=active",
                $"netsh interface ipv4 delete route 0.0.0.0/1 \"{TunInterfaceName}\" store=active",
                $"netsh interface ipv4 delete route 128.0.0.0/1 \"{TunInterfaceName}\" store=active",
                // IPv6 default route — present only when TUN IPv6 was enabled; harmless to delete otherwise.
                $"netsh interface ipv6 delete route {XrayConfigConstants.TunAutoRouteV6} \"{TunInterfaceName}\" store=active",
                // Legacy route.exe form for the same /1 split-routes.
                "route delete 0.0.0.0 mask 128.0.0.0",
                "route delete 128.0.0.0 mask 128.0.0.0",
            };

            // serverAddress may be a host name (e.g. proxy.example.com), but Windows `route delete`
            // does not resolve domains. Skip server-IP cleanup unless it is IPv4.
            if (TryParseSafeIPv4Address(serverAddress, out var serverIPv4))
            {
                batch.Add($"netsh interface ipv4 delete route {serverIPv4}/32 \"{TunInterfaceName}\" store=active");
                batch.Add($"route delete {serverIPv4} mask 255.255.255.255");
            }

            foreach (var dns in legacyDnsServers)
                batch.Add($"route delete {dns} mask 255.255.255.255");

            batch.AddRange(BuildDnsResetBatch());

            RunElevatedBatch(batch);
            Debug.WriteLine("[TunService] TUN 路由兜底清理完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TunService] 清理 TUN 路由失败: {ex.Message}");
        }
    }

    private static bool TryParseSafeIPv4Address(string? value, out string address)
    {
        address = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!IPAddress.TryParse(value, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        address = parsed.ToString();
        return true;
    }

    /// <summary>
    /// Runs a batch of full command lines (e.g. "netsh interface ipv4 ...", "route delete ...")
    /// in a single cmd.exe — chained with `&amp;` so a failure in one doesn't abort the rest.
    /// One UAC prompt total when not already admin; zero when admin.
    /// </summary>
    private static bool RunElevatedBatch(IReadOnlyList<string> commandLines)
    {
        if (commandLines.Count == 0)
            return true;

        var combined = string.Join(" & ", commandLines);
        var cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var isAdmin = AdminHelper.IsAdministrator();

        var psi = new ProcessStartInfo
        {
            FileName = cmdPath,
            Arguments = "/c " + combined,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        if (isAdmin)
        {
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
        }
        else
        {
            psi.UseShellExecute = true;
            psi.Verb = "runas";
        }

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
                return false;

            process.WaitForExit(5000);
            // Exit code reflects only the LAST command in the chain — best-effort cleanup,
            // not an authoritative "all succeeded" signal.
            Debug.WriteLine($"[TunService] cleanup 批处理退出代码: {process.ExitCode}");
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Debug.WriteLine("[TunService] 管理员授权被取消");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TunService] cleanup 批处理执行失败: {ex.Message}");
            return false;
        }
    }
}
