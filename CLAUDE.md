# ModelForge - AI Agent 规则手册

## 项目概览

| 属性         | 说明                                                                      |
| ------------ | ------------------------------------------------------------------------- |
| **项目名称** | ModelForge                                                                |
| **项目类型** | Office 生产力插件套件                                                     |
| **技术架构** | Sidecar (.NET 10 + 原生 COM) + Office JS API + 后端桥接 (ASP.NET Core 10) |
| **参考产品** | Macabacus                                                                 |
| **目标用户** | 投行、私募、风投、FP&A、管理咨询等财务专业人士                            |

## 核心架构（2026 重构版）

```
┌─────────────────────────────────────────────────────────────────┐
│                    Ribbon 功能区 (融合)                         │
├─────────────────────────────┬───────────────────────────────────┤
│  Sidecar (.NET 10 + 原生 COM)│        Web Add-in Commands       │
│    (COM 深度操作 + 快捷键)     │     (灵活更新无需安装包)       │
├─────────────────────────────┴───────────────────────────────────┤
│                                                                 │
│  Sidecar 负责：                     Web Add-in 负责：           │
│  ├── 全局键盘钩子 (Win32)           ├── Admin Console           │
│  ├── 原生 COM 互操作                ├── Shared Libraries        │
│  ├── 公式追踪 + Model Check         ├── Corporate Dictionary    │
│  ├── 文件优化 + Prepare to Share    ├── Dashboard 统计          │
│  ├── Power Tools                    ├── Omnibar 速查栏          │
│  ├── Visualizations                 ├── AIWA 前端 UI            │
│  ├── 跨应用数据联动                 └── 企业策略下发            │
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

| 模块 | 路径 | 说明 |
| --- | --- | --- |
| Sidecar | `src/sidecar/ModelForge.Sidecar/` | .NET 10 REST + 原生 COM |
| Web Add-in | `src/web/` | 管理后台与 AI 功能 |
| 后端 API | `src/backend/` | 跨插件通信桥接 + 企业服务 |

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

| 变量名 | 说明 | 示例值 |
| --- | --- | --- |
| `MODELFORGE_DB_CONNECTION` | 数据库连接字符串 | `Host=localhost;Database=ModelForge;Username=postgres` |
| `VITE_MODELFORGE_API_URL` | Web Add-in 后端 API 地址 | `http://localhost:5095` |
| `VITE_MODELFORGE_SIDECAR_URL` | Web Add-in Sidecar 地址 | `http://localhost:5200` |
| `VITE_MODELFORGE_SIDECAR_TOKEN` | 任务窗格调用 Sidecar `/api/*` 的本地令牌 | Vite 构建期注入；Ribbon function-file 建议用 localStorage |
| `Sidecar__LocalApiToken` | Sidecar 本地 `/api/*` 令牌 | 空值表示开发兼容模式 |
| `ModelForge__ServiceToken` | Backend 服务级导出令牌 | Sidecar 读取企业词典时与 `Sidecar__ServiceToken` 保持一致 |
| `MODELFORGE_AI_API_KEY` | AI 服务 API Key | `sk-xxx...` |
| `MODELFORGE_SSO_ENABLED` | 是否启用 SSO | `true/false` |
| `Sidecar__KeyboardHookEnabled` | 启用键盘钩子 | `true/false` |

## 技术栈

| 层级 | 技术选型 | 版本 |
| --- | --- | --- |
| Sidecar 运行时 | .NET Web Host (Minimal API) | 10.0 |
| COM 互操作 | 原生 COM (P/Invoke) | — |
| Office JS API | Office.js | 1.1+ |
| 后端框架 | ASP.NET Core | 10.0+ |
| 前端框架 | React | 18+ |
| 前端语言 | TypeScript | 5.0+ |
| 构建工具 | Vite | 6.0+ |
| UI 组件库 | Fluent UI React | 9.0+ |
| 状态管理 | Zustand | 4.0+ |
| 数据库 | InMemory / SQLite / PostgreSQL | - |

## 测试统计

| 项目 | 测试类 | 测试数 | 状态 |
| --- | --- | --- | --- |
| Backend 单元测试 | 12 | 60 | ✅ 全部通过（含 SQLite 持久化测试） |
| Sidecar 非 COM 回归 | 15 | 170 | ✅ 全部通过（`FullyQualifiedName!~Com` 过滤） |
| COM 集成测试 | 4 | 22 | ✅ Excel/PPT/Word 原生 COM 互操作 |
| E2E 测试 | 2 | 48 | ✅ Backend + Sidecar HTTP 全链路 |
| 用户式 Office E2E | 1 | 1 | ✅ Office 2024 命令级链路通过 |
| Web Add-in (TS) | 13 | 95 | ✅ 全部通过 |
| 总计 | 47 | 396 | ✅（不含 COM 22 时为 374） |

## 部署与运维

| 文件 | 说明 |
| --- | --- |
| `.env.example` | Docker 环境变量模板（复制为 `.env` 后填写） |
| `docker-compose.yml` | PostgreSQL + Backend + Web + Ollama 一键部署 |
| `scripts/rebuild-publish.ps1` | 重新生成 `publish/` 构建产物 |
| `scripts/generate-samples.ps1` | 自动生成 Excel/PPT/Word 测试样例 |
| `src/web/public/function-file.html` | Office Add-in 命令函数文件 |

## 深入文档指针

