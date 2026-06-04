# ModelForge Changelog

## 0.1.0 (2026-06-03) — Phase A+B+C+D 初始交付

### 新增

#### Sidecar (.NET 10 Worker Service)
- Win32 全局键盘钩子 (WH_KEYBOARD_LL)，20 个快捷键注册
- .NET 原生 COM Interop (oleaut32!GetActiveObject + dynamic)
- 20 个命令全部实现：7 Power Tools + 4 Visualizations + Model Check + Formula Tracing + Cross-App Linking + Optimization
- 5 REST 端点: `/health`, `/api/shortcuts`, `/api/execute`, `/api/excel/info`

#### Backend (.NET 10 ASP.NET Core)
- `/health`, `/api/version`, `/api/commands`, `/api/commands/dispatch`
- `/api/config/{scope}`, `/api/audit-events`, `/api/links`
- 契约升级: `CommandExecutionTarget.Sidecar`, 全线 DTO 同步

#### Web Add-in (React 18 + Fluent UI 9 + Vite 6)
- TaskPane 主布局: 6 面板导航 (总览/命令/Excel/审计/AIWA/管理)
- Dashboard 状态卡片 (Backend + Sidecar + Command 统计)
- Command Panel: 按功能分组执行
- Omnibar: fuse.js 模糊搜索 + 键盘导航
- AIWA Chat: 5 模式 (总结/展开/改写/校对/翻译) Mock 交互
- Admin Console: 用户管理 + 审计日志 + 配置面板
- Sidecar HTTP 客户端 + function-file.html

#### 测试
- 51 单元测试 (ChordParser 21 + ShortcutRegistry 8 + PowerTools 22)

#### 安装器
- WiX v5 MSI 项目 (Sidecar + Backend 双 Windows Service)
- `build-installer.ps1` 一键发布脚本

### 变更

- **BREAKING**: 原 VSTO (.NET Framework 4.8) 架构已移除
- 后端: `net8.0` → `net10.0`
- 契约: `netstandard2.0` → `net10.0`, `CommandExecutionTarget.Vsto` → `Sidecar`
- Manifest: Home Tab 按钮 → CustomTab 5 组 20+ ExecuteFunction 按钮
- CLI: `dotnet-version 9.0.x` → `10.0.x`

### 已修复

- `Marshal.GetActiveObject` 不存在 → oleaut32!GetActiveObject P/Invoke
- NetOfficeFw 仅支持 .NET Framework → 切换为 .NET 原生 COM Interop
- DCF 模板 `InlineData` 编译时表达式 → 使用 `const uint` 运算
- `static readonly` 作为 `ref` 参数 → 局部变量复制
