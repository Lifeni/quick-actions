using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;
using QuickActions.Interop;

namespace QuickActions.Notepad;

/// <summary>
/// 自写记事本：无系统标题栏，自绘"标题栏 + 工具栏"单行（图标按钮），等宽字体 + 行号、
/// 始终置顶、明暗主题适配、位置记忆、内容持久化、基础语法高亮、Ctrl+滚轮缩放、Ctrl+S 保存。
/// 隐藏/关闭时保存内容到 notepad.txt、位置到注册表；下次打开恢复。
/// </summary>
public sealed class NotepadWindow : Form
{
    private const string PositionKey = @"HKEY_CURRENT_USER\Software\QuickActions\Notepad";
    private const string ContentFileName = "notepad.txt";
    private const int BarHeight = 36;
    private const float BaseFontSize = 10f;

    private readonly string _dataDir;
    private readonly RichTextBox _editor;
    private readonly LineNumberGutter _gutter;
    private readonly System.Windows.Forms.Timer _highlightTimer;
    private readonly ToolTip _tooltip;
    private readonly bool _dark;
    private readonly Color _barBg;
    private readonly Color _textColor;
    private readonly Color _editorBg;
    private readonly Color _commentColor;
    private readonly Color _stringColor;
    private readonly Color _keywordColor;
    private readonly Color _numberColor;
    private readonly Color _fenceColor;
    private readonly Color _headerColor;
    private readonly Font _glyphFont;
    private readonly string _monoFamily;
    private float _zoom = 1f;

    public NotepadWindow(string dataDir)
    {
        _dataDir = dataDir;
        _dark = IsDarkMode();
        _barBg = _dark ? Color.FromArgb(45, 45, 48) : Color.FromArgb(240, 240, 240);
        _editorBg = _dark ? Color.FromArgb(30, 30, 30) : Color.White;
        _textColor = _dark ? Color.FromArgb(212, 212, 212) : Color.FromArgb(27, 27, 27);
        _commentColor = _dark ? Color.FromArgb(106, 153, 85) : Color.FromArgb(0, 128, 0);
        _stringColor = _dark ? Color.FromArgb(206, 145, 120) : Color.FromArgb(163, 21, 21);
        _keywordColor = _dark ? Color.FromArgb(86, 156, 214) : Color.FromArgb(0, 0, 255);
        _numberColor = _dark ? Color.FromArgb(181, 206, 168) : Color.FromArgb(9, 134, 88);
        _fenceColor = _dark ? Color.FromArgb(197, 134, 192) : Color.FromArgb(128, 0, 128);
        _headerColor = _dark ? Color.FromArgb(86, 156, 214) : Color.FromArgb(0, 0, 255);
        _monoFamily = MakeMonoFamily();
        _glyphFont = MakeGlyphFont();

        Text = "快捷记事";
        TopMost = true;
        FormBorderStyle = FormBorderStyle.None; // 自绘标题栏，与工具栏合一
        StartPosition = FormStartPosition.Manual;
        MinimumSize = new Size(400, 260);
        Size = new Size(840, 560); // 默认窗口，容纳单行栏与编辑区
        Font = new Font("Microsoft YaHei UI", 9f);
        BackColor = _editorBg;
        _tooltip = new ToolTip();

        // 自绘标题栏：左侧标题，右侧图标按钮，整条可拖动、双击最大化
        var bar = new Panel { Dock = DockStyle.Top, Height = BarHeight, BackColor = _barBg };
        var titleLabel = new Label
        {
            Text = "快捷记事",
            Dock = DockStyle.Left,
            AutoSize = true,
            Padding = new Padding(10, 0, 0, 0),
            ForeColor = _textColor,
            BackColor = _barBg,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        bar.Controls.Add(titleLabel);

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 4, 4, 4),
            BackColor = _barBg,
        };
        buttonBar.Controls.Add(MakeIconButton("\uE718", "取消置顶（切换）", (_, _) => TopMost = !TopMost));
        buttonBar.Controls.Add(MakeIconButton("\uE8C8", "复制到剪贴板", (_, _) => CopyToClipboard()));
        buttonBar.Controls.Add(MakeIconButton("\uE77F", "粘贴", (_, _) => PasteFromClipboard()));
        buttonBar.Controls.Add(MakeIconButton("\uE74E", "保存到文件（Ctrl+S）", (_, _) => SaveToFile()));
        buttonBar.Controls.Add(MakeIconButton("\uE922", "最大化", (_, _) =>
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized));
        buttonBar.Controls.Add(MakeIconButton("\uE923", "最小化", (_, _) => WindowState = FormWindowState.Minimized));
        buttonBar.Controls.Add(MakeIconButton("\uE711", "关闭", (_, _) => Close()));
        bar.Controls.Add(buttonBar);
        bar.MouseDown += OnBarMouseDown;
        bar.DoubleClick += (_, _) =>
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        Controls.Add(bar);

