using System.Text.Json;
using QuickActions.Core;
using QuickActions.Interop;

namespace QuickActions.Actions;

/// <summary>
/// 切换投影模式,基于 SetDisplayConfig 直接应用拓扑(比 DisplaySwitch.exe 可靠:同步、可验证结果)。
/// args 支持 {"mode": "internal"} 或字符串 "internal";mode 取值:
/// internal(仅当前屏幕) / extend(扩展) / external(仅外接) / clone(复制)。
/// </summary>
public sealed class DisplayModeAction : IAction
{
    public string Name => "display_mode";

    public string Execute(object? args)
    {
        string mode = ExtractMode(args);
        uint topology = TopologyFor(mode);

        int hr = NativeMethods.SetDisplayConfig(
            0, IntPtr.Zero, 0, IntPtr.Zero,
            topology | NativeMethods.SDC_USE_SUPPLIED_DISPLAY_CONFIG | NativeMethods.SDC_APPLY);

        if (hr != 0)
            throw new InvalidOperationException($"SetDisplayConfig 失败 (Win32 错误 {hr})");

        return $"已切换显示模式: {ModeDisplayName(mode)}";
    }

    internal static string ExtractMode(object? args)
    {
        if (args is string s && !string.IsNullOrWhiteSpace(s))
            return s.Trim().ToLowerInvariant();

        if (args is JsonElement e && e.ValueKind == JsonValueKind.Object
            && e.TryGetProperty("mode", out var mode)
            && mode.ValueKind == JsonValueKind.String)
            return mode.GetString()!.Trim().ToLowerInvariant();

        throw new ArgumentException("缺少 args.mode (internal|extend|external|clone)");
    }

    internal static uint TopologyFor(string mode) => mode switch
    {
        "internal" => NativeMethods.SDC_TOPOLOGY_INTERNAL,
        "extend" => NativeMethods.SDC_TOPOLOGY_EXTEND,
        "external" => NativeMethods.SDC_TOPOLOGY_EXTERNAL,
        "clone" => NativeMethods.SDC_TOPOLOGY_CLONE,
        _ => throw new ArgumentException($"未知显示模式 '{mode}'(支持 internal|extend|external|clone)"),
    };

    private static string ModeDisplayName(string mode) => mode switch
    {
        "internal" => "仅当前屏幕",
        "extend" => "扩展模式",
        "external" => "仅外接屏幕",
        _ => "复制模式",
    };
}
