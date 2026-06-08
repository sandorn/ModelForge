# ModelForge Changelog

## [Unreleased]

### 新增

### 变更

### 已修复

## 0.2.0 (2026-06-08)

### 新增（26 轮迭代，111 新命令，146 命令总计）

#### Excel（+37 命令）
- ModelCheck 全工作簿扫描、公式追踪可视化（箭头+高亮）、跨工作表追踪
- Black-Scholes 期权模板、LBO 模型模板、XIRR/NPV 模板、用户模板管理
- Quick Chart（柱状图/折线图）、批量图表格式化、添加数据序列
- 条件格式预设（热力图/数据条/图标集/Top10）
- 数据验证（下拉列表/数值范围）、AutoSum、Paste Values
- 公式简化分析、描述性统计、相关性分析
- 打印区域、冻结窗格、删除重复值、隐藏空白工作表
- Chart 链接到 PPT、Range 链接到 Word

#### PowerPoint（+45 命令）
- TurboShapes（Harvey Ball/Progress Bar/Rating Stars）
- Section 管理、页码管理、Tombstone 交易墓碑
- 形状旋转/交换位置、层级管理（置顶/置底）
- Reformat View（按字体/字号搜索和替换）
- MasterShapes 形状库、Meta Shapes 元数据
- Logo Library、动画（出现/淡入/飞入）、过渡效果
- 背景色、版式应用、新建/复制/移动幻灯片
- 幻灯片导出 PNG

#### Word（+22 命令）
- 分页符/分节符/TOC/封面页
- 表格行列插入/SUM 公式
- 文档大纲导航/跳转、Heading 样式
- 查找替换、文档统计、页边距/方向

#### Backend（+7 API）
- Dashboard 统计（Top 命令/宿主分布/趋势图）
- AIWA Chat（Ollama/OpenAI-compatible/Mock 三 Provider）
- 企业策略配置、品牌模板、合同条款库
- 用户组管理、自定义 RBAC 角色 CRUD
- 审计完整性验证、版本管理
- Admin 用户编辑/删除 API

#### Web（+13 面板）
- Dashboard 统计面板（趋势条形图+活跃功能排行+快捷操作）
- AI 配置面板、快捷键参考面板、模板浏览器
- Admin Console 用户编辑/删除 UI
- React.lazy 代码分割（主包 492→401KB）
- Omnibar Office 原生命令 + 搜索历史 + 宿主检测
- 命令面板宿主过滤（Excel/PPT/Word/All）

#### 基础设施
- Serilog 文件日志（Console + File 双输出）
- 用户 DB Schema（EF Core UserEntry + SqliteUserStore）
- Office.js 宿主自动检测

### 变更
- 版本号：0.1.3 → 0.2.0
- 快捷键：88 个命令有快捷键（A-Z + Shift 全部占用）
- 测试：325 → 337（+12 企业 API 测试）

### 验证
- Backend 72/72 · Sidecar 170/170 · Web 95/95 全部通过

## 0.1.3 (2026-06-07)

### 新增

#### 数据库

- 多 Provider 支持: `inmemory` / `sqlite` / `postgres`，通过 `DatabaseProvider` 环境变量切换
- Npgsql 集成: Docker Compose 自动使用 PostgreSQL
- `SqliteLinkMetadataStore`: 链接元数据 SQLite 持久化

#### API

- `GET /api/admin/audit-events?count=50`: 管理员审计事件查询端点
- `GET /health`: 增强，返回数据库连接状态

#### 持久化

- EF Core + SQLite: `ModelForgeDbContext` 含 4 张表（Configurations, AuditEvents, LinkMetadata, DictionaryTerms）
- `SqliteConfigurationStore`: 配置持久化（替代 InMemoryConfigurationStore）
- `SqliteAuditSink`: 审计事件持久化（替代 InMemoryAuditSink）
- 通过 `DatabaseProvider=inmemory|sqlite|postgres` 配置切换（默认仍为内存模式以兼容现有测试）

#### 部署

