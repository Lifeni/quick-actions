using QuickActions.Actions;
using QuickActions.Config;
using QuickActions.Core;

namespace QuickActions;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string baseDir = AppContext.BaseDirectory;
        using var log = Logger.Open(Path.Combine(baseDir, "data"));
        log.Info("QuickActions 启动");

        bool smoke = args.Contains("--smoke", StringComparer.OrdinalIgnoreCase);

        // 1. 确保配置存在(首次运行自举默认配置)
        var configStore = new ConfigStore(Path.Combine(baseDir, "config", "config.json"));
        configStore.EnsureExists(log);

        // 2. 加载配置;失败时弹框提示,必须人工修复
        List<ConfigEntry> entries;
        try
        {
            entries = configStore.Load();
        }
        catch (Exception ex)
        {
            log.Error($"配置加载失败: {ex}");
            if (!smoke)
                MessageBox.Show($"配置文件加载失败:\n{ex.Message}\n\n请检查 {configStore.Path}",
                    "QuickActions", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        // 3. 注册内置动作
        var registry = new ActionRegistry();
        registry.Register(new DisplayModeAction());

        // 4. 注册热键,启动宿主
        using var app = new App(registry, log);
        var failures = app.RegisterAll(entries);
        foreach (var failure in failures)
            log.Error($"注册失败: {failure}");

        if (smoke)
        {
            string? current = DisplayTopology.GetCurrentMode();
            log.Info($"smoke: 配置条目 {entries.Count},注册失败 {failures.Count},当前拓扑 {current ?? "未知"}");
            return failures.Count == 0 ? 0 : 1;
        }

        if (failures.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, failures),
                "QuickActions - 部分热键注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        Application.Run();
        log.Info("退出");
        return 0;
    }
}
