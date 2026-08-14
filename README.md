# quick-actions

Windows 常驻后台的一键动作平台:配置驱动的全局热键 → 动作框架。无主界面,通过系统托盘管理。

## 定位

把所有"一键操作"需求收敛到一个常驻程序里,热键集中注册、动作按接口扩展:

- 当前内置动作:切换投影模式(仅当前屏幕 / 扩展模式)
- 后续动作(规划):切换音频输出设备、静音麦克风、一键打开常用软件等

## 配置

```bash
# 构建
dotnet build src/QuickActions/QuickActions.csproj

# 运行(开发)
dotnet run --project src/QuickActions

# 发布 Release(输出到 dist/)
dotnet publish src/QuickActions/QuickActions.csproj -c Release -o dist
```

## 技术栈

- .NET Framework 4.8.1(Windows 11 内置,免安装 runtime,任何 Win11 直接运行)
- WinForms 无窗口消息泵宿主 + 自管系统托盘(自定义气泡图标)
- P/Invoke:`RegisterHotKey`(全局热键)、`SetDisplayConfig`(投影拓扑)、`QueryDisplayConfig`(当前拓扑)
- 内置 MiniJson 解析器,零外部依赖,发布产物为单个 exe(约 76KB)
- 高 DPI:app.manifest 声明 PerMonitorV2

## 配置

运行时数据位于 `%APPDATA%\QuickActions\`(exe 目录保持干净):

- `config.json` — 热键配置,首次启动自举默认值
- `log.txt` — 运行日志

启动后弹一次"已在后台运行"通知,列出生效热键。热键支持:

- 单键:`F13`~`F24`、`F1`~`F12`、字母/数字键
- 组合键:`Ctrl+Shift+F14`、`Alt+F13`(修饰键: `Ctrl` / `Alt` / `Shift` / `Win`)

示例:

```json
[
  { "hotkey": "F13", "action": "display_mode", "args": { "mode": "toggle", "modes": ["internal", "extend"] } }
]
```

`display_mode` 支持:

- `internal`(仅当前屏幕)、`extend`(扩展)、`external`(仅外接)、`clone`(复制):直接切换到指定模式;若已是该模式则不执行、不弹通知。
- `toggle`:自动判断当前拓扑,在 `modes`(默认 `["internal","extend"]`)中循环切换——一个键完成"仅当前屏幕 ↔ 扩展"。

## 目录结构

```
quick-actions/
├── config/          # 配置模板
├── docs/            # 设计文档
├── scripts/         # 开发/运维脚本
├── src/QuickActions/    # 主程序
├── tests/QuickActions.Tests/  # 单元测试
└── data/            # 运行时数据(不入库)
```

## 状态

可用:投影切换(toggle + 指定模式)、自定义托盘与气泡、单 exe 发布(net481)。
