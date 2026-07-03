using System;
using System.Text;
using XrayUI.Models;

namespace XrayUI.Helpers
{
    /// <summary>
    /// Runtime mutable store for the two global hotkeys (toggle start/stop, restore from tray).
    /// Loaded from AppSettings at startup; updated live by the Personalize page so MainWindow
    /// can re-register with user32 immediately, before Done persists the change — mirrors
    /// <see cref="ProtocolColorStore"/>. No separate enabled flag: a hotkey is active whenever
    /// its VirtualKey is non-zero (assigned), matching PowerToys' shortcut behavior.
    /// </summary>
    public static class GlobalHotkeyStore
    {
        public const int ToggleId = 1;
        public const int RestoreId = 2;

        public const uint ModAlt = 0x0001;
        public const uint ModControl = 0x0002;
        public const uint ModShift = 0x0004;
        public const uint ModWin = 0x0008;

        public static uint ToggleModifiers { get; set; }
        public static uint ToggleVirtualKey { get; set; }

        public static uint RestoreModifiers { get; set; }
        public static uint RestoreVirtualKey { get; set; }

        public static event EventHandler? HotkeysChanged;
        public static void NotifyHotkeysChanged() => HotkeysChanged?.Invoke(null, EventArgs.Empty);

        /// <summary>Reads the combo for <see cref="ToggleId"/> or <see cref="RestoreId"/> — lets
        /// callers that already branch on id (MainWindow's registration loop, the Personalize
        /// hotkey dialog) avoid a separate Toggle/Restore branch just to pick the right fields.</summary>
        public static (uint Mods, uint Vk) GetCombo(int id) => id switch
        {
            ToggleId => (ToggleModifiers, ToggleVirtualKey),
            RestoreId => (RestoreModifiers, RestoreVirtualKey),
            _ => (0, 0),
        };

        /// <summary>See <see cref="GetCombo"/>.</summary>
        public static void SetCombo(int id, uint mods, uint vk)
        {
            if (id == ToggleId) { ToggleModifiers = mods; ToggleVirtualKey = vk; }
            else if (id == RestoreId) { RestoreModifiers = mods; RestoreVirtualKey = vk; }
        }

        public static void LoadFrom(AppSettings s)
        {
            (ToggleModifiers, ToggleVirtualKey) = ParseCombo(s.HotkeyToggleCombo);
            (RestoreModifiers, RestoreVirtualKey) = ParseCombo(s.HotkeyRestoreCombo);
        }

        public static void SaveTo(AppSettings s)
        {
            s.HotkeyToggleCombo = FormatCombo(ToggleModifiers, ToggleVirtualKey);
            s.HotkeyRestoreCombo = FormatCombo(RestoreModifiers, RestoreVirtualKey);
        }

        private static (uint mods, uint vk) ParseCombo(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return (0, 0);
            var parts = raw.Split(':');
            if (parts.Length == 2 &&
                uint.TryParse(parts[0], out var mods) &&
                uint.TryParse(parts[1], out var vk))
                return (mods, vk);
            return (0, 0);
        }

        private static string? FormatCombo(uint mods, uint vk) =>
            vk == 0 ? null : $"{mods}:{vk}";

        /// <summary>Friendly display like "Ctrl+Alt+S" — deliberately not localized; keyboard
        /// shortcuts are conventionally shown in Latin abbreviations even on non-English Windows.
        /// Returns "" when no key is set.</summary>
        public static string FormatDisplay(uint mods, uint vk)
        {
            if (vk == 0) return "";

            var sb = new StringBuilder();
            if ((mods & ModControl) != 0) sb.Append("Ctrl+");
            if ((mods & ModAlt) != 0) sb.Append("Alt+");
            if ((mods & ModShift) != 0) sb.Append("Shift+");
            if ((mods & ModWin) != 0) sb.Append("Win+");
            sb.Append(KeyName(vk));
            return sb.ToString();
        }

        // VK_OEM_* codes are positional (US QWERTY) rather than character-based, so ToUnicode/
        // the active keyboard layout would be needed for a fully layout-correct label. Hardcoding
        // the US QWERTY glyph is the same simplification most editors' keybinding UIs make, and is
        // good enough for a 2-hotkey feature — not worth pulling in MapVirtualKey/ToUnicode interop.
        private static string KeyName(uint vk) => vk switch
        {
            >= 0x30 and <= 0x39 => ((char)vk).ToString(), // 0-9
            >= 0x41 and <= 0x5A => ((char)vk).ToString(), // A-Z
            >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",        // F1-F24
            >= 0x60 and <= 0x69 => $"Num{vk - 0x60}",      // Numpad 0-9
            0x6A => "Num*",
            0x6B => "Num+",
            0x6D => "Num-",
            0x6E => "Num.",
            0x6F => "Num/",
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x14 => "CapsLock",
            0x20 => "Space",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x2C => "PrtScn",
            0x2D => "Insert",
            0x2E => "Delete",
            0xBA => ";",
            0xBB => "=",
            0xBC => ",",
            0xBD => "-",
            0xBE => ".",
            0xBF => "/",
            0xC0 => "`",
            0xDB => "[",
            0xDC => "\\",
            0xDD => "]",
            0xDE => "'",
            _ => $"Key{vk}"
        };
    }
}