- `.env.example`: Docker 环境变量模板
- `scripts/rebuild-publish.ps1`: 一键重生成构建产物脚本
- `docker-compose.yml`: 新增 `env_file` 支持
- `scripts/build-installer.ps1`: 修复 MSI 可复现构建链路，发布 Backend/Sidecar 自包含单文件服务，打包 Web Add-in、`function-file.html` 与 Office manifest
- `scripts/manual-office-e2e.ps1`: 新增本机用户式 Office E2E 脚本，按真实用户方式启动 Excel/PowerPoint/Word，通过 Sidecar API 执行 Excel IFERROR、PPT 对齐、Excel→PPT 链接和 Word 模板生成，并归档验证产物
- `scripts/check-office-runtime.ps1`: 新增 Office 运行时预检，检测/可选关闭 WPS/Kingsoft 进程，确认 Microsoft Office 2016+/Office 2024 进程路径，并校验 Sidecar `/api/status` 的 COM 版本；`scripts/dev-all.cmd` 与 `scripts/manual-office-e2e.ps1` 已接入该预检
- `ModelForge.msi`: 当前构建约 83 MB，已通过 `wix msi validate`

#### 测试覆盖

- Backend 单元测试: JwtService, UserStore, ConfigurationStore, AuditSink, AuditRetention, TelemetryPolicy, CommandCatalog, DictionaryService, LinkMetadataStore, RoleDefinitions (60 tests)
- Sidecar 单元测试扩展: CellClassifier, ToggleSign, StatisticsInserter, ModelCheckLogic, ShortcutRegistry 原子替换, 39 个默认快捷键唯一性, LinkRefreshPlanner, DeckCheck PDF/Logo/品牌报告模板, BackendBridgeClient 服务级字典读取, Sidecar 本地令牌鉴权, ShapeTools 几何算法, Office COM 运行时校验 (170 non-COM tests with `FullyQualifiedName!~Com`; full Sidecar project includes COM tests)
- Web Add-in 前端测试: authStore, contracts, bridgeStore, ApiClient, LoginPage, App command groups, SidecarClient, AdminConsole, AiwaChat, uiAudit, Office manifest/function-file + Ribbon 可见反馈 + DeckCheck PDF/Logo parsing + Sidecar 令牌头/本地保存 + Admin 角色权限/审计摘要/筛选/下钻/保留策略解包 (95 tests, Vitest + jsdom)
- E2E 覆盖新增认证审计、Admin 用户生命周期、管理写操作审计、角色权限只读矩阵、审计统计摘要、审计保留策略、遥测关闭行为、诊断摘要/诊断包、Corporate Dictionary 导入/导出、链接元数据刷新请求、Sidecar 快捷键导入/导出和 Sidecar 执行审计自动上报：登录、登录成功/失败审计、列表、新增、启停、重复用户、非法创建请求、角色权限、审计摘要、审计保留 dry-run/校验、管理用户/配置/字典写操作审计、遥测关闭、诊断摘要、术语导入/导出、空导入错误、链接刷新请求、快捷键导入/导出、冲突回滚和 Sidecar 校验失败审计传播

#### CI/CD

- CI 管道: `dotnet run` → `dotnet test`, 新增 Solution 级构建 + 全量测试 Job

#### API

