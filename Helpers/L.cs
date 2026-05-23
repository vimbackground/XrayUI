namespace XrayUI.Helpers;

/// <summary>
/// Strongly-typed accessors for resource strings. Each property is the canonical
/// way to look up a localized string from C# — compiler catches typos, IDE
/// supports go-to-definition. XAML still uses <c>x:Uid</c> on the same key
/// (the resw entry key for XAML carries a property suffix like <c>.Text</c>;
/// the C# key here matches the bare name).
///
/// Entries are added incrementally as call sites are localized.
/// </summary>
public static class L
{
    // ── Generic dialog buttons ─────────────────────────────────────────────
    public static string Dialog_OK        => Loc.GetString("Dialog_OK");
    public static string Dialog_Cancel    => Loc.GetString("Dialog_Cancel");
    public static string Dialog_Save      => Loc.GetString("Dialog_Save");
    public static string Dialog_Done      => Loc.GetString("Dialog_Done");
    public static string Dialog_Confirm   => Loc.GetString("Dialog_Confirm");
    public static string Dialog_Add       => Loc.GetString("Dialog_Add");
    public static string Dialog_Delete    => Loc.GetString("Dialog_Delete");
    public static string Dialog_Replace   => Loc.GetString("Dialog_Replace");
    public static string Dialog_Preparing => Loc.GetString("Dialog_Preparing");
    public static string Dialog_On        => Loc.GetString("Dialog_On");
    public static string Dialog_Off       => Loc.GetString("Dialog_Off");

    // ── Confirmation dialogs ───────────────────────────────────────────────
    public static string Confirm_ReplaceTitle => Loc.GetString("Confirm_ReplaceTitle");
    public static string Confirm_ReplaceMsg   => Loc.GetString("Confirm_ReplaceMsg");

    // ── Import link dialog ─────────────────────────────────────────────────
    public static string Import_Title       => Loc.GetString("Import_Title");
    public static string Import_Placeholder => Loc.GetString("Import_Placeholder");
    public static string Import_SupportHint => Loc.GetString("Import_SupportHint");

    // ── Edit server dialog ─────────────────────────────────────────────────
    public static string EditServer_AddTitle         => Loc.GetString("EditServer_AddTitle");
    public static string EditServer_EditTitle        => Loc.GetString("EditServer_EditTitle");
    public static string EditServer_Name             => Loc.GetString("EditServer_Name");
    public static string EditServer_Address          => Loc.GetString("EditServer_Address");
    public static string EditServer_Port             => Loc.GetString("EditServer_Port");
    public static string EditServer_Protocol         => Loc.GetString("EditServer_Protocol");
    public static string EditServer_Encryption       => Loc.GetString("EditServer_Encryption");
    public static string EditServer_Password         => Loc.GetString("EditServer_Password");
    public static string EditServer_Transport        => Loc.GetString("EditServer_Transport");
    public static string EditServer_Path             => Loc.GetString("EditServer_Path");
    public static string EditServer_WsHost           => Loc.GetString("EditServer_WsHost");
    public static string EditServer_Security         => Loc.GetString("EditServer_Security");
    public static string EditServer_Fingerprint      => Loc.GetString("EditServer_Fingerprint");
    public static string EditServer_AllowInsecure    => Loc.GetString("EditServer_AllowInsecure");
    public static string EditServer_FlowPlaceholder  => Loc.GetString("EditServer_FlowPlaceholder");

    // ── Edit port dialog ───────────────────────────────────────────────────
    public static string EditPort_Title  => Loc.GetString("EditPort_Title");
    public static string EditPort_Header => Loc.GetString("EditPort_Header");

    // ── TUN mode ───────────────────────────────────────────────────────────
    public static string Tun_EnableTitle => Loc.GetString("Tun_EnableTitle");

    // ── Share link dialog ──────────────────────────────────────────────────
    public static string Share_Title    => Loc.GetString("Share_Title");
    public static string Share_CopyLink => Loc.GetString("Share_CopyLink");

    // ── Startup dialog ─────────────────────────────────────────────────────
    public static string Startup_Title       => Loc.GetString("Startup_Title");
    public static string Startup_AutoStart   => Loc.GetString("Startup_AutoStart");
    public static string Startup_AutoConnect => Loc.GetString("Startup_AutoConnect");

