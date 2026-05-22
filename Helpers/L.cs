namespace XrayUI.Helpers;

/// <summary>
/// Strongly-typed accessors for resource strings. Each property is the canonical
/// way to look up a localized string from C# — compiler catches typos, IDE
/// supports go-to-definition. XAML still uses <c>x:Uid</c> on the same key
/// (the resw entry key matches the property name 1:1).
///
/// Entries are added incrementally as call sites are localized. Empty for now —
/// see commit 3 of the i18n series.
/// </summary>
public static class L
{
}