- Sidecar `/api/execute` 输入校验已落地（CommandId 非空, Host 合法性），并统一为 `ApiEnvelope<SidecarExecuteResponse>` 响应
- Sidecar `/api/status` 返回 `ApiEnvelope<SidecarStatusResponse>`；`/api/excel/info` 作为兼容别名保留，异常路径记录 Warning 日志
- Sidecar 快捷键配置 API：新增 `/api/shortcuts/export` 与 `/api/shortcuts/import`，返回 `ApiEnvelope<ShortcutExportResponse>` / `ApiEnvelope<ShortcutImportResponse>`，导入冲突时保持现有注册表不变；默认映射覆盖当前 39 个已实现 Excel/PPT/Word 命令并修复 `Names Manager` 默认键冲突
- Web Add-in TypeScript 契约已同步 C# Contracts；`SidecarExecuteRequest` 与 Corporate Dictionary DTO 本地重复定义已清理
- apiClient 扩展 8 个新方法 (dispatchCommand, config, audit, links, login)
- Web Add-in Admin Console 接入 Corporate Dictionary 后端 API，支持术语列表、添加/删除、样例文本检查、命中高亮、JSON 批量导入/导出和 CSV/XLSX 模板导入/导出
- Admin Console 角色权限只读页：Backend 新增 `/api/admin/roles` 返回内置 RBAC 权限矩阵，Web Admin Console 增加“角色权限”标签页；自定义角色和用户组映射仍待产品化
- Admin 审计统计摘要：Backend 新增 `/api/admin/audit-events/summary`，Admin Console 审计页展示最近事件类型、用户和宿主 Top 10
- Admin 审计趋势、热力图与筛选基础版：`/api/admin/audit-events`、`/export`、`/summary` 支持事件类型、用户、宿主、级别、命令/资源、关键词和时间窗口筛选；`/summary` 返回事件类型 × 宿主 heatmap；Admin Console 审计页用同一条件刷新列表、摘要、趋势条形图、热力图和 CSV 导出；可交互趋势图仍待补
- Admin 审计下钻基础版：Admin Console 审计页的事件类型/用户/宿主摘要和事件类型 × 宿主热力图单元可点击写入筛选条件并刷新列表、摘要与 CSV 导出查询；时间趋势区间下钻和自定义多维分组仍待补
- Admin 审计保留策略基础版：Backend 新增 `/api/admin/audit-events/retention`，支持 `dryRun` 预览和真实清理；默认 `AuditRetentionDays=90`，可通过配置或环境变量覆盖；InMemory/SQLite/PostgreSQL 审计存储均支持按 `cutoffUtc` 删除；Admin Console 审计页提供预览/执行入口，诊断摘要显示保留天数和待清理数量
- Web UI 显式点击审计基础版：新增 `uiAudit` 前端服务，导航、刷新/退出、命令执行、Omnibar、快捷键配置、Deck Check、链接刷新、Admin 审计/诊断、Corporate Dictionary 和 AIWA 显式操作会以 `ui.*` 事件 best-effort 上报 `/api/audit-events`；上报失败不阻塞 UI，普通信息级事件受 `TelemetryEnabled` 控制
- Sidecar 执行审计自动上报：`/api/execute` 成功上报 `command.executed`，输入校验失败上报 `command.validation_failed`，Office 未运行或 COM 异常上报 `command.failed`；Backend 仍按 `TelemetryEnabled` 和严重级别策略决定是否落库，审计上报失败仅记录日志，不阻塞本机命令响应
- Backend 管理写操作审计：Admin 用户创建/启停、配置更新、Corporate Dictionary 术语新增/删除/导入记录 `admin.*` 审计事件，actor 使用 JWT `sub`，resourceId 指向用户、配置 scope 或术语 ID
- Backend 认证审计：`/api/auth/login` 成功记录 `auth.login.succeeded`，失败记录 `auth.login.failed`，用于保留基础安全审计
- 遥测关闭开关：`TelemetryEnabled=false` 默认跳过普通信息级使用统计并返回 `recorded=false`；安全/认证/管理和 Warning+ 事件仍记录
- Web Add-in Excel 面板接入 Sidecar 快捷键配置，支持刷新、编辑、保存、JSON 导入/导出
- Corporate Dictionary 新增 `/api/dictionary/export` 与 `/api/dictionary/import`，返回 `ApiEnvelope<DictionaryExportResponse>` / `ApiEnvelope<DictionaryImportResponse>`
- AIWA Mock 响应接入 Corporate Dictionary 后处理，自动调用 `/api/dictionary/check` 并展示命中项/替换建议
- PowerPoint ShapeTools 在 Sidecar/Backend 命令目录和 Office Ribbon 补齐对齐/分布/统一尺寸矩阵
- Ribbon function-file 新增成功/失败可见反馈页 `feedback.html`，真实 Office 点击不再只写控制台；Sidecar `/api/execute` 对未知命令返回 400 信封错误，避免无效按钮被误判为成功
- PowerPoint ShapeTools 对齐/分布算法改为基于选中形状边界盒的确定性几何计算；水平/垂直分布使用等边距策略并覆盖不同宽高形状
- Sidecar COM 连接增加 Microsoft Office 运行时校验：检测并拒绝 Kingsoft/WPS Office 和 Office 12 兼容 COM，避免 `Excel.Application` ROT 指向 WPS 时把 Ribbon 命令误执行成 500
- Ribbon 手工回归确认 WPS 未关闭会导致 `Excel.Application` ROT 被 WPS 抢占；现通过 Sidecar 运行时校验 + `check-office-runtime.ps1` 前置预检双重防护，复测前要求关闭 `wps.exe` / `et.exe` / `wpp.exe`
- 审计日志 CSV 导出：Backend 新增 `/api/admin/audit-events/export`，Admin Console 增加导出按钮，并补 E2E 覆盖
- Admin 诊断摘要/诊断包：Backend 新增 `/api/admin/diagnostics` 与 `/api/admin/diagnostics/bundle`，返回版本、数据库连接、命令/链接/字典/审计计数、脱敏配置摘要、运行时信息和最近审计事件，排除密钥、认证令牌、工作簿内容和本机日志文件；Web Admin Console 新增“诊断”面板和下载按钮并通过 ApiClient 调用该端点
- Names Manager Sidecar 路由：`excel.names-manager` 接入 `/api/execute`，支持扫描和删除无效命名，并补 E2E 可达性覆盖
- Deck Check 品牌报告模板基础版：`ppt.deck-check` 支持 `checkLogos`、`exportPdf`、`reportPath`、`templateName`、`reportTitle`、`brandPrimaryColor`、`brandAccentColor` 和 Logo 位置/尺寸阈值参数，返回 `LogoIssues`、`LogoPositionIssues`、`TemplateName`、`ReportTitle`、`BrandPrimaryColor`、`BrandAccentColor`、`TotalIssues`、`OverallStatus` 和 `ReportPath`；Web Deck Check 面板增加状态摘要和“导出 PDF 报告”入口
- 链接元数据驱动精准刷新基础版：Sidecar `excel.refresh-links` 会读取 Backend `/api/links`，优先按 `targetAddress` 精准定位 PowerPoint `SlideN/ShapeN|ChartN|Name` 和 Word `FieldN|InlineShapeN|TableN` 目标刷新，并在后端不可达、无元数据或目标地址不足时回退本机全量刷新
- Sidecar 服务级字典读取：Backend 新增 `/api/dictionary/service-export`，仅在配置 `ModelForge:ServiceToken` 且请求头 `X-Service-Token` 匹配时返回企业词典；Deck Check 无 `forbiddenTerms` 参数时自动尝试读取 Backend Corporate Dictionary，失败时回退本地默认术语
- Sidecar 本地 API 令牌：新增 `Sidecar:LocalApiToken` 可选配置，启用后保护 Sidecar `/api/*` 并要求 `X-ModelForge-Sidecar-Token`；`/health` 继续公开，Web task pane 支持通过 Vite 环境变量或本地 UI/localStorage 带令牌，Ribbon function-file 支持全局变量或 localStorage
- Office manifest/function-file 静态回归覆盖：验证 Excel/PowerPoint/Word host 均声明 `FunctionFile`，`ExecuteFunction` 命令前缀映射到 Sidecar `excel`/`powerpoint`/`word` host，PPT Ribbon 暴露全部 13 个支持命令，并解析 `ApiEnvelope<T>` 错误响应
- Office Ribbon 真实侧载首轮巡检：Excel/Word/PowerPoint 均可从共享目录加载 `ModelForge`；巡检发现的按钮无反馈和 PPT 分布算法问题已进入本轮修复

