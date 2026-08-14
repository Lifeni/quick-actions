using QuickActions.Config;

namespace QuickActions.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "qa-tests-" + Guid.NewGuid().ToString("N"));

    public ConfigStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private ConfigStore NewStore() => new(Path.Combine(_dir, "config.json"));

    [Fact]
    public void Load_ValidJson_ReturnsEntries()
    {
        File.WriteAllText(NewStore().Path,
            """
            [
              { "hotkey": "F13", "action": "display_mode", "args": { "mode": "internal" } },
              { "hotkey": "Ctrl+F14", "action": "display_mode", "args": { "mode": "extend" } }
            ]
            """);

        var entries = NewStore().Load();

        Assert.Equal(2, entries.Count);
        Assert.Equal("F13", entries[0].Hotkey);
        Assert.Equal("display_mode", entries[0].Action);
        Assert.NotNull(entries[0].Args);
    }

    [Fact]
    public void Load_MissingRequiredFields_Throws()
    {
        File.WriteAllText(NewStore().Path, """[ { "hotkey": "F13" } ]""");

        Assert.ThrowsAny<Exception>(() => NewStore().Load());
    }

    [Fact]
    public void Load_InvalidJson_Throws()
    {
        File.WriteAllText(NewStore().Path, "{ not json");

        Assert.ThrowsAny<Exception>(() => NewStore().Load());
    }

    [Fact]
    public void EnsureExists_CreatesDefaultConfig()
    {
        var store = NewStore();
        var log = TestLog.Null;

        store.EnsureExists(log);

        Assert.True(File.Exists(store.Path));
        var entries = store.Load();
        Assert.NotEmpty(entries);
    }

    [Fact]
    public void EnsureExists_KeepsExistingFile()
    {
        var store = NewStore();
        File.WriteAllText(store.Path, """[ { "hotkey": "F13", "action": "display_mode" } ]""");
        var log = TestLog.Null;

        store.EnsureExists(log);

        var entries = store.Load();
        Assert.Single(entries);
    }
}

/// <summary>测试用空实现，避免依赖真实文件系统日志。</summary>
internal static class TestLog
{
    public static Logger Null => Logger.Null;
}
