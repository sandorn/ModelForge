# ModelForge VSTO 项目说明

本目录用于承载 Excel、PowerPoint、Word 的 VSTO 插件项目。阶段一优先建设 Excel 插件壳与后端桥接链路。

## 当前状态

当前仓库通过 CLI 创建了可追踪的 VSTO 目录、Ribbon XML、命令目录、快捷键注册草案、后端 API 客户端和 COM 释放规范代码。真实 VSTO 项目仍需在安装了 Office 开发工具的 Visual Studio 中创建，因为 VSTO 模板不属于 `dotnet new` 标准模板。

## Visual Studio 创建步骤

1. 确认已安装：
   - Visual Studio 2022
   - Office/SharePoint Development workload
   - .NET Framework 4.8 Developer Pack
   - Microsoft Office 2016/2019/365 桌面版
2. 在 `src/vsto/` 下创建 `ModelForge.Excel` Excel VSTO Add-in 项目。
3. 将当前 `ModelForge.Excel` 目录中的 `Ribbon/`、`Commands/`、`Configuration/`、`Infrastructure/`、`Services/` 合并进真实项目。
4. 将 `Ribbon/ModelForgeRibbon.xml` 注册为 Ribbon XML，并把回调方法连接到真实 `ThisAddIn` 生命周期。
5. 在 `ThisAddIn_Startup` 中初始化：
   - `BackendBridgeClient`
   - `ShortcutRegistry`
   - `OfficeVersionInfo`
   - 日志与全局异常处理

## 阶段一边界

- 必须显示 `ModelForge` Ribbon。
- 至少提供一个 VSTO 按钮触发 `/api/commands/dispatch`。
- 禁止 VSTO 与 Web Add-in 直接通信，必须通过后端桥接。
- COM 对象必须遵循 `ComObjectScope` 释放规范。