#### 文档

- `docs/PhaseA-Demo-Script.md`: 三链路联调演示脚本 (7 步)
- `docs/API契约.md`: 新增 Sidecar REST API 章节 (4 端点)
- `docs/安全自查清单.md`: 新增内测/试点发布安全门禁清单
- `docs/Office-Add-in-企业分发指南.md`: 新增 Microsoft 365 集中部署、网络共享测试和回滚流程
- `docs/发布说明-0.2.0.md`: 发布说明、验证结果、已知限制和回滚步骤
- `docs/用户指南.md` / `docs/管理员指南.md`: 同步链接刷新、Corporate Dictionary CSV/XLSX 模板和发布验证说明
- `scripts/generate-samples.ps1`: 样例文件自动生成脚本
- 当前版本提升至 `0.2.0` / MSI `0.2.0.0`

### 变更

- **REMOVED**: `src/vsto/` 历史代码目录 (16 C# 文件)，已被 Sidecar 架构完全替代
- Sidecar DTO 整理完成：`SidecarExecuteRequest` 由 `ApiContracts.cs` 统一提供
- sidecarClient.ts 切换到 `/api/status`，`/api/excel/info` 保持旧客户端兼容

### 已修复

- Backend smoke test: 从 console `dotnet run` 迁移至 xUnit
- sidecarClient 解包 Sidecar `ApiEnvelope<T>`，并使用 `/api/status` 查询状态
- bridgeStore 类型引用修复 (SidecarHealth → HealthResponse, SidecarExcelInfo → SidecarStatusResponse)
- bridgeStore 兜底错误文案乱码修复为中文 `未知错误`
- Backend/Sidecar 启动入口补齐 `UseWindowsService`，避免 MSI 注册为 Windows Service 后无法按服务生命周期启动
- Backend 发布版默认监听端口固定为 `http://localhost:5095`，避免只依赖 `launchSettings.json` 导致服务安装后回落到 `:5000`
- MSI 构建脚本补齐 Web Add-in 静态资源和 manifest WiX 片段生成，避免安装包只包含骨架文件
- Sidecar Office COM 服务改为按需重新连接运行中的 Excel/PowerPoint/Word，避免用户关闭并重开 Office 后仍命中过期 COM 实例
- `IfErrorWrapper` 增加空 Excel/空选区/异常单元格防御，避免用户式操作返回 500
- Excel→PowerPoint OLE 链接修复 `Shapes.PasteSpecial` 参数顺序并加入剪贴板重试，解决真实 PowerPoint 粘贴链路 500
- Web Add-in manifest 为 Presentation/Document host 补齐 `FunctionFile`，避免 PPT/Word Ribbon `ExecuteFunction` 无函数文件入口；`function-file.html` 改为统一调用 Sidecar `/api/execute` 并抛出结构化错误
- Web 登录页改为解包 Backend `ApiEnvelope<LoginResponse>`，修复真实登录成功后 token/user 写入 `undefined` 的用户路径错误
- Web 主界面清理用户可见乱码文案，恢复 Links 面板入口，并修复命令面板使用短 ID 导致无法匹配后端 `excel.*` 命令目录的问题
- Web Admin Console 和 AIWA 清理用户可见乱码；Admin Console 兼容裸 JSON 与 `ApiEnvelope<T>` 响应，AIWA Mock 输出和 Corporate Dictionary 结果恢复可读中文
- Backend 业务错误路径（命令分发、审计、链接、字典）统一返回 `ApiEnvelope<object>` 并补 E2E 断言
- Backend Admin 用户端点统一返回 `ApiEnvelope<T>`；创建用户补齐 `username`/`password`/`role` 校验、重复用户 409、缺失用户 404 信封错误；修复启停用户接口返回 active 状态与实际状态相反的问题；`LoginRequest`/`LoginResponse`/`AdminUser*` 集中到 Contracts；`apiClient.login` 与 Admin 方法复用统一信封解包

### 验证

- Office 2024 用户式 E2E 已通过：`scripts/manual-office-e2e.ps1` 生成 `artifacts/manual-e2e/office-e2e-20260606-165021.xlsx`、`office-e2e-20260606-165021.pptx`、`office-e2e-20260606-165021.txt`
- MSI 管理员安装/卸载回归已通过：`scripts/test-msi-admin.ps1` 完成安装、双 Windows Service 启动、Backend/Sidecar `/health`、卸载和服务移除；日志归档在 `artifacts/manual-e2e/msi-install-admin.log` 与 `artifacts/manual-e2e/msi-uninstall-admin.log`。非管理员权限失败（1925 / 1603）和相对 MSI 路径失败（1324）已作为失败路径证据归档
- `scripts/dev-all.cmd` 改为纯 ASCII 输出和独立窗口启动，修复中文乱码导致批处理被解析成错误命令的问题；本地已验证 Backend、Sidecar 和 Web Add-in 健康检查通过
- `scripts/check-office-runtime.ps1` 已在 Microsoft Office 2024 Excel 进程下通过：`EXCEL.EXE` 路径为 `Microsoft Office\root\Office16`，Sidecar `/api/status` 返回 `connected=true`、`version=16.0`
- 新增 `scripts/clear-legacy-vsto-addins.ps1`，用于清理历史 `src/vsto` 架构遗留的 Office VSTO/COM 加载项注册表项；已清理 `HKCU\Software\Microsoft\Office\Excel\Addins\ModelForge.Excel`，避免 Excel 启动时继续尝试下载已删除的 `ModelForge.Excel.vsto`
- 修复 Office Ribbon 真实侧载不显示：`manifest/modelForge.web.xml` 移除非法嵌套 `Group`，恢复 `CustomTab -> Group -> Control` 层级；`officeManifest.test.ts` 增加 Ribbon Group 嵌套防回归测试，当前 manifest/function-file 定向测试 8/8 通过
- 修复 Office 共享文件夹列表为空的 manifest 校验问题：所有 Ribbon `Supertip` 补齐必需 `Description`，新增 `Command.Tooltip` 长字符串资源；manifest/function-file 定向测试扩展为 9/9 通过
- 新增 `scripts/setup-office-addin-sideload.ps1`，创建本机 Office Add-in 共享目录并注册 `HKCU\Software\Microsoft\Office\16.0\WEF\TrustedCatalogs`，替代直接复制到 `WEF` 缓存目录的非可靠侧载方式
- 回归命令通过：Sidecar 定向 COM 测试 3/3、Backend `dotnet test` 60/60、非 COM 回归 Backend 60/60、Sidecar 163/163、E2E 47/47；最新定向回归 Sidecar 非 COM 170/170、Web `npm test -- --run` 95/95；Admin 用户生命周期定向 E2E 2/2、Sidecar 执行审计定向 E2E 1/1、Web `npm run build`

## 0.1.0 (2026-06-03) — Phase A+B+C+D 初始交付

### 新增

#### 数据库

- 多 Provider 支持: `inmemory` / `sqlite` / `postgres`，通过 `DatabaseProvider` 环境变量切换
- Npgsql 集成: Docker Compose 自动使用 PostgreSQL
- `SqliteLinkMetadataStore`: 链接元数据 SQLite 持久化

#### API

- `GET /api/admin/audit-events?count=50`: 管理员审计事件查询端点
- `GET /health`: 增强，返回数据库连接状态

#### 持久化

- EF Core + SQLite: `ModelForgeDbContext` 含 4 张表（Configurations, AuditEvents, LinkMetadata, DictionaryTerms）
- `SqliteConfigurationStore`: 配置持久化（替代 InMemoryConfigurationStore）
- `SqliteAuditSink`: 审计事件持久化（替代 InMemoryAuditSink）
- 通过 `UseSqlite=true` 配置切换（默认仍为内存模式以兼容现有测试）

#### 部署

- `.env.example`: Docker 环境变量模板
- `scripts/rebuild-publish.ps1`: 一键重生成构建产物脚本
- `docker-compose.yml`: 新增 `env_file` 支持

#### Sidecar (.NET 10 Worker Service)

- Win32 全局键盘钩子 (WH_KEYBOARD_LL)，20 个快捷键注册
- .NET 原生 COM Interop (oleaut32!GetActiveObject + dynamic)
- 20+ 个命令已有基础实现/注册；部分命令仍存在用户路径或完整产品化缺口
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