        // 编辑器 + 行号
        _editor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font(_monoFamily, BaseFontSize),
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            DetectUrls = false,
            BackColor = _editorBg,
            ForeColor = _textColor,
            BorderStyle = BorderStyle.None,
        };

        _gutter = new LineNumberGutter(_editor)
        {
            Dock = DockStyle.Left,
            Width = 46,
            BackColor = _dark ? Color.FromArgb(37, 37, 38) : Color.FromArgb(243, 243, 243),
            NumberColor = _dark ? Color.FromArgb(133, 133, 133) : Color.FromArgb(110, 110, 110),
        };

        _highlightTimer = new System.Windows.Forms.Timer { Interval = 300 };

        _editor.TextChanged += (_, _) =>
        {
            _gutter.Invalidate();
            _highlightTimer.Stop();
            _highlightTimer.Start();
        };
        _editor.VScroll += (_, _) => _gutter.Invalidate();
        _editor.Resize += (_, _) => _gutter.Invalidate();
        _editor.MouseWheel += OnEditorMouseWheel;

        Controls.Add(_gutter);
        Controls.Add(_editor);

        _highlightTimer.Tick += (_, _) =>
        {
            _highlightTimer.Stop();
            ApplyHighlight();
        };

        KeyPreview = true;
        KeyDown += OnFormKeyDown;
        FormClosing += (_, _) =>
        {
            SavePosition();
            SaveToFile();
        };

        RestorePosition();
        LoadContent();
    }

    /// <summary>暗色主题下让滚动条等系统元素跟随暗色（DWM 沉浸式暗色模式）。</summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_dark)
        {
            int value = 1;
            NativeMethods.DwmSetWindowAttribute(Handle, 20, ref value, sizeof(int));
        }
    }

    /// <summary>无边框窗口：边缘拖拽调整大小。</summary>
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTBOTTOMRIGHT = 17;
        const int HTBOTTOM = 15;
        const int HTRIGHT = 11;
        const int edge = 6;

        if (m.Msg == WM_NCHITTEST)
        {
            int x = m.LParam.ToInt32() & 0xFFFF;
            int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;
            Point p = PointToClient(new Point(x, y));
            bool right = p.X >= Width - edge;
            bool bottom = p.Y >= Height - edge;
            if (right && bottom)
            {
                m.Result = (IntPtr)HTBOTTOMRIGHT;
                return;
            }
            if (right)
            {
                m.Result = (IntPtr)HTRIGHT;
                return;
            }
            if (bottom)
            {
                m.Result = (IntPtr)HTBOTTOM;
                return;
            }
        }
        base.WndProc(ref m);
    }

    /// <summary>按住 Ctrl 滚动滚轮缩放文本（70%～250%）。</summary>
    private void OnEditorMouseWheel(object? sender, MouseEventArgs e)
    {
        if (!ModifierKeys.HasFlag(Keys.Control))
            return;
        float factor = e.Delta > 0 ? 1.1f : 1f / 1.1f;
        _zoom = Math.Min(Math.Max(_zoom * factor, 0.7f), 2.5f);
        _editor.Font = new Font(_monoFamily, BaseFontSize * _zoom);
        _gutter.Invalidate();
        ApplyHighlight();
        ((HandledMouseEventArgs)e).Handled = true;
    }

    private void OnBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        // 交给系统标题栏逻辑处理拖动
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, 0x00A1 /*WM_NCLBUTTONDOWN*/, (IntPtr)2 /*HTCAPTION*/, IntPtr.Zero);
    }

    private Button MakeIconButton(string glyph, string tooltip, EventHandler onClick)
    {
        var button = new Button
        {
            Text = glyph,
            Font = _glyphFont,
            Width = 34,
            Height = 26,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(1),
            BackColor = _barBg,
            ForeColor = _textColor,
            TabStop = false,
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderColor = _barBg;
        button.FlatAppearance.MouseOverBackColor = _dark ? Color.FromArgb(75, 75, 78) : Color.FromArgb(225, 225, 225);
        button.Click += onClick;
        _tooltip.SetToolTip(button, tooltip);
        return button;
    }

    /// <summary>显示并聚焦（内容保持上次状态，可继续编辑）。</summary>
    public void ShowWindow()
    {
        Show();
        Activate();
        _editor.Focus();
    }

    /// <summary>隐藏并保存内容与位置（不清除文本）。</summary>
    public void HideWindow()
    {
        SavePosition();
        SaveToFile();
        Hide();
    }

    /// <summary>复制全部内容到剪贴板（不清除文本）。</summary>
    public void CopyToClipboard()
    {
        if (_editor.TextLength == 0)
            return;
        try
        {
            Clipboard.SetText(_editor.Text);
        }
        catch
        {
            // 剪贴板被其他进程占用等：静默失败
        }
    }

    private void PasteFromClipboard()
    {
        try
        {
            if (Clipboard.ContainsText())
                _editor.Paste();
        }
        catch
        {
            // 剪贴板不可读时忽略
        }
    }

    private void SaveToFile()
    {
        try
        {
            Directory.CreateDirectory(_dataDir);
            File.WriteAllText(Path.Combine(_dataDir, ContentFileName), _editor.Text);
        }
        catch
        {
            // 保存失败不影响使用（下次仍会尝试）
        }
    }

    private void LoadContent()
    {
        try
        {
            string path = Path.Combine(_dataDir, ContentFileName);
            if (File.Exists(path))
                _editor.Text = File.ReadAllText(path);
        }
        catch
        {
            // 读取失败按空内容处理
        }
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.S)
        {
            SaveToFile();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void ApplyHighlight()
    {
        if (_editor.TextLength == 0)
            return;
        string language = SyntaxHighlighter.DetectLanguage(_editor.Text);
        var tokens = SyntaxHighlighter.Tokenize(_editor.Text, language);

        _editor.SuspendLayout();
        int selStart = _editor.SelectionStart;
        int selLength = _editor.SelectionLength;
        _editor.SelectAll();
        _editor.SelectionColor = _textColor;
        foreach (var (start, length, type) in tokens)
        {
            _editor.Select(start, length);
            _editor.SelectionColor = ColorFor(type);
        }
        _editor.Select(selStart, selLength);
        _editor.SelectionColor = _textColor;
        _editor.ResumeLayout();
    }

    private Color ColorFor(TokenType type) => type switch
    {
        TokenType.Comment => _commentColor,
        TokenType.String => _stringColor,
        TokenType.Keyword => _keywordColor,
        TokenType.Number => _numberColor,
        TokenType.Fence => _fenceColor,
        TokenType.Header => _headerColor,
        _ => _textColor,
    };

    private static string MakeMonoFamily()
    {
        var families = new HashSet<string>(FontFamily.Families.Select(f => f.Name));
        return families.Contains("Consolas") ? "Consolas"
            : families.Contains("Cascadia Mono") ? "Cascadia Mono"
            : families.Contains("Courier New") ? "Courier New"
            : FontFamily.GenericMonospace.Name;
    }

    private static Font MakeGlyphFont()
    {
        var families = new HashSet<string>(FontFamily.Families.Select(f => f.Name));
        string family = families.Contains("Segoe Fluent Icons") ? "Segoe Fluent Icons"
            : families.Contains("Segoe MDL2 Assets") ? "Segoe MDL2 Assets"
            : FontFamily.GenericSansSerif.Name;
        return new Font(family, 11f);
    }

    private static bool IsDarkMode()
    {
        try
        {
            return Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1) is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }

    private void RestorePosition()
    {
        try
        {
            int x = ReadInt("X"), y = ReadInt("Y"), w = ReadInt("Width"), h = ReadInt("Height");
            if (w >= MinimumSize.Width && h >= MinimumSize.Height)
            {
                var wa = Screen.FromPoint(new Point(x, y)).WorkingArea;
                x = Math.Min(Math.Max(x, wa.Left), Math.Max(wa.Left, wa.Right - w));
                y = Math.Min(Math.Max(y, wa.Top), Math.Max(wa.Top, wa.Bottom - h));
                Location = new Point(x, y);
                Size = new Size(w, h);
                return;
            }
        }
        catch
        {
            // 注册表异常按默认位置处理
        }
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - 40, area.Top + 40);
    }

    private void SavePosition()
    {
        try
        {
            if (WindowState == FormWindowState.Normal)
            {
                Registry.SetValue(PositionKey, "X", Location.X.ToString(), RegistryValueKind.String);
                Registry.SetValue(PositionKey, "Y", Location.Y.ToString(), RegistryValueKind.String);
                Registry.SetValue(PositionKey, "Width", Size.Width.ToString(), RegistryValueKind.String);
                Registry.SetValue(PositionKey, "Height", Size.Height.ToString(), RegistryValueKind.String);
            }
        }
        catch
        {
            // 写入失败不影响使用
        }
    }

    private int ReadInt(string name)
    {
        object? value = Registry.GetValue(PositionKey, name, null);
        return value is string s && int.TryParse(s, out int result) ? result : 0;
    }

    private sealed class LineNumberGutter : Panel
    {
        private readonly RichTextBox _target;

        public Color NumberColor { get; set; }

        public LineNumberGutter(RichTextBox target)
        {
            _target = target;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);
            if (_target.IsDisposed || _target.TextLength == 0)
                return;

            int firstLine = _target.GetLineFromCharIndex(_target.GetCharIndexFromPosition(new Point(2, 2)));
            int firstCharIdx = _target.GetFirstCharIndexFromLine(firstLine);
            Point firstPos = _target.GetPositionFromCharIndex(firstCharIdx);
            int lineHeight = _target.Font.Height;
            int nextLineIdx = _target.GetFirstCharIndexFromLine(firstLine + 1);
            if (nextLineIdx >= 0 && nextLineIdx < _target.TextLength)
                lineHeight = _target.GetPositionFromCharIndex(nextLineIdx).Y - firstPos.Y;

            int visible = _target.Height / lineHeight + 2;
            int lastLine = _target.GetLineFromCharIndex(Math.Max(0, _target.TextLength - 1));
            using var brush = new SolidBrush(NumberColor);
            using var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            for (int line = firstLine; line <= Math.Min(firstLine + visible, lastLine); line++)
            {
                int y = firstPos.Y + (line - firstLine) * lineHeight;
                g.DrawString((line + 1).ToString(), _target.Font, brush,
                    new RectangleF(0, y, Width - 6, lineHeight), format);
            }
        }
    }
}
