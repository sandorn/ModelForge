# ModelForge.Excel VSTO 工程

本目录已从”可迁移骨架”推进为 Visual Studio 可打开的 Excel VSTO 工程草案，目标框架为 `.NET Framework 4.8`。支持 Visual Studio 2022 (v17) 和 Visual Studio 2026 (v18)。

当前工程用于阶段一验证：在 Excel 中加载 ModelForge Ribbon，并通过本地后端桥接服务调用 `/health` 与 `/api/commands/dispatch`。

## 目录说明

| 目录 | 说明 |
| ---- | ---- |
| `Ribbon/` | ModelForge Ribbon XML、`IRibbonExtensibility` 入口与回调 |
| `Commands/` | Excel 命令 ID、快捷键映射与执行入口 |
| `Configuration/` | 本地后端桥接配置 |
| `Infrastructure/` | COM 释放、Office 版本检测、异常处理等基础设施 |
| `Services/` | 后端 API 调用客户端 |
| `Properties/` | 程序集元数据 |

核心入口：

- `ModelForge.Excel.sln`：Visual Studio 解决方案。
- `ModelForge.Excel.csproj`：旧式 `.NET Framework 4.8` VSTO 项目文件。
- `ThisAddIn.cs`：插件启动、关闭、Ribbon 创建和桥接服务初始化。
- `ThisAddIn.Designer.cs`：VSTO 启动对象最小设计器代码。
- `Ribbon/ModelForgeRibbon.cs`：Office Ribbon XML 加载入口。

## 本机打开与调试

首次构建前需要生成本机开发签名配置。该配置只写入当前用户证书库和未跟踪的 `ModelForge.Excel.LocalSigning.props`，不会提交到仓库：

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\setup-vsto-dev-signing.ps1
```

如需在命令行验证 VSTO 工程，可从仓库根目录执行：

```cmd
powershell -NoProfile -ExecutionPolicy Bypass -Command "$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'; & $msbuild 'src\vsto\ModelForge.Excel\ModelForge.Excel.csproj' '/t:Rebuild' '/p:Configuration=Debug' '/p:Platform=AnyCPU' '/v:minimal'"
```

> 若使用 Visual Studio 2022 Build Tools，请将 `$msbuild` 指向 `D:\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe`。

当前已在本机验证 Debug Rebuild 通过 (VS 2026 MSBuild v18.6.3)，并生成以下关键产物：

- `bin\Debug\ModelForge.Excel.dll`
- `bin\Debug\ModelForge.Excel.dll.manifest`
- `bin\Debug\ModelForge.Excel.vsto`
- `bin\Debug\Microsoft.Office.Tools.Common.v4.0.Utilities.dll`

1. 启动后端桥接服务：

   ```cmd
   scripts\dev-backend.cmd
   ```

2. 用 Visual Studio 2022 或 2026 打开：

   ```text
   src\vsto\ModelForge.Excel\ModelForge.Excel.sln
   ```

3. 确认已安装 Visual Studio 的 `Office/SharePoint Development` 工作负载，以及本机 Microsoft Office 桌面版 Excel。

   > **VS 2026 用户**：若 IDE 提示项目类型不受支持，请先运行一次 `devenv /setup` 合并 VSTO PKGDEF 配置，然后重新打开解决方案。

4. 在 Visual Studio 中生成项目。如果提示 Office PIA、VSTO Runtime 或 Office Tools 缺失，请通过 Visual Studio Installer 修复工作负载。

5. 按 `F5` 启动调试。Excel 打开后应出现 `ModelForge` Ribbon 标签。

6. 点击 `检查后端`，确认本地后端 `/health` 可访问。

## 当前工程边界

- 当前命令行环境检测到 Visual Studio 2026 Community (v18.6.2) 和 Visual Studio 2022 Build Tools，具备 MSBuild、`.NET Framework 4.8 Targeting Pack` 与 OfficeTools targets。
- 当前命令行 Debug Rebuild 已通过，可验证 DLL、ClickOnce application manifest 与 `.vsto` 入口产物生成。
- 当前命令行环境未发现 Excel 桌面版路径和 `Microsoft.Office.Interop.Excel` PIA，因此真实 Excel 加载、F5 调试、COM 自动化行为仍需要在完整 Visual Studio IDE + Office 桌面版环境中验证。
- ClickOnce manifest 签名是 VSTO 构建必需项。仓库不提交个人证书或私钥；本机通过 `scripts\setup-vsto-dev-signing.ps1` 生成 `ModelForge.Excel.LocalSigning.props` 后再构建。
- `ThisAddIn.Designer.cs` 是最小可迁移设计器代码；如 Visual Studio 生成完整模板文件，应优先采用 VS 生成版本，并保留 `ThisAddIn.cs`、`Ribbon/`、`Commands/`、`Configuration/`、`Infrastructure/`、`Services/` 中的业务代码。

## 开发约束

- 插件侧保持 `.NET Framework 4.8` 兼容写法。
- VSTO 与 Web Add-in 不直接通信，统一经过本地后端桥接。
- 复杂 Office COM 调用必须使用 `ComObjectScope` 或等价模式显式释放中间对象。
- Ribbon 回调必须捕获异常，避免异常穿透到 Office 宿主导致插件被禁用。
