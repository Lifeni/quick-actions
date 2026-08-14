using QuickActions.Interop;

namespace QuickActions.Actions;

/// <summary>
/// 只读查询当前显示拓扑(不修改显示状态)。
/// 判定规则:单条激活路径 → target 为内嵌面板则 internal,否则 external;
/// 多条激活路径 → 共享同一 source 则为 clone,否则 extend。
/// </summary>
public static class DisplayTopology
{
    /// <summary>返回当前拓扑名:"internal" | "clone" | "extend" | "external";无法判定时返回 null。</summary>
    public static string? GetCurrentMode()
    {
        try
        {
            // 两趟查询:先取数组大小,再取数据
            uint numPath = 0, numMode = 0;
            int first = NativeMethods.QueryDisplayConfig(
                NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref numPath, null, ref numMode, null, out _);
            if (first != NativeMethods.ERROR_SUCCESS && first != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
                return null;
            if (numPath == 0)
                return null;

            var paths = new DisplayConfigPathInfo[numPath];
            var modes = new DisplayConfigModeInfo[numMode];
            int second = NativeMethods.QueryDisplayConfig(
                NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref numPath, paths, ref numMode, modes, out _);
            if (second != NativeMethods.ERROR_SUCCESS)
                return null;

            int count = (int)Math.Min(numPath, (uint)paths.Length);
            var active = new List<DisplayConfigPathInfo>(count);
            for (int i = 0; i < count; i++)
            {
                if ((paths[i].Flags & NativeMethods.DISPLAYCONFIG_PATH_ACTIVE) != 0)
                    active.Add(paths[i]);
            }

            if (active.Count == 0)
                return null;

            if (active.Count == 1)
                return IsEmbeddedTarget(active[0].TargetInfo.OutputTechnology) ? "internal" : "external";

            var firstSource = active[0].SourceInfo;
            for (int i = 1; i < active.Count; i++)
            {
                var source = active[i].SourceInfo;
                if (source.AdapterIdLow != firstSource.AdapterIdLow
                    || source.AdapterIdHigh != firstSource.AdapterIdHigh
                    || source.SourceId != firstSource.SourceId)
                {
                    return "extend";
                }
            }
            return "clone";
        }
        catch
        {
            // 查询失败时返回 null,调用方按"无法比较"保守处理
            return null;
        }
    }

    private static bool IsEmbeddedTarget(uint outputTechnology)
    {
        if ((outputTechnology & NativeMethods.OUTPUT_TECHNOLOGY_INTERNAL) != 0)
            return true;
        return outputTechnology is NativeMethods.OUTPUT_TECHNOLOGY_LVDS
            or NativeMethods.OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED
            or NativeMethods.OUTPUT_TECHNOLOGY_UDI_EMBEDDED;
    }
}
