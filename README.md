# quick-actions

<p align="center"><img src="docs/quick-actions.png" alt="QuickActions 图标" width="96"></p>

> 本项目由 AI 协作完成：代码、文档与迭代均经 AI 生成和优化。

Windows 常驻后台的一键动作平台：配置驱动的全局热键 → 动作框架。无主界面，通过系统托盘管理。

## 定位

把所有"一键操作"需求收敛到一个常驻程序：热键集中注册、动作按接口扩展。

- 内置动作：投影切换、亮暗切换、日出日落自动亮暗
- 托盘菜单：动作切换（带快捷键提示）、自动亮暗、开机自启、打开配置、重启应用、版本号

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

- .NET Framework 4.8.1：Win11 内置，免安装 runtime，直接运行
- WinForms 无窗口宿主 + 自管系统托盘（自定义气泡图标）
- 自绘 Win11 风格托盘菜单：圆角实色卡片（双缓冲防闪烁）、主题跟随、Segoe Fluent Icons 图标
- P/Invoke：`RegisterHotKey`、`SetDisplayConfig`、`QueryDisplayConfig`、`SendMessageTimeout`（WM_SETTINGCHANGE）；主题读写用 `Microsoft.Win32.Registry`
- 内置 MiniJson，零外部依赖，单 exe 发布（约 100KB）
- 高 DPI（PerMonitorV2）、单实例守卫（重复启动自动退出）

## 配置

运行时数据位于 `%APPDATA%\QuickActions\`（exe 目录保持干净）：

- `config.json`——热键配置，首次启动自举默认值
- `log.txt`——运行日志

启动后弹一次"已在后台运行"通知，列出生效热键。热键支持：

- 单键：`F13`~`F24`、`F1`~`F12`、字母或数字键
- 组合键：`Ctrl+Shift+F14`、`Alt+F13`（修饰键：`Ctrl`、`Alt`、`Shift`、`Win`）

托盘右键菜单：

- **切换投影 / 切换亮暗**：与对应热键一致，右侧显示快捷键（如 `F13`）
- **打开配置**：用默认程序打开 `%APPDATA%\QuickActions\config.json`
- **自动亮暗**：勾选启用日出日落自动切换（重启后保持）
- **开机自启**：写 `HKCU\...\Run`，无需管理员权限
- **重启应用**：配置只在启动时读取，修改后点此生效
- **版本 v0.1.0**：点击跳转 GitHub 项目页
- **退出应用**

示例：

```json
[
  { "hotkey": "F13", "action": "display_mode", "args": { "mode": "toggle", "modes": ["internal", "extend"] } },
  { "hotkey": "F14", "action": "theme", "args": { "mode": "toggle" } }
]
```

`display_mode` 支持：

- `internal` / `extend` / `external` / `clone`：直接切换指定模式；已是该模式则不执行
- `toggle`：在 `modes`（默认 `["internal","extend"]`）中循环切换

`theme` 支持：

- `light` / `dark`：直接切换（广播 `WM_SETTINGCHANGE`，运行中的应用即时刷新）
- `toggle`：切换相反值（亮 ↔ 暗）

`auto_theme`（声明式条目，可选）：日出切亮色、日落切暗色，托盘菜单勾选启用。内置济南坐标（36.6512, 117.1201），不配置也能用：

```json
{ "action": "auto_theme", "args": { "latitude": "36.6512", "longitude": "117.1201", "offset_minutes": "0" } }
```

- `latitude` / `longitude`：按日期计算（NOAA 算法，中纬度误差约几分钟；时区固定北京时间 UTC+8）
- `sunrise` / `sunset`（`"HH:mm"`）：固定时间
- `offset_minutes`：切换点整体偏移，正数=延后
- 极昼/极夜地区当天不切换；错过切换点会自动对账修正

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

可用：投影切换、亮暗切换、日出日落自动亮暗（内置济南坐标，可覆盖）、快捷键提示、开机自启、单实例守卫、Win11 风格托盘菜单、单 exe 发布（net481）。

## 更新日志

见 [CHANGELOG.md](CHANGELOG.md)。

## 开源协议

[MIT](LICENSE) — 可自由使用、修改、商用，需保留版权声明。
