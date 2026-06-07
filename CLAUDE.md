# ModelForge - AI Agent 规则手册

## 项目概览

| 属性         | 说明                                           |
| ------------ | ---------------------------------------------- |
| **项目名称** | ModelForge                                     |
| **项目类型** | Office 生产力插件套件                          |
| **技术架构** | Sidecar (.NET 10 + 原生 COM) + Office JS API + 后端桥接 (ASP.NET Core 10) |
| **参考产品** | Macabacus                                      |
| **目标用户** | 投行、私募、风投、FP&A、管理咨询等财务专业人士 |

## 核心架构（2026 重构版）

```
┌─────────────────────────────────────────────────────────────────┐
│                    Ribbon 功能区 (融合)                           │
├─────────────────────────────┬───────────────────────────────────┤
│  Sidecar (.NET 10 + 原生 COM)│        Web Add-in Commands        │
│    (COM 深度操作 + 快捷键)     │     (灵活更新无需安装包)           │
├─────────────────────────────┴───────────────────────────────────┤
│                                                                 │
│  Sidecar 负责：                     Web Add-in 负责：             │
│  ├── 全局键盘钩子 (Win32)           ├── Admin Console            │
│  ├── 原生 COM 互操作                 ├── Shared Libraries        │
│  ├── 公式追踪 + Model Check         ├── Corporate Dictionary    │
│  ├── 文件优化 + Prepare to Share    ├── Dashboard 统计          │
│  ├── Power Tools                    ├── Omnibar 速查栏           │
│  ├── Visualizations                 ├── AIWA 前端 UI             │
│  ├── 跨应用数据联动                  └── 企业策略下发            │
│  └── PPT/Word 底层交互                                          │
│                                                                 │
│  Backend 负责：认证、配置、审计、链接元数据、字典、命令桥接、遥测    │
└─────────────────────────────────────────────────────────────────┘
```

## 硬边界规则

- **Sidecar 职责**：性能敏感功能必须用 Sidecar（全局键盘钩子、原生 COM 互操作、跨应用联动、公式解析）
- **Web Add-in 职责**：灵活更新功能用 Web（管理后台、AIWA、配置管理）
- **通信规范**：Sidecar ↔ Web Add-in 通过 localhost REST (:5200)；Sidecar ↔ Backend 通过 HTTP (:5095)；禁止直接进程通信
- **文档优先**：新增 API/路由/环境变量必须同步更新 docs/ 相关文档
- **运行时边界**：Sidecar 使用 .NET 10 Web Host + 原生 COM（`GetActiveObject` P/Invoke，零 PIA）；后端服务使用 ASP.NET Core 10（目标框架 `net10.0`）
- **MVP 顺序**：先完成 Sidecar + Web Add-in 基础设施，再扩展 PPT/Word 支持与 AIWA

## 项目结构

```
ModelForge/
├── src/
│   ├── sidecar/               # Sidecar REST 服务 (.NET 10 + 原生 COM)
│   │   └── ModelForge.Sidecar/
│   │       ├── Program.cs      # Host + DI + REST (:5200)
│   │       ├── Commands/       # 命令 ID + 快捷键注册
│   │       ├── Configuration/  # BridgeOptions
│   │       ├── Interop/        # 原生 COM 互操作
│   │       ├── Keyboard/       # Win32 全局键盘钩子
│   │       ├── PowerTools/     # FillRight, FillDown, IFERROR...
│   │       ├── Visualizations/ # 三色审计标色
│   │       ├── ModelCheck/     # 错误/外链/循环引用扫描
│   │       ├── Formula/        # 公式追踪 (Precedents/Dependents)
│   │       ├── Linking/        # 跨应用链接 (Excel→PPT/Word)
│   │       ├── Optimization/   # WorkbookOptimizer, PrepareToShare
│   │       ├── Services/       # BackendBridgeClient (HTTP)
│   │       └── Api/            # Sidecar REST 端点
│   ├── web/                    # Web Add-in (React + TypeScript)
│   ├── backend/                # 后端 API 桥接 (ASP.NET Core 10)
│   ├── shared/                 # 共享类型/常量 (.NET 10)
├── manifest/                   # Add-in Manifest 配置
├── docs/                       # 项目文档
│   ├── 技术路线规划.md          # 🆕 新架构规划
│   ├── 功能规划.md              # 功能清单与优先级
│   ├── 实现路径规划.md           # 实施阶段与里程碑
│   └── 版权信息.md               # 版权声明
├── LICENSE                     # 专有软件许可证
└── README.md                   # 项目说明
```

