namespace QuickActions.Core;

/// <summary>
/// 一键动作接口。新动作:实现此接口并在 Program 启动时注册到 <see cref="ActionRegistry"/>。
/// <see cref="Execute"/> 返回人类可读的结果描述(写日志 + 托盘气泡);失败抛异常由宿主捕获。
/// </summary>
public interface IAction
{
    /// <summary>配置中引用的动作名,如 "display_mode"。</summary>
    string Name { get; }

    /// <summary>执行动作。args 为配置条目中的 args 字段(JSON 值)。</summary>
    string Execute(object? args);
}
