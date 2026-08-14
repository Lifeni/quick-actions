using System.Runtime.InteropServices;

namespace QuickActions.Interop;

// DISPLAYCONFIG_* 结构体:字段顺序与原生定义一致,Sequential 布局。
// 仅用于 QueryDisplayConfig 读取路径/模式信息,不需要完整定义所有联合字段。

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigRational
{
    public uint Numerator;
    public uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathSourceInfo
{
    public uint AdapterIdLow;
    public uint AdapterIdHigh;
    public uint SourceId;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathTargetInfo
{
    public uint AdapterIdLow;
    public uint AdapterIdHigh;
    public uint TargetId;
    public uint OutputTechnology;
    public uint Rotation;
    public uint Scaling;
    public DisplayConfigRational RefreshRate;
    public uint ScanLineOrdering;
    public int TargetAvailable;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathInfo
{
    public DisplayConfigPathSourceInfo SourceInfo;
    public DisplayConfigPathTargetInfo TargetInfo;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigModeInfo
{
    public uint InfoType;
    public uint Id;
    public uint AdapterIdLow;
    public uint AdapterIdHigh;

    // DISPLAYCONFIG_TARGET_MODE / SOURCE_MODE 联合,占位 44 字节(两结构最大者)
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)]
    public byte[] ModeInfo;
}