## 关键路径

| 模块            | 路径                              | 说明               |
| --------------- | --------------------------------- | ------------------ |
| Sidecar         | `src/sidecar/ModelForge.Sidecar/` | .NET 10 REST + 原生 COM           |
| Web Add-in      | `src/web/`                        | 管理后台与 AI 功能 |
| 后端 API        | `src/backend/`                    | 跨插件通信桥接 + 企业服务 |

## 命令速查

### 开发环境

```powershell
# 搭建环境
dotnet --version              # 检查 .NET 10 SDK
node --version               # 检查 Node.js 20 LTS
# Sidecar 开发需要 Visual Studio 2022/2026（无需 Office 工作负载）+ Microsoft Office 桌面版

# Sidecar 开发
dotnet build src/sidecar/    # 构建 Sidecar
dotnet run --project src/sidecar/ModelForge.Sidecar  # 启动 Sidecar (:5200)

# Web Add-in 开发
cd src/web
npm install                  # 安装依赖
npm run dev                  # 启动开发服务器 (:5173)
npm run build                # 构建生产版本

# 后端 API
dotnet run --project src/backend/ModelForge.Backend  # 启动后端 (:5095)
```

### 部署命令

```powershell
# 构建 Sidecar (self-contained)
dotnet publish src/sidecar/ModelForge.Sidecar -c Release -r win-x64 --self-contained

# 构建后端
dotnet publish src/backend/ModelForge.Backend -c Release

# 部署 Web Add-in
cd src/web && npm run build
```

## 环境变量

| 变量名                     | 说明             | 示例值                                                          |
| -------------------------- | ---------------- | --------------------------------------------------------------- |
| `MODELFORGE_DB_CONNECTION` | 数据库连接字符串 | `Host=localhost;Database=ModelForge;Username=postgres`          |
| `VITE_MODELFORGE_API_URL`  | Web Add-in 后端 API 地址 | `http://localhost:5095`                                  |
| `VITE_MODELFORGE_SIDECAR_URL` | Web Add-in Sidecar 地址 | `http://localhost:5200`                                |
| `VITE_MODELFORGE_SIDECAR_TOKEN` | 任务窗格调用 Sidecar `/api/*` 的本地令牌 | Vite 构建期注入；Ribbon function-file 建议用 localStorage |
| `Sidecar__LocalApiToken`   | Sidecar 本地 `/api/*` 令牌 | 空值表示开发兼容模式 |
| `ModelForge__ServiceToken` | Backend 服务级导出令牌 | Sidecar 读取企业词典时与 `Sidecar__ServiceToken` 保持一致 |
| `MODELFORGE_AI_API_KEY`    | AI 服务 API Key  | `sk-xxx...`                                                     |
| `MODELFORGE_SSO_ENABLED`   | 是否启用 SSO     | `true/false`                                                    |
| `Sidecar__KeyboardHookEnabled` | 启用键盘钩子 | `true/false`                                               |

## 技术栈

| 层级          | 技术选型                | 版本 |
| ------------- | ----------------------- | ---- |
| Sidecar 运行时 | .NET Web Host (Minimal API) | 10.0 |
| COM 互操作    | 原生 COM (P/Invoke)    | —    |
| Office JS API | Office.js               | 1.1+ |
| 后端框架      | ASP.NET Core            | 10.0+ |
| 前端框架      | React                   | 18+  |
| 前端语言      | TypeScript              | 5.0+ |
| 构建工具      | Vite                    | 6.0+ |
| UI 组件库     | Fluent UI React         | 9.0+ |
| 状态管理      | Zustand                 | 4.0+ |
| 数据库        | InMemory / SQLite / PostgreSQL | -    |


## 测试统计