    // ── Edit server dialog (extras not in stash) ───────────────────────────
    public static string EditServer_SocksUsername        => Loc.GetString("EditServer_SocksUsername");
    public static string EditServer_EchPlaceholder       => Loc.GetString("EditServer_EchPlaceholder");
    public static string EditServer_FinalmaskPlaceholder => Loc.GetString("EditServer_FinalmaskPlaceholder");

    // ── Chain proxy dialog ─────────────────────────────────────────────────
    public static string ChainProxy_AddTitle  => Loc.GetString("ChainProxy_AddTitle");
    public static string ChainProxy_EditTitle => Loc.GetString("ChainProxy_EditTitle");

    // ── MainViewModel ──────────────────────────────────────────────────────
    public static string Main_NoSelection  => Loc.GetString("Main_NoSelection");
    public static string Main_NotConnected => Loc.GetString("Main_NotConnected");

    // ── ControlPanel ───────────────────────────────────────────────────────
    public static string ControlPanel_Start              => Loc.GetString("ControlPanel_Start");
    public static string ControlPanel_Stop               => Loc.GetString("ControlPanel_Stop");
    public static string ControlPanel_StatusApplying     => Loc.GetString("ControlPanel_StatusApplying");
    public static string ControlPanel_StatusNotRunning   => Loc.GetString("ControlPanel_StatusNotRunning");
    public static string ControlPanel_RoutingGlobal      => Loc.GetString("ControlPanel_RoutingGlobal");
    public static string ControlPanel_RoutingSmart       => Loc.GetString("ControlPanel_RoutingSmart");
    public static string ControlPanel_UpdateFound        => Loc.GetString("ControlPanel_UpdateFound");

    // ── Error dialogs ──────────────────────────────────────────────────────
    public static string Error_NoServer            => Loc.GetString("Error_NoServer");
    public static string Error_NoServerMsg         => Loc.GetString("Error_NoServerMsg");
    public static string Error_StartFailed         => Loc.GetString("Error_StartFailed");
    public static string Error_XrayStartFailed     => Loc.GetString("Error_XrayStartFailed");
    public static string Error_ReapplyFailed       => Loc.GetString("Error_ReapplyFailed");
    public static string Error_XrayReapplyFailed   => Loc.GetString("Error_XrayReapplyFailed");
    public static string Error_UpdateFailed        => Loc.GetString("Error_UpdateFailed");
    public static string Error_UpdaterLaunchFailed => Loc.GetString("Error_UpdaterLaunchFailed");

    // ── Startup / Update / TUN ─────────────────────────────────────────────
    public static string Startup_SetFailed => Loc.GetString("Startup_SetFailed");
    public static string Update_Updating   => Loc.GetString("Update_Updating");
    public static string Tun_EnableMsg     => Loc.GetString("Tun_EnableMsg");

    // ── DNS settings dialog ────────────────────────────────────────────────
    public static string Dns_DialogTitle        => Loc.GetString("Dns_DialogTitle");
    public static string Dns_ResetDefaults      => Loc.GetString("Dns_ResetDefaults");
    public static string Dns_ServerPlaceholder  => Loc.GetString("Dns_ServerPlaceholder");
    public static string Dns_QueryStrategyLabel => Loc.GetString("Dns_QueryStrategyLabel");
    public static string Dns_EnableCacheLabel   => Loc.GetString("Dns_EnableCacheLabel");
    public static string Dns_DirectTitle        => Loc.GetString("Dns_DirectTitle");
    public static string Dns_DirectDesc         => Loc.GetString("Dns_DirectDesc");
    public static string Dns_ProxyTitle         => Loc.GetString("Dns_ProxyTitle");
    public static string Dns_ProxyDesc          => Loc.GetString("Dns_ProxyDesc");
    public static string Dns_TunOnlyHint        => Loc.GetString("Dns_TunOnlyHint");
    public static string Dns_Experimental       => Loc.GetString("Dns_Experimental");
    public static string Dns_Provider_Ali       => Loc.GetString("Dns_Provider_Ali");
    public static string Dns_Provider_Tencent   => Loc.GetString("Dns_Provider_Tencent");
    public static string Dns_Provider_Google    => Loc.GetString("Dns_Provider_Google");
    public static string Dns_Strategy_V4Only    => Loc.GetString("Dns_Strategy_V4Only");
    public static string Dns_Strategy_V6Only    => Loc.GetString("Dns_Strategy_V6Only");
    public static string Dns_Strategy_Auto      => Loc.GetString("Dns_Strategy_Auto");
}
