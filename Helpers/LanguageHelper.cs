using System;
using System.Diagnostics;
using Microsoft.Windows.Globalization;

namespace XrayUI.Helpers
{
    /// <summary>
    /// One row in <see cref="LanguageHelper.SupportedLanguages"/>. <c>Tag = null</c>
    /// means "follow system" (no <c>PrimaryLanguageOverride</c> applied).
    /// Top-level (not nested) so XAML's <c>x:DataType</c> can reference it directly.
    ///
    /// Real language rows pass a fixed <paramref name="displayName"/> endonym
    /// (the language's name in its own script — "简体中文", "English") so that
    /// users always see their own language spelled the way they recognize it,
    /// regardless of the currently-applied UI locale. The "follow system" row is
    /// the exception: it isn't a language name, it's an action description, so it
    /// passes a <c>resourceKey</c> and the display string is resolved against the
    /// current UI locale instead.
    /// </summary>
    public sealed partial class LanguageInfo
    {
        private readonly string _displayName;
        private readonly string? _resourceKey;

        public string? Tag { get; }

        public string DisplayName =>
            _resourceKey is null ? _displayName : Loc.GetString(_resourceKey);

        public LanguageInfo(string? tag, string displayName, string? resourceKey = null)
        {
            Tag = tag;
            _displayName = displayName;
            _resourceKey = resourceKey;
        }
    }

    /// <summary>
    /// Drives the WinAppSDK <see cref="ApplicationLanguages.PrimaryLanguageOverride"/>.
    /// Adding a new language means adding one row to <see cref="SupportedLanguages"/> —
    /// the UI dropdown, persisted-setting normalization and index lookups all read
    /// from this single table.
    ///
    /// Index 0 is the "follow system" choice (Tag = null): when applied, no override
    /// is set and WinAppSDK falls back to the OS locale.
    /// </summary>
    public static class LanguageHelper
    {
        public static readonly LanguageInfo[] SupportedLanguages =
        [
            // Index 0 — "follow system" is an action label, not a language name, so
            // it must follow the current UI locale (not stay in Chinese forever).
            new(null,    "跟随系统", resourceKey: "Language_FollowSystem"),
            new("zh-CN", "简体中文"),
            new("en-US", "English"),
        ];

        /// <summary>
        /// Returns the canonical tag if supported. A <c>null</c> return is also the
        /// signal to skip <see cref="ApplyOverride"/> (i.e. follow system) — that
        /// mapping is deliberate, not an error case.
        /// </summary>
        public static string? Normalize(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            foreach (var lang in SupportedLanguages)
            {
                if (lang.Tag is not null
                    && string.Equals(lang.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return lang.Tag;
            }
            return null;
        }

        /// <summary>
        /// 0-based index in <see cref="SupportedLanguages"/>. <c>null</c> and unknown
        /// tags both map to index 0 ("follow system"), keeping the UI dropdown
        /// truthful when no explicit choice has been persisted.
        /// </summary>
        public static int IndexOf(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0;
            for (int i = 0; i < SupportedLanguages.Length; i++)
            {
                if (string.Equals(SupportedLanguages[i].Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }

        /// <summary>Tag at the given index. Index 0 ("follow system") returns <c>null</c>.</summary>
        public static string? TagAt(int index)
            => (uint)index < (uint)SupportedLanguages.Length
                ? SupportedLanguages[index].Tag
                : null;

        /// <summary>
        /// Applies the WinAppSDK language override. <c>null</c> / unsupported means
        /// "follow system", which must explicitly clear any previously-persisted
        /// override. Must be called before any XAML resource resolution.
        /// </summary>
        public static void ApplyOverride(string? tag)
        {
            var language = Normalize(tag);

            try
            {
                ApplicationLanguages.PrimaryLanguageOverride = language ?? string.Empty;
            }
            catch (Exception ex)
            {
                var label = string.IsNullOrEmpty(language) ? "follow system" : language;
                Debug.WriteLine($"[Language] Failed to apply '{label}': {ex.Message}");
            }
        }
    }
}