| 文档 | 路径 | 内容 |
| --- | --- | --- |
| 技术路线规划 | `docs/技术路线规划.md` | 2026 重构架构方案 |
| 功能规划 | `docs/功能规划.md` | 完整功能清单与优先级矩阵 |
| 实现路径 | `docs/实现路径规划.md` | 5 个实施阶段与里程碑 |
| 开发环境 | `docs/开发环境搭建.md` | Sidecar + Web + Backend 搭建 |
| 管理员指南 | `docs/管理员指南.md` | MSI、服务、Admin API、词典模板 |
| 安全自查 | `docs/安全自查清单.md` | 内测/试点发布安全门禁 |
| Add-in 分发 | `docs/Office-Add-in-企业分发指南.md` | Microsoft 365 集中部署与回滚 |
| 发布说明 | `docs/发布说明-0.2.0.md` | 试点候选版验证、限制与回滚 |
| 版权信息 | `docs/版权信息.md` | 版权声明与许可条款 |
| 用户指南 | `docs/用户指南.md` | 本地启动、侧载、调试与排查 |
| 人工干预 | `docs/人工干预操作手册.md` | SSO/云端同步/视频教程操作步骤 |
| CHANGELOG | `docs/CHANGELOG.md` | 版本变更与架构迁移记录 |

## 开发阶段进度

| 阶段 | 状态 | 周期 | 关键交付 |
| --- | --- | --- | --- |
| Phase A：基础设施 | ✅ 完成 | 2周 | Sidecar + Web + Backend 框架 + COM 互操作 + 测试基础 |
| Phase B：核心功能 | ⚠️ 基础版 | 4周 | Power Tools、快捷键（39 个已实现命令默认映射+配置导入/导出）、Visualizations、ModelCheck 活动工作表扫描、Word 模板（3）；全工作簿审计/100+ 快捷键对应命令规格待补 |
| Phase C：高级功能 | ⚠️ 基础版 | 4周 | DocBuilder（3）、PrepareToShare 部分增强、DeckCheck 增强、LinkManager UI、多宿主命令（Excel/PPT/Word）、Backend 命令注册；用户路径和产品化缺口仍在 |
| Phase D：打磨部署 | 🚧 进行中 | 2周 | MSI 安装器可复现构建（83MB, 含 Backend/Sidecar/Web/manifest）、管理员 MSI 安装/卸载回归已通过、COM 集成（22）、HTTP E2E（48）、Office 2024 用户式 E2E、Sidecar API 信封化、Sidecar `/api/*` 可选本地令牌、Sidecar `/api/execute` 审计自动上报、Web UI 显式点击审计、Admin 诊断摘要/诊断包/审计摘要/趋势/热力图/审计保留策略与 Web 诊断面板、Sidecar 服务令牌读取企业词典、DeckCheck Web 查看器（含 Logo 存在/位置检查与品牌化 HTML/PDF 报告基础版）、NamesManager 面板与 Sidecar 路由、链接元数据驱动刷新基础版、Docker 编排验证、Ribbon function-file 静态回归、PPT 形状工具全量 Ribbon 入口、Ribbon 真实侧载已显示并完成初轮点击巡检；剩余：逐命令行为验收归档、企业级 Dashboard/SSO/AIWA 生产接入 |

## MVP 边界

| 纳入 MVP | 暂不纳入 MVP |
| --- | --- |
| Sidecar 基础框架 + 原生 COM 互操作 | 完整 SSO、复杂 RBAC、多租户计费 |
| 命令目录注册 + Win32 全局快捷键基础版 | AIWA 真实大模型生产调用 |
| Power Tools 基础版 | 完整 PowerPoint / Word 插件能力（Sidecar 工具部分就绪，Ribbon/用户入口仍需补齐） |
| Visualizations 与 Model Check 基础版 | 100+ 快捷键对应命令规格与全量产品化覆盖 |
| Prepare to Share 与 Workbook Optimization 基础版 | Brandfetch 等第三方 Logo 数据服务生产集成 |
| Excel → PowerPoint Range 链接原型，Chart 链接 helper | 私有化部署包和完整企业运维平台 |
| 后端健康检查、配置读写、链接元数据、审计事件 | |

## 风险警示

1. **Office 版本兼容性**：原生 COM 需覆盖 Office 2016/2019/365 多版本手工回归；Office PIA 仅作为 Windows-only 预编译后备方案
2. **全局键盘钩子**：WH_KEYBOARD_LL 可能触发杀毒软件警告；需 Code-sign Sidecar + 提供关闭选项
3. **跨应用链接稳定性**：增加链接健康检查，提供自动修复向导
4. **AI 数据安全**：支持私有化部署选项，支持 BYOK
5. **COM 进程隔离**：Sidecar 通过 `oleaut32!GetActiveObject` P/Invoke 连接运行中 Office 实例；WPS/Kingsoft 进程可能抢占 ROT 导致绑定错误 COM 版本（已通过运行时校验 + `scripts/check-office-runtime.ps1` 预检双重防护）；权限不匹配时记录诊断日志

## 样例文件

运行 `.\scripts\generate-samples.ps1` 自动生成测试用 Excel/PowerPoint/Word 样例文件。

## 协作流程

- **代码审查**：所有 PR 必须经过代码审查
- **测试要求**：单元测试覆盖率 > 60%
- **文档同步**：代码变更必须同步更新相关文档
- **提交规范**：遵循 Conventional Commits 规范
- **CI**: GitHub Actions 自动运行 Backend 测试（`dotnet test`）+ Sidecar 测试（`dotnet test`）+ Solution 构建 + Web Add-in 构建（`npm run build`），均在 windows-latest 上执行。Docker Compose 支持本地一键部署（postgres+backend+ollama+nginx）
