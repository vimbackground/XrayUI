using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using XrayUI.Helpers;

namespace XrayUI.Views
{
    /// <summary>
    /// Pure capture UI for a global hotkey combo — no Win32/registration knowledge. The caller
    /// (PersonalizeControl code-behind) is responsible for probing/registering with user32 after
    /// the hosting dialog closes, and only committing on success.
    /// </summary>
    public sealed partial class HotkeyRecorderControl : UserControl
    {
        public uint Mods { get; private set; }
        public uint Vk { get; private set; }

        /// <summary>Fires the first time a valid combo (has at least one modifier) is captured —
        /// lets the hosting dialog enable its Save button.</summary>
        public event EventHandler? ComboCaptured;

        public HotkeyRecorderControl(uint initialMods, uint initialVk)
        {
            InitializeComponent();
            Mods = initialMods;
            Vk = initialVk;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            RecorderText.Text = Vk == 0
                ? L.Personalize_HotkeyRecorderPlaceholder
                : GlobalHotkeyStore.FormatDisplay(Mods, Vk);
        }

        private void RecorderButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsModifierKey(e.Key)) { e.Handled = true; return; }

            e.Handled = true;
            var mods = CurrentModifiers();
            if (mods == 0)
            {
                ErrorInfoBar.Message = L.Personalize_HotkeyInvalidMsg;
                ErrorInfoBar.IsOpen = true;
                return;
            }

            ErrorInfoBar.IsOpen = false;
            Mods = mods;
            Vk = (uint)e.Key;
            UpdateDisplay();
            ComboCaptured?.Invoke(this, EventArgs.Empty);
        }

        private static bool IsModifierKey(VirtualKey key) => key is
            VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl or
            VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu or
            VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift or
            VirtualKey.LeftWindows or VirtualKey.RightWindows;

        private static uint CurrentModifiers()
        {
            uint mods = 0;
            if (IsDown(VirtualKey.Control)) mods |= GlobalHotkeyStore.ModControl;
            if (IsDown(VirtualKey.Menu)) mods |= GlobalHotkeyStore.ModAlt;
            if (IsDown(VirtualKey.Shift)) mods |= GlobalHotkeyStore.ModShift;
            if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows)) mods |= GlobalHotkeyStore.ModWin;
            return mods;

            static bool IsDown(VirtualKey key) =>
                InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
        }
    }
}
