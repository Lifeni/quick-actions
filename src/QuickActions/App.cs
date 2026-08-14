using System.Drawing;
using System.Windows.Forms;
using QuickActions.Config;
using QuickActions.Core;

namespace QuickActions;

/// <summary>
/// 常驻宿主：系统托盘图标 + 热键分发。无主窗口。
/// 动作执行结果以托盘气泡提示（自定义图标），失败记录日志并气泡报错；
/// 未发生实际变化时只记录日志、不弹通知。
/// </summary>
public sealed class App : IDisposable
{
    private readonly TrayIcon _tray;
    private readonly HotkeyManager _hotkeys = new();
    private readonly ActionRegistry _registry;
    private readonly Logger _log;
    private object? _toggleArgs;

    /// <summary>菜单/默认切换参数:与配置默认一致的 internal ↔ extend 循环。</summary>
    private static readonly object DefaultToggleArgs =
        MiniJson.Parse("""{ "mode": "toggle", "modes": ["internal", "extend"] }""");

    public App(ActionRegistry registry, Logger log)
    {
        _registry = registry;
        _log = log;

        var menu = new ContextMenuStrip();
        menu.Items.Add("切换投影显示模式", null, (_, _) =>
            Execute(_registry.Find("display_mode")!, _toggleArgs ?? DefaultToggleArgs));
        menu.Items.Add("退出", null, (_, _) => Application.Exit());

        _tray = new TrayIcon(LoadTrayIcon(), menu);
    }

    /// <summary>注册全部配置条目；返回失败项列表（热键冲突、格式错误、未知动作），不中断其余注册。
    /// 注册完成后弹一次"已在后台运行"通知，列出生效热键。</summary>
    public IReadOnlyList<string> RegisterAll(IEnumerable<ConfigEntry> entries)
    {
        var failures = new List<string>();
        var registered = new List<string>();

        foreach (var entry in entries)
        {
            // 记录第一条 display_mode 条目的参数,托盘菜单"切换投影显示模式"与热键行为保持一致
            if (entry.Action == "display_mode" && _toggleArgs is null)
                _toggleArgs = entry.Args;

            if (!HotkeyParser.TryParse(entry.Hotkey, out var hotkey, out var parseError))
            {
                failures.Add($"“{entry.Hotkey}”：{parseError}");
                continue;
            }

            var action = _registry.Find(entry.Action);
            if (action is null)
            {
                failures.Add($"“{entry.Hotkey}”：未知动作“{entry.Action}”");
                continue;
            }

            if (!_hotkeys.Register(hotkey, () => Execute(action, entry.Args), out var registerError))
            {
                failures.Add($"“{entry.Hotkey}”：{registerError}");
                continue;
            }

            registered.Add(entry.Hotkey);
            _log.Info($"热键 {entry.Hotkey} → {entry.Action} 已注册");
        }

        if (registered.Count > 0)
            _tray.ShowBalloon("QuickActions", $"已在后台运行\n热键：{string.Join("、", registered)}");

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
                _log.Info($"[{action.Name}] {result.Message}（未变化，不提示）");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[{action.Name}] 执行失败: {ex}");
            _tray.ShowBalloon("QuickActions", $"动作失败: {ex.Message}");
        }
    }

    /// <summary>加载嵌入资源中的图标；失败时回退系统默认图标。</summary>
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
