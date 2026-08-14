using System.Text.Json;

namespace QuickActions.Config;

/// <summary>配置条目:热键 → 动作 + 参数。</summary>
public sealed class ConfigEntry
{
    public required string Hotkey { get; init; }
    public required string Action { get; init; }
    public object? Args { get; init; }
}

/// <summary>
/// 配置读写。运行时文件在 exe 旁 config/config.json;首次启动自举写入默认配置,
/// 因此单文件发布也能正常工作(不依赖 Content 提取)。
/// </summary>
public sealed class ConfigStore
{
    private static readonly string DefaultConfigJson =
        """
        [
          { "hotkey": "F13", "action": "display_mode", "args": { "mode": "internal" } },
          { "hotkey": "F14", "action": "display_mode", "args": { "mode": "extend" } }
        ]
        """;

    public string Path { get; }

    public ConfigStore(string path) => Path = path;

    /// <summary>配置文件不存在时创建默认配置。已存在则不动(保留用户修改)。</summary>
    public void EnsureExists(Logger log)
    {
        if (File.Exists(Path)) return;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, DefaultConfigJson);
        log.Info($"已创建默认配置: {Path}");
    }

    public List<ConfigEntry> Load()
    {
        string json = File.ReadAllText(Path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<ConfigEntry>>(json, options)
            ?? throw new InvalidDataException("配置内容为空");
    }
}
