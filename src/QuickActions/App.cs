using System.Drawing;
using System.Windows.Forms;
using QuickActions.Config;
using QuickActions.Core;

namespace QuickActions;

/// <summary>
/// 常驻宿主:系统托盘图标 + 热键分发。无主窗口。
/// 动作执行结果以托盘气泡提示(自定义图标),失败记录日志并气泡报错;
/// 未发生实际变化时只记录日志、不弹通知。
/// </summary>
public sealed class App : IDisposable
{
    private readonly TrayIcon _tray;
    private readonly HotkeyManager _hotkeys = new();
    private readonly ActionRegistry _registry;
    private readonly Logger _log;

    public App(ActionRegistry registry, Logger log)
    {
        _registry = registry;
        _log = log;

        _tray = new TrayIcon(LoadTrayIcon(), () => Application.Exit());
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
            var result = action.Execute(args);
            if (result.Changed)
            {
                _log.Info($"[{action.Name}] {result.Message}");
                _tray.ShowBalloon("QuickActions", result.Message);
            }
            else
            {
                _log.Info($"[{action.Name}] {result.Message}(未变化,不提示)");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[{action.Name}] 执行失败: {ex}");
            _tray.ShowBalloon("QuickActions", $"动作失败: {ex.Message}");
        }
    }

    /// <summary>加载嵌入资源中的图标;失败时回退系统默认图标。</summary>
    private static Icon LoadTrayIcon()
    {
        try
        {
            using var stream = typeof(App).Assembly.GetManifestResourceStream("QuickActions.Assets.quick-actions.ico");
            if (stream is not null)
                return new Icon(stream);
        }
        catch
        {
            // 回退到系统图标
        }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _hotkeys.Dispose();
        _tray.Dispose();
    }
}