| 项目 | 测试类 | 测试数 | 状态 |
|------|--------|--------|------|
| Backend 单元测试 | 12 | 60 | ✅ 全部通过（含 SQLite 持久化测试） |
| Sidecar 非 COM 回归 | 15 | 170 | ✅ 全部通过（`FullyQualifiedName!~Com` 过滤） |
| COM 集成测试 | 4 | 22 | ✅ Excel/PPT/Word 原生 COM 互操作 |
| E2E 测试 | 2 | 48 | ✅ Backend + Sidecar HTTP 全链路 |
| 用户式 Office E2E | 1 | 1 | ✅ Office 2024 命令级链路通过 |
| Web Add-in (TS) | 13 | 95 | ✅ 全部通过 |
| 总计 | 47 | 396 | ✅（不含 COM 22 时为 374） |

> E2E 测试已实现：自动启动 Backend(:5095) + Sidecar(:5200)，验证 HTTP 全链路（健康检查/命令目录/配置/认证审计/Admin 用户生命周期与管理写操作审计/角色权限/审计统计摘要/审计保留策略/遥测关闭行为/诊断摘要/诊断包/链接元数据刷新请求/执行/输入校验/Sidecar `/api/execute` 审计自动上报/Sidecar 本地令牌鉴权）。
> COM 集成测试已通过 Office 2024 验证：Excel (连接+读写+AutoFill)、PowerPoint (连接+幻灯片)、Word (连接+文本)。
> 用户式 Office E2E 已通过 Office 2024 验证：`scripts/manual-office-e2e.ps1` 按真实用户方式启动 Excel/PowerPoint/Word，经 Sidecar API 执行 Excel IFERROR、PPT 对齐、Excel→PPT OLE 链接和 Word 尽调模板生成；产物位于 `artifacts/manual-e2e/office-e2e-20260606-165021.*`。
> 测试覆盖：JWT、用户存储、配置存储、审计事件、认证审计、遥测关闭策略、审计保留清理、Admin 诊断摘要、Admin 角色权限只读矩阵、Admin 审计统计摘要/趋势时间桶、Admin 管理写操作审计、Sidecar 执行审计自动上报、服务令牌字典读取、命令目录、字典服务、链接元数据、链接刷新规划器、Sidecar 本地令牌鉴权、快捷键注册/原子替换/39 个默认映射唯一性、和弦解析、单元格分类、公式操作、统计计算、ModelCheck 逻辑、NamesManager、DocBuilder(3)、PrepareToShare 增强(隐藏行列+断裂链接)、LinkRefresher、DeckCheck 增强(编号+密度+Logo 位置+品牌化 HTML/PDF 报告基础版)、ShapeTools 消息格式与几何算法、Linking 消息格式。Web: bridgeStore 多宿主 + LinkManager + Omnibar 宿主标签 + Office manifest/function-file 静态校验（含 PPT 全量 Ribbon 命令、Sidecar 令牌头、Ribbon 可见反馈） + 登录 ApiEnvelope 解包 + App 命令分组完整 ID 校验 + 快捷键配置 JSON 解析/SidecarClient 解包 + Sidecar 令牌本地保存 + Admin/Aiwa 文案与信封解析 + Admin 角色权限/审计摘要/趋势图/热力图下钻/服务端筛选/保留策略解包 + Web UI 显式点击审计 payload + Corporate Dictionary 高亮分段/JSON/CSV/XLSX 模板导入导出 + ApiClient 登录/Admin/诊断方法解包 + Admin 诊断面板。COM 集成: Excel/PPT/Word 原生互操作验证。Backend/Admin 业务路径与 Sidecar `/api/execute`/`/api/status` 已统一 `ApiEnvelope<T>` 结构化响应（Sidecar `/health`、`/api/shortcuts` 仍保留轻量原始 JSON）。Web Add-in 前端测试覆盖 authStore、contracts、bridgeStore、ApiClient、LoginPage、App 命令分组、SidecarClient、AdminConsole、AiwaChat、Office manifest/function-file、uiAudit。Backend 支持三模式持久化：`inmemory`（默认）、`sqlite`（EF Core + SQLite）、`postgres`（EF Core + Npgsql）。通过环境变量 `DatabaseProvider` 切换。

## 部署与运维

