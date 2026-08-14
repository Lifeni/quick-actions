using System.Runtime.InteropServices;

namespace QuickActions.Interop;

// NOTIFYICONDATA v3(与 Win32 布局一致;x64 下 cbSize 应为 1080)
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NotifyIconData
{
    public uint CbSize;
    public IntPtr Hwnd;
    public uint Id;
    public uint Flags;
    public uint CallbackMessage;
    public IntPtr Icon;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Tip;

    public uint State;
    public uint StateMask;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Info;

    public uint TimeoutOrVersion;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string InfoTitle;

    public uint InfoFlags;
}

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;

    // RegisterHotKey modifiers
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    // SetDisplayConfig topology flags
    public const uint SDC_TOPOLOGY_INTERNAL = 0x00000001;
    public const uint SDC_TOPOLOGY_CLONE = 0x00000002;
    public const uint SDC_TOPOLOGY_EXTEND = 0x00000004;
    public const uint SDC_TOPOLOGY_EXTERNAL = 0x00000008;
    public const uint SDC_APPLY = 0x00000080;

    // QueryDisplayConfig flags
    public const uint QDC_DATABASE_CURRENT = 0x00000004;

    public const uint ERROR_SUCCESS = 0;
    public const uint ERROR_INSUFFICIENT_BUFFER = 122;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetDisplayConfig(
        uint numPathArrayElements,
        IntPtr pathArray,
        uint numModeInfoArrayElements,
        IntPtr modeInfoArray,
        uint flags);

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(
        uint flags,
        ref uint numPathArrayElements,
        ref uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [In, Out] DisplayConfigPathInfo[]? pathArray,
        ref uint numModeInfoArrayElements,
        [In, Out] DisplayConfigModeInfo[]? modeInfoArray,
        out uint currentTopologyId);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpdata);
}
