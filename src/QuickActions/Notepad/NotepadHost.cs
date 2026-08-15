using QuickActions.Core;

namespace QuickActions.Notepad;

/// <summary>
/// 记事本单例宿主：管理窗口生命周期与切换逻辑。
/// 热键切换：隐藏 → 显示；可见 → 复制内容到剪贴板并隐藏（内容保留，下次唤醒可继续编辑）。
/// </summary>
public sealed class NotepadHost : IDisposable
{
    private readonly string _dataDir;
    private NotepadWindow? _window;

    public NotepadHost(string dataDir) => _dataDir = dataDir;

    public ActionResult Toggle()
    {
        if (_window is null || _window.IsDisposed)
        {
            _window = new NotepadWindow(_dataDir);
            _window.ShowWindow();
            return new ActionResult(true, "已打开记事本");
        }

        if (_window.Visible)
        {
            _window.CopyToClipboard();
            _window.HideWindow();
            return new ActionResult(true, "已复制内容并隐藏记事本");
        }

        _window.ShowWindow();
        return new ActionResult(true, "已显示记事本");
    }

    /// <summary>托盘菜单入口：直接显示（不复制不隐藏）。</summary>
    public void Show()
    {
        if (_window is null || _window.IsDisposed)
            _window = new NotepadWindow(_dataDir);
        _window.ShowWindow();
    }

    public void Dispose()
    {
        if (_window is not null && !_window.IsDisposed)
            _window.Close();
        _window = null;
    }
}