| 文件 | 说明 |
|------|------|
| `.env.example` | Docker 环境变量模板（复制为 `.env` 后填写） |
| `docker-compose.yml` | PostgreSQL + Backend + Web + Ollama 一键部署 |
| `scripts/rebuild-publish.ps1` | 重新生成 `publish/` 构建产物 |
| `scripts/generate-samples.ps1` | 自动生成 Excel/PPT/Word 测试样例 |
| `src/web/public/function-file.html` | Office Add-in 命令函数文件 |

## 深入文档指针

| 文档         | 路径                       | 内容                     |
| ------------ | -------------------------- | ------------------------ |
| 技术路线规划 | `docs/技术路线规划.md`     | 🆕 2026 重构架构方案    |
| 功能规划     | `docs/功能规划.md`         | 完整功能清单与优先级矩阵 |
| 实现路径     | `docs/实现路径规划.md`     | 5 个实施阶段与里程碑    |
| 开发环境     | `docs/开发环境搭建.md`     | Sidecar + Web + Backend 搭建 |
| 管理员指南   | `docs/管理员指南.md`       | MSI、服务、Admin API、词典模板 |
| 安全自查     | `docs/安全自查清单.md`     | 内测/试点发布安全门禁 |
| Add-in 分发  | `docs/Office-Add-in-企业分发指南.md` | Microsoft 365 集中部署与回滚 |
| 发布说明     | `docs/发布说明-0.1.1-stage1.md` | 试点候选版验证、限制与回滚 |
| 版权信息     | `docs/版权信息.md`         | 版权声明与许可条款       |
| Phase A 演示 | `docs/PhaseA-Demo-Script.md` | 三链路联调演示步骤 |

## 开发阶段进度

| 阶段                  | 状态      | 周期 | 关键交付                              |
| --------------------- | --------- | ---- | ------------------------------------- |
| Phase A：基础设施     | ✅ 完成 | 2周 | Sidecar + Web + Backend 框架 + COM 交互 + 测试基础 |
| Phase B：核心功能     | ⚠️ 基础版 | 4周 | Power Tools、快捷键(39 个已实现命令默认映射+配置导入/导出)、Visualizations、ModelCheck 活动工作表扫描、Word 模板(3)；全工作簿审计/100+ 快捷键对应命令规格待补 |
| Phase C：高级功能     | ⚠️ 基础版 | 4周 | DocBuilder(3)、PrepareToShare 部分增强、DeckCheck 增强、LinkManager UI、多宿主命令(Excel/PPT/Word)、Backend 命令注册；用户路径和产品化缺口仍在 |
| Phase D：打磨部署     | 🚧 进行中   | 2周 | MSI 安装器可复现构建(83MB, 含 Backend/Sidecar/Web/manifest)、管理员 MSI 安装/卸载回归已通过、COM 集成(22)、HTTP E2E(48)、Office 2024 用户式 E2E、Sidecar API 信封化、Sidecar `/api/*` 可选本地令牌、Sidecar `/api/execute` 审计自动上报、Web UI 显式点击审计、Admin 诊断摘要/诊断包/审计摘要/趋势/热力图/审计保留策略与 Web 诊断面板、Sidecar 服务令牌读取企业词典、DeckCheck Web 查看器（含 Logo 存在/位置检查与品牌化 HTML/PDF 报告基础版）、NamesManager 面板与 Sidecar 路由、链接元数据驱动刷新基础版、Docker 编排验证、Ribbon function-file 静态回归、PPT 形状工具全量 Ribbon 入口、Ribbon 真实侧载已显示并完成初轮点击巡检；剩余：逐命令行为验收归档、企业级 Dashboard/SSO/AIWA 生产接入 |

## MVP 边界

| 纳入 MVP | 暂不纳入 MVP |
| -------- | ------------ |
| Sidecar 基础框架 + 原生 COM 互操作      | 完整 SSO、复杂 RBAC、多租户计费 |
| 命令目录注册 + Win32 全局快捷键基础版 | AIWA 真实大模型生产调用 |
| Power Tools 基础版 | 完整 PowerPoint / Word 插件能力（Sidecar 工具部分就绪，Ribbon/用户入口仍需补齐） |
| Visualizations 与 Model Check 基础版 | 100+ 快捷键对应命令规格与全量产品化覆盖 |
| Prepare to Share 与 Workbook Optimization 基础版 | Brandfetch 等第三方 Logo 数据服务生产集成 |
| Excel → PowerPoint Range 链接原型，Chart 链接 helper | 私有化部署包和完整企业运维平台 |
| 后端健康检查、配置读写、链接元数据、审计事件 |  |

