# quick-actions

<p align="center"><img src="docs/quick-actions.png" alt="QuickActions 图标" width="96"></p>

> 本项目由 AI 协作完成：代码、文档与迭代均经 AI 生成和优化。

Windows 常驻后台的一键动作平台：配置驱动的全局热键 → 动作框架。无主界面，通过系统托盘管理。

## 定位

把所有"一键操作"需求收敛到一个常驻程序里，热键集中注册、动作按接口扩展：

- 当前内置动作：切换投影模式（仅当前屏幕或扩展模式）、切换 Windows 亮色/暗色模式、按日出日落自动切换亮暗
- 托盘菜单：投影/亮暗切换（带快捷键提示）、自动亮暗开关、开机自启开关、打开配置、重启应用
- 后续动作（规划）：切换音频输出设备、静音麦克风、一键打开常用软件等

## 快速开始

```bash
dotnet build src/QuickActions/QuickActions.csproj

# 运行(开发)
dotnet run --project src/QuickActions

# 发布 Release(输出到 dist/)
dotnet publish src/QuickActions/QuickActions.csproj -c Release -o dist

# 冒烟检查(不常驻,验证配置加载与热键注册)
dotnet run --project src/QuickActions -- --smoke
```

## 技术栈

- .NET Framework 4.8.1（Windows 11 内置，免安装 runtime，任何 Win11 直接运行）
- WinForms 无窗口消息泵宿主 + 自管系统托盘（自定义气泡图标）
- 自绘 Win11 风格托盘菜单：DWM 圆角实色卡片（双缓冲防闪烁），亮/暗跟随系统主题，Segoe Fluent Icons 图标
- P/Invoke：`RegisterHotKey`（全局热键）、`SetDisplayConfig`（投影拓扑）、`QueryDisplayConfig`（当前拓扑）、`SendMessageTimeout`（WM_SETTINGCHANGE 主题刷新广播）；主题状态读写用内置 `Microsoft.Win32.Registry`
- 内置 MiniJson 解析器，零外部依赖，发布产物为单个 exe（约 76KB）
- 高 DPI：app.manifest 声明 PerMonitorV2
- 单实例守卫：命名 Mutex，重复启动自动退出；"重启应用"先释放互斥体再拉起新实例

## 配置

运行时数据位于 `%APPDATA%\QuickActions\`（exe 目录保持干净）：

- `config.json`——热键配置，首次启动自举默认值
- `log.txt`——运行日志

启动后弹一次"已在后台运行"通知，列出生效热键。热键支持：

- 单键：`F13`~`F24`、`F1`~`F12`、字母或数字键
- 组合键：`Ctrl+Shift+F14`、`Alt+F13`（修饰键：`Ctrl`、`Alt`、`Shift`、`Win`）

托盘右键菜单：

- **切换投影 / 切换亮暗**：与对应热键行为一致（复用配置参数），右侧显示快捷键提示（如 `F13`）
- **打开配置**：用系统默认程序打开 `%APPDATA%\QuickActions\config.json`
- **自动亮暗**：勾选启用/停用自动亮暗切换，状态持久化（重启后保持）
- **开机自启**：写 `HKCU\...\Run`（用户级，无需管理员权限），勾选状态每次弹出菜单时刷新
- **重启应用**：配置文件只在启动时读取一次，修改 `config.json` 后点此重启生效
- **退出应用**：结束常驻进程

示例：

```json
[
  { "hotkey": "F13", "action": "display_mode", "args": { "mode": "toggle", "modes": ["internal", "extend"] } },
  { "hotkey": "F14", "action": "theme", "args": { "mode": "toggle" } }
]
```

`display_mode` 支持：

- `internal`（仅当前屏幕）、`extend`（扩展）、`external`（仅外接）、`clone`（复制）：直接切换到指定模式；若已是该模式则不执行、不弹通知。
- `toggle`：自动判断当前拓扑，在 `modes`（默认 `["internal","extend"]`）中循环切换——一个键完成"仅当前屏幕 ↔ 扩展"。

`theme` 支持：

- `light` / `dark`：直接切换到指定模式（应用与系统外观同步切换，广播 `WM_SETTINGCHANGE` 后运行中的应用即时刷新）；若已是该模式则不执行、不弹通知。
- `toggle`：读取当前模式并切换到相反值（亮 ↔ 暗）。当前模式无法读取时报错并由宿主气泡提示。

`auto_theme`（声明式条目，可选，无热键）：日出 → 亮色、日落 → 暗色，自动切换。启用/停用走托盘菜单勾选（持久化，重启后保持）。**内置默认济南坐标（36.6512, 117.1201），不配置也能用**；需要覆盖时在 config.json 加条目：

```json
{ "action": "auto_theme", "args": { "latitude": "36.6512", "longitude": "117.1201", "offset_minutes": "0" } }
```

- 两种覆盖方式：
  - `latitude` / `longitude`：日出日落按日期计算（NOAA 算法，中纬度误差约几分钟；时区取系统当前时区，北京时间 = UTC+8 无需额外配置）
  - `sunrise` / `sunset`（`"HH:mm"`）：固定时间，不用关心坐标
- `offset_minutes`（可选）：对切换点整体偏移，正数=延后（如日落亮转暗延后 30 分钟）
- 极昼/极夜地区：当天无日出/日落时不切换；错过切换点（睡眠唤醒等）会自动对账修正

## 目录结构

```
quick-actions/
├── AGENTS.md        # AI Agent 开发指南(架构/约定/测试规范)
├── LICENSE          # MIT 开源协议
├── config/          # 配置模板(运行时数据在 %APPDATA%\QuickActions)
├── docs/            # 设计文档与素材
├── scripts/         # 开发/运维脚本
├── src/QuickActions/    # 主程序
└── tests/QuickActions.Tests/  # 单元测试
```

## 状态

可用：投影切换（toggle 与指定模式）、Windows 亮暗切换（toggle 与指定模式）、日出日落自动亮暗（内置济南坐标，可覆盖）、快捷键提示、开机自启开关、单实例守卫、Win11 风格托盘菜单、自定义托盘与气泡、单 exe 发布（net481）。

## 开源协议

[MIT](LICENSE) — 可自由使用、修改、商用，需保留版权声明。
