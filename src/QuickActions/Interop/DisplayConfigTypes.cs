using System.Runtime.InteropServices;

namespace QuickActions.Interop;

// DISPLAYCONFIG_PATH_INFO / MODE_INFO:仅作为 QueryDisplayConfig 的缓冲区类型,
// 拓扑判定直接使用系统返回的 DISPLAYCONFIG_TOPOLOGY_ID,不读取路径字段。

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] // DISPLAYCONFIG_PATH_SOURCE_INFO
    public byte[] SourceInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)] // DISPLAYCONFIG_PATH_TARGET_INFO
    public byte[] TargetInfo;

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

    // 原生结构含 UINT64,对齐 8,总大小 64(联合 44 后补 4 字节 padding)
    public uint Padding;
}