> **本轮审计修复 (2026-06-06)**：
> - 修复 `DeckCheckReport` 契约缺口：补齐 `MissingSlideNumbers`、`DenseTextSlides`，并在 DeckCheck 扫描中实际统计。
> - 修复 Sidecar `/api/execute` 回归：恢复 `commandId` 非空校验、`host` 白名单和大小写归一化。
> - 修复 Word 命令路由：`word.build-cim`、`word.build-management-presentation` 已接入 Sidecar，并在无活动文档时自动创建文档。
> - 稳定 COM 测试：Sidecar 测试程序集禁用并行，避免多个测试同时启动/关闭 Office 进程。
> - 修复 MSI 打包链路：`scripts/build-installer.ps1` 现在单文件发布 Backend/Sidecar，生成 Web/manifest WiX 片段，并构建完整 `ModelForge.msi`。
> - 修复 Windows Service 启动链路：Backend/Sidecar 均启用 `UseWindowsService`，避免 MSI 注册服务后仍按控制台生命周期启动。
> - 修复 Backend 发布版监听端口：`appsettings.json` 固定 `Urls=http://localhost:5095`，避免发布版/服务模式回落到 ASP.NET Core 默认 `:5000`。
> - 推进 Web Add-in 产品化：Admin Console 已接入 Corporate Dictionary 后端 API；AIWA Mock 响应已增加 Corporate Dictionary 后处理。
> - 补齐 PPT 形状工具矩阵：Sidecar、Backend 命令目录和 Office Ribbon 已支持对齐左/中/右/上/中/下、水平/垂直分布、统一宽/高/尺寸，并新增 manifest 静态回归防止入口缺失。
> - 验证结果：Solution Release 构建 0 error / 0 warning；非 COM 回归 Backend 60/60、Sidecar 163/163、E2E 47/47，COM 22/22、Web 91/91 全部通过；最新定向回归 Sidecar 非 COM 170/170、Web 95/95 通过；`docker compose config --quiet` 通过。
> - 用户式 Office E2E：`scripts/manual-office-e2e.ps1` 已在 Office 2024 上通过，覆盖 Excel IFERROR、PPT 对齐、Excel→PPT OLE 链接、Word 模板生成；修复了 Sidecar COM 缓存过期、Excel→PPT `PasteSpecial` 参数和前端兜底错误文案乱码。
> - MSI 回归状态：管理员 PowerShell 已执行 `scripts/test-msi-admin.ps1` 通过；安装、`ModelForge.Sidecar` / `ModelForge.Backend` 服务启动、`http://localhost:5200/health`、`http://localhost:5095/health`、卸载和服务移除均通过，日志位于 `artifacts/manual-e2e/msi-install-admin.log` 与 `artifacts/manual-e2e/msi-uninstall-admin.log`。非管理员权限失败和相对 MSI 路径 `1324` 失败路径也已归档。
> - 本地开发启动脚本：`scripts/dev-all.cmd` 已改为纯 ASCII 批处理，避免中文乱码破坏 `cmd` 语法；已接入 `scripts/check-office-runtime.ps1` 前置预检，防止 WPS/Kingsoft 抢占 Office COM；已验证可启动 Backend、Sidecar 和 Web Add-in，并通过 `http://localhost:5095/health`、`http://localhost:5200/health`、`http://localhost:5173/function-file.html`。
> - 旧 VSTO 残留清理：用户侧 Excel 启动时仍尝试加载 `file:///D:/CODES/model-forge/src/vsto/ModelForge.Excel/bin/Debug/ModelForge.Excel.vsto`，根因为 `HKCU\Software\Microsoft\Office\Excel\Addins\ModelForge.Excel` 历史注册表项残留；已新增并执行 `scripts/clear-legacy-vsto-addins.ps1`，当前未再发现 ModelForge VSTO 注册项。
> - 修复真实 Office Ribbon 不显示：`manifest/modelForge.web.xml` 原 `CustomTab` 下存在嵌套 `Group`，桌面 Office 会忽略该 Ribbon 定义；现已改为 `CustomTab -> Group -> Control` 合法层级，并新增 `officeManifest.test.ts` 防止 Ribbon Group 嵌套回归。新增 `scripts/setup-office-addin-sideload.ps1`，通过本机共享目录 + Office TrustedCatalog 方式侧载 manifest；直接复制到 `WEF` 缓存目录不作为可靠侧载步骤。
> - 修复共享文件夹列表为空的 manifest 校验问题：所有 Ribbon `Supertip` 已补齐必需 `Description`，新增 `Command.Tooltip` 长字符串资源，并扩展 `officeManifest.test.ts` 校验所有 Supertip 同时包含 `Title` 与 `Description`。当前 manifest/function-file 定向测试 9/9 通过。
> - 修复 Ribbon function-file 链路：Presentation/Document host 已补齐 `FunctionFile`，`function-file.html` 统一解析 Sidecar `ApiEnvelope<T>`、按命令前缀映射 Excel/PowerPoint/Word host，并新增静态测试覆盖 manifest ExecuteFunction 与函数文件契约。
> - 修复真实 Ribbon 点击首轮缺陷：Excel/Word/PowerPoint 已能从共享目录加载 `ModelForge`；针对“部分按钮无反应”问题，`function-file.html` 新增成功/失败用户可见反馈页 `feedback.html`，不再只写浏览器控制台；Sidecar `/api/execute` 对宿主不支持的未知命令返回 `400 ApiEnvelope<SidecarExecuteResponse>`，避免无效按钮被误报为成功。
> - 修复 PPT 形状工具算法：对齐改为基于选中形状整体边界盒；水平/垂直分布改为等边距分布（考虑不同宽高），替代 PowerPoint COM 内置分布常量/中心点分布的不确定行为，并新增纯几何单元测试覆盖。
> - 修复 WPS/旧 COM 误绑定：本机存在 Kingsoft/WPS 进程时，`Excel.Application` ROT 会返回 `Microsoft Excel 12.0` 兼容 COM，导致 Ribbon 命令 500 或用户感知为“按钮无反应”；Sidecar 现在校验 COM 版本和路径，拒绝 WPS/Office 12 绑定并返回“Excel 未运行”结构化提示。新增 `scripts/check-office-runtime.ps1`，可检测/关闭 `wps.exe`、`et.exe`、`wpp.exe`，并确认 Sidecar `/api/status` 为 Microsoft Office 16.0；`dev-all.cmd` 和 `manual-office-e2e.ps1` 已接入该预检。
> - 修复 Web 登录链路：`LoginPage` 已按后端 `ApiEnvelope<LoginResponse>` 解包，避免真实登录成功后把 token/user 读成 `undefined`；新增登录响应成功、401、后端错误和畸形信封测试。
> - 修复 Web 主界面用户路径：`App.tsx` 清理用户可见乱码文案，恢复 Links 面板入口，并将命令面板从短 ID 改为后端目录完整 `excel.*` 命令 ID，避免按钮列表匹配为空。
> - 清理 Web Admin/AIWA 可见乱码：Admin Console 改为中文可读文案并兼容裸 JSON/`ApiEnvelope<T>` 两类响应；AIWA Mock 输出和 Corporate Dictionary 结果文案已恢复可读中文。
> - 审计日志导出：Backend 新增 `/api/admin/audit-events/export` CSV 导出端点，Admin Console 增加“导出 CSV”按钮，并新增 E2E 覆盖。
> - Names Manager 路由：`excel.names-manager` 已接入 Sidecar `/api/execute`，支持 `scan` 和 `delete/deleteInvalid` 参数，并新增 HTTP E2E 可达性覆盖。
> - Backend 业务错误信封：命令分发、审计、链接和字典错误路径已从裸 JSON 统一为 `ApiEnvelope<object>`，并新增 E2E 覆盖。
> - Deck Check PDF/Logo 基础版：`ppt.deck-check` 支持 `checkLogos`、`exportPdf`、`reportPath` 参数，输出 `LogoIssues` 与 `ReportPath`；Web Deck Check 面板增加“导出 PDF 报告”入口。
> - Deck Check 品牌报告模板基础版：`ppt.deck-check` 支持 `templateName`、`reportTitle`、`brandPrimaryColor`、`brandAccentColor` 与 Logo 位置/尺寸阈值参数，输出 `LogoPositionIssues`、`TotalIssues`、`OverallStatus`，HTML/PDF 报告包含状态摘要、品牌色、企业模板名称和 Logo 位置统计。
> - 统一 Admin/API 用户路径：`/api/admin/users`、新增用户、启停用户已返回 `ApiEnvelope<T>`；创建用户补齐 `username`/`password`/`role` 校验、重复用户 409、缺失用户 404 信封错误；修复启停用户响应 active 状态取反问题；`LoginRequest`/`LoginResponse`/`AdminUser*` 已集中到 Contracts；`apiClient.login` 与 Admin 方法复用统一信封解包。
> - Admin 角色权限只读基础版：新增 `/api/admin/roles`，返回内置 RBAC 角色权限矩阵；Admin Console 增加“角色权限”面板；自定义角色和用户组映射仍待产品化。
> - Admin 审计统计摘要与筛选基础版：`/api/admin/audit-events`、`/export`、`/summary` 已支持事件类型、用户、宿主、级别、命令/资源、关键词和时间窗口筛选；Admin Console 审计页使用同一条件刷新列表、摘要、趋势条形图和 CSV 导出；事件类型/用户/宿主摘要和热力图单元已支持点击下钻筛选。
> - Admin 审计热力图基础版：`/api/admin/audit-events/summary` 返回事件类型 × 宿主 `heatmap` 矩阵（Top 8 事件类型），Admin Console 审计页展示高频功能热力图并支持按事件类型 + 宿主下钻；多维自定义分组仍待补。
> - 遥测关闭开关：`TelemetryEnabled=false` 默认跳过普通信息级使用统计并返回 `recorded=false`；安全/认证/管理和 Warning+ 事件仍记录，满足试点隐私合规基础要求。
> - Corporate Dictionary 高亮 UI：Admin Console 样例检查结果已按后端命中位置分段渲染，高亮违规词并展示替换建议；新增无命中、排序和重叠/非法位置过滤测试。
> - Corporate Dictionary JSON 导入/导出：新增 `/api/dictionary/export`、`/api/dictionary/import`，`DictionaryTerm`/`DictionaryCheck*`/`DictionaryImport*`/`DictionaryExportResponse` 已集中到 Contracts；Admin Console 支持导出/导入 JSON，并补 Backend、E2E、Web 回归测试。
> - Corporate Dictionary CSV/XLSX 模板导入/导出：Admin Console 支持从 CSV 和 Excel `.xlsx` 模板导入术语并导出模板；`.xlsx` 采用轻量 OOXML 解析，未引入存在无修复高危审计的 `xlsx` 包；覆盖逗号/引号转义、XLSX 往返和缺失表头校验。
> - 快捷键配置基础版：Sidecar 新增 `/api/shortcuts/export` 和 `/api/shortcuts/import` 信封端点，导入前原子校验冲突并避免半清空；默认映射已覆盖当前 39 个已实现 Excel/PPT/Word 命令并修复 `Names Manager` 默认键冲突；Web Excel 面板支持快捷键刷新、编辑、保存、JSON 导入/导出；100+ 继续扩展需要先定义并实现对应命令/用户路径。
> - 链接元数据驱动精准刷新基础版：`excel.refresh-links` 会读取 Backend `/api/links`，优先按 `targetAddress` 精准定位 PowerPoint `SlideN/ShapeN|ChartN|Name` 和 Word `FieldN|InlineShapeN|TableN` 目标刷新；当后端不可达、无元数据或目标地址不足以定位时保留本机全量刷新回退；失效链接自动修复仍待补。
> - Admin 诊断摘要/诊断包：新增 `/api/admin/diagnostics` 与 `/api/admin/diagnostics/bundle`，导出版本、数据库连接、命令/链接/字典/审计计数、脱敏配置摘要、运行时信息和最近审计事件；Web Admin Console 已提供“诊断”面板和下载按钮；刻意排除密钥、认证令牌、工作簿内容和本机日志文件。
> - Admin 审计保留策略基础版：新增 `/api/admin/audit-events/retention`，支持 `dryRun` 预览和真实删除，默认保留 `90` 天并可通过 `AuditRetentionDays` 配置/环境变量覆盖；InMemory/SQLite/PostgreSQL 审计存储均支持按 `cutoffUtc` 清理，真实清理会记录 `admin.audit.retention.pruned`，Admin Console 审计页提供预览/执行入口，诊断摘要显示保留天数和待清理数量。
> - Web UI 显式点击审计基础版：新增 `uiAudit` 前端服务，导航、刷新/退出、命令执行、Omnibar、快捷键配置、Deck Check、链接刷新、Admin 审计/诊断、Corporate Dictionary 和 AIWA 显式用户操作会以 `ui.*` 事件 best-effort 上报 `/api/audit-events`；该类普通信息级事件受 `TelemetryEnabled` 控制，上报失败不阻塞 UI。
> - Sidecar 本地 API 令牌：`Sidecar:LocalApiToken` 为空时保持开发兼容；配置后所有 `/api/*` 端点要求 `X-ModelForge-Sidecar-Token`，`/health` 继续公开；Web task pane 支持通过 Vite 环境变量或本地 UI/localStorage 带令牌，Ribbon function-file 支持全局变量或 localStorage。
> - Sidecar 执行审计自动上报：`/api/execute` 成功上报 `command.executed`，输入校验失败上报 `command.validation_failed`，Office 未运行或 COM 异常上报 `command.failed`；Backend 仍按 `TelemetryEnabled` 和严重级别策略决定是否落库，审计上报失败仅记录日志，不阻塞本机命令响应。
> - Backend 管理写操作审计：Admin 用户创建/启停、配置更新、Corporate Dictionary 术语新增/删除/导入会记录 `admin.*` 审计事件，actor 使用 JWT `sub`，resourceId 指向用户、配置 scope 或术语 ID。
> - Backend 认证审计：`/api/auth/login` 成功记录 `auth.login.succeeded`，失败记录 `auth.login.failed`，用于保留基础安全审计。

