using System;
using System.Runtime.InteropServices;

namespace XrayUI.Helpers
{
    /// <summary>
    /// Thin wrapper over user32's global hotkey APIs. Source-generated (LibraryImport) to match
    /// the AOT-friendly interop convention already used in <see cref="JobObjectGuard"/>.
    /// </summary>
    internal static partial class HotkeyInterop
    {
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
