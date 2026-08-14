using QuickActions.Config;
using QuickActions.Core;

namespace QuickActions;

/// <summary>
/// 常驻宿主:系统托盘图标 + 热键分发。无主窗口。
/// 动作执行结果以托盘气泡提示,失败记录日志并气泡报错。
/// </summary>
public sealed class App : IDisposable
{
    private readonly NotifyIcon _tray;
    private readonly HotkeyManager _hotkeys = new();
    private readonly ActionRegistry _registry;
    private readonly Logger _log;

    public App(ActionRegistry registry, Logger log)
    {
        _registry = registry;
        _log = log;

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "QuickActions",
            Visible = true,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("退出", null, (_, _) =>
        {
            _tray.Visible = false;
            Application.Exit();
        });
        _tray.ContextMenuStrip = menu;
    }

    /// <summary>注册全部配置条目;返回失败项列表(热键冲突、格式错误、未知动作),不中断其余注册。</summary>
    public IReadOnlyList<string> RegisterAll(IEnumerable<ConfigEntry> entries)
    {
        var failures = new List<string>();

        foreach (var entry in entries)
        {
            if (!HotkeyParser.TryParse(entry.Hotkey, out var hotkey, out var parseError))
            {
                failures.Add($"'{entry.Hotkey}': {parseError}");
                continue;
            }

            var action = _registry.Find(entry.Action);
            if (action is null)
            {
                failures.Add($"'{entry.Hotkey}': 未知动作 '{entry.Action}'");
                continue;
            }

            if (!_hotkeys.Register(hotkey, () => Execute(action, entry.Args), out var registerError))
            {
                failures.Add($"'{entry.Hotkey}': {registerError}");
                continue;
            }

            _log.Info($"热键 {entry.Hotkey} → {entry.Action} 已注册");
        }

        return failures;
    }

    private void Execute(IAction action, object? args)
    {
        try
        {
            string result = action.Execute(args);
            _log.Info($"[{action.Name}] {result}");
            _tray.ShowBalloonTip(2000, "QuickActions", result, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _log.Error($"[{action.Name}] 执行失败: {ex}");
            _tray.ShowBalloonTip(3000, "QuickActions", $"动作失败: {ex.Message}", ToolTipIcon.Error);
        }
    }

    public void Dispose()
    {
        _hotkeys.Dispose();
        _tray.Dispose();
    }
}