> **Sidecar API 状态 (2026-06-06)**：`/api/execute` 已添加输入校验（`commandId` 非空 + `host` 白名单）并统一返回 `ApiEnvelope<SidecarExecuteResponse>`；成功、校验失败、Office 未运行和 COM 异常路径会自动上报 Backend 审计事件；`/api/status` 已返回 `ApiEnvelope<SidecarStatusResponse>` 并记录异常日志，`/api/excel/info` 作为兼容别名保留；`SidecarExecuteRequest` 已集中到 Contracts。
## 风险警示

1. **Office 版本兼容性**：原生 COM 需覆盖 Office 2016/2019/365 多版本手工回归；Office PIA 仅作为 Windows-only 预编译后备方案
2. **全局键盘钩子**：WH_KEYBOARD_LL 可能触发杀毒软件警告；需 Code-sign Sidecar + 提供关闭选项
3. **跨应用链接稳定性**：增加链接健康检查，提供自动修复向导
4. **AI 数据安全**：支持私有化部署选项，支持 BYOK
5. **COM 进程隔离**：Sidecar 通过 `Marshal.GetActiveObject()` 连接运行中 Office 实例；权限不匹配时记录诊断日志

## 样例文件

运行 `.\scripts\generate-samples.ps1` 自动生成测试用 Excel/PowerPoint/Word 样例文件。

## 协作流程

- **代码审查**：所有 PR 必须经过代码审查
- **测试要求**：单元测试覆盖率 > 60%
- **文档同步**：代码变更必须同步更新相关文档
- **提交规范**：遵循 Conventional Commits 规范
- **CI**: GitHub Actions 自动运行 Backend 测试 (`dotnet test`) + Sidecar 测试 (`dotnet test`) + Solution 构建 + Web Add-in 构建 (`npm run build`)，均在 windows-latest 上执行。Docker Compose 支持本地一键部署 (postgres+backend+ollama+nginx)
