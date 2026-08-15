# AGENTS.md — QuickActions 开发指南

Windows 常驻托盘的一键动作平台：配置驱动的全局热键 → 动作框架。无主界面，单 exe 常驻后台。

## 硬性约束（不可破坏）

- **.NET Framework 4.8.1**（`net481`），Win11 免安装 runtime 直接跑——这是项目的核心卖点
- **零外部依赖**：无 NuGet 运行时包、无 Windows App SDK/WinUI。JSON 用内置 MiniJson（仅对象/数组/字符串）
- **单 exe 发布**：`dotnet publish -c Release -o dist`，产物约 76KB。exe 目录保持干净，运行时数据全在 `%APPDATA%\QuickActions\`
- 语言 C# `latest` + nullable + implicit usings；测试用 xunit
- 注释与用户可见文案使用中文

## 常用命令

```bash
dotnet build src/QuickActions/QuickActions.csproj        # 主程序
dotnet test tests/QuickActions.Tests/QuickActions.Tests.csproj   # 测试（必须全绿）
dotnet run --project src/QuickActions -- --smoke          # 冒烟：注册检查，不常驻
dotnet publish src/QuickActions/QuickActions.csproj -c Release -o dist
```

注意：常驻实例运行时会锁 exe，重新构建前先停掉它（任务管理器结束 QuickActions.exe）。

## 架构

```
Program.cs        入口：单实例守卫 → 配置加载 → 动作注册 → App 启动 → 自动亮暗恢复
App.cs            常驻宿主：托盘图标 + 热键注册 + 菜单构建 + 动作执行（气泡/日志）
TrayIcon.cs       自管系统托盘（NOTIFYICONDATA + 隐藏消息窗口）
ModernMenu.cs     自绘 Win11 风格托盘菜单（圆角实色卡片、图标、勾选、快捷键提示）
Core/             热键解析/注册、动作注册表、单实例互斥体
Config/           ConfigStore（%APPDATA% 配置读写）+ MiniJson
Actions/          内置动作 + 日出日落调度
Interop/          P/Invoke 集中地（RegisterHotKey、SetDisplayConfig、DWM、鼠标钩子等）
```

### 启动流程（Program.cs）

1. `--smoke` 参数：注册检查后立即退出（不抢单实例互斥体）
2. 单实例守卫：`Local\QuickActions.SingleInstance` 命名 Mutex，重复启动直接退出
3. 加载 `%APPDATA%\QuickActions\config.json`（首次自举默认值；解析失败弹框并退出）
4. 注册动作到 `ActionRegistry`，构建 `App`（含托盘菜单），`RegisterAll` 注册热键
5. 若自动亮暗启用标志为真，恢复 `AutoThemeScheduler`

### 新增一个动作（标准流程）

1. 实现 `IAction`（`Name` + `Execute(object? args)` 返回 `ActionResult(Changed, Message)`）；失败抛异常由宿主捕获
2. 纯逻辑抽成 `internal static` 供测试（参考 `DisplayModeAction.Decide`/`ThemeAction.PickToggleTarget`）
3. `Program.cs` 注册进 `ActionRegistry`
4. 配置条目：`{ "hotkey": "...", "action": "动作名", "args": {...} }`
5. 若需要托盘菜单项：`App` 构造菜单时添加 `ModernMenuItem`，并捕获首个配置条目的参数/热键回填
6. 测试：纯函数 + 参数解析；**禁止写测试改变真实状态**（注册表/主题/显示器）

### 配置 schema

- 条目：`hotkey`（可省略=声明式条目，如 `auto_theme`）、`action`、`args`
- `display_mode`：`mode` 为 internal/extend/external/clone/toggle（`modes` 数组）
- `theme`：`mode` 为 light/dark/toggle
- `auto_theme`（声明式）：`latitude`/`longitude`/`offset_minutes` 或固定 `sunrise`/`sunset`；缺省回退内置济南坐标

### 已知设计决策

- 菜单自绘而非 ContextMenuStrip：Win11 现代样式（圆角+图标+提示）只有自绘或 WinUI 3 能做到；后者破坏零依赖约束
- 自绘弹窗：实色主题卡片 + 双缓冲（半透明 Acrylic 已被移除——其整窗重绘是闪烁根源）
- 菜单文字垂直居中带 `TextShift` 光学补偿（CJK 墨迹不占满行盒底部的视觉修正）
- 主题读写：`HKCU\...\Themes\Personalize` 的 AppsUseLightTheme/SystemUsesLightTheme + WM_SETTINGCHANGE 广播
- 开机自启：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`（用户级免管理员）
- 日出日落：NOAA 算法（`SunTimes.cs`），参考值见测试注释（北京/特罗姆瑟）
- 配置只在启动时读取一次：修改 `config.json` 后需重启生效（托盘菜单"重启应用"，重启前先释放单实例互斥体）
- 日志：`Logger` append-only；被占用时降级为 Null 不阻断启动

## 测试约定

- 纯函数优先；P/Invoke 布局用 `Marshal.SizeOf` 断言（曾因 CCD 结构体越界崩过 testhost）
- 覆盖：动作逻辑、配置加载、热键解析、MiniJson、日出日落参考值、调度纯函数
- 完整套件必须通过后才算完成
