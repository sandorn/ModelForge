# ModelForge Changelog

## [Unreleased] — Phase A 收尾

### 新增


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

#### 测试覆盖
- Backend 单元测试: JwtService, ConfigurationStore, AuditSink, CommandCatalog, DictionaryService, LinkMetadataStore (36 tests)
- Sidecar 单元测试扩展: CellClassifier, ToggleSign, StatisticsInserter, ModelCheckLogic (120 tests total)
- Web Add-in 前端测试: authStore, contracts, bridgeStore (17 tests, Vitest + jsdom)

#### CI/CD
- CI 管道: `dotnet run` → `dotnet test`, 新增 Solution 级构建 + 全量测试 Job

#### API
- Sidecar `/api/execute` 结构化错误响应 (`ApiEnvelope<SidecarExecuteResponse>`)
- Sidecar `/api/execute` 输入校验 (CommandId 非空, Host 合法性)
- Sidecar `/api/status` 异常改为日志记录
- Web Add-in TypeScript 契约同步至 C# `ApiContracts.cs` (12 DTO + 6 enum)
- apiClient 扩展 8 个新方法 (dispatchCommand, config, audit, links, login)

#### 文档
- `docs/PhaseA-Demo-Script.md`: 三链路联调演示脚本 (7 步)
- `docs/API契约.md`: 新增 Sidecar REST API 章节 (4 端点)
- `scripts/generate-samples.ps1`: 样例文件自动生成脚本

### 变更

- **REMOVED**: `src/vsto/` 历史代码目录 (16 C# 文件)，已被 Sidecar 架构完全替代
- Sidecar DTO 迁移: `SidecarExecuteRequest` 等从 `SidecarEndpoints.cs` → `ApiContracts.cs`
- sidecarClient.ts: `/api/excel/info` → `/api/status`, 本地类型 → 共享 contracts

### 已修复

- Backend smoke test: 从 console `dotnet run` 迁移至 xUnit
- sidecarClient 错误 API 路径修复
- bridgeStore 类型引用修复 (SidecarHealth → HealthResponse, SidecarExcelInfo → SidecarStatusResponse)


## 0.1.0 (2026-06-03) — Phase A+B+C+D 初始交付

### 新增


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
