namespace XrayUI.Helpers
{
    // Test-side stand-ins for the WinAppSDK ResourceLoader-backed localization
    // facade (Helpers/L.cs, Helpers/Loc.cs). Only the members that linked
    // production sources actually touch are stubbed; add members here when a
    // newly linked file references more of L.*.
    public static class L
    {
        public static string ServerDetail_Timeout => "Timeout";
        public static string Subscription_NeverUpdated => "Never updated";
        public static string Subscription_JustNow => "Just now";
        public static string Chain_NeedServerList => "Chain needs server list";
        public static string Chain_EndpointMissing => "Chain endpoint missing";
        public static string Chain_NoNesting => "Chain nesting not supported";
    }

    public static class Loc
    {
        public static string Format(string key, params object?[] args) =>
            $"{key}({string.Join(",", args)})";
    }
}

namespace Microsoft.UI.Xaml
{
    public enum Visibility
    {
        Visible = 0,
        Collapsed = 1
    }
}
