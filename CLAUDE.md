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
- **历史代码**：`src/vsto/` 目录为历史参考，不参与编译

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
│   └── vsto/                   # 历史参考（不参与编译）
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
| 历史参考        | `src/vsto/ModelForge.Excel/`      | 不参与编译 |
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
| `MODELFORGE_API_URL`       | 后端 API 地址    | `https://api.modelforge.local`                                  |
| `MODELFORGE_SIDECAR_URL`   | Sidecar 地址     | `http://localhost:5200`                                         |
| `MODELFORGE_AI_API_KEY`    | AI 服务 API Key  | `sk-xxx...`                                                     |
| `MODELFORGE_SSO_ENABLED`   | 是否启用 SSO     | `true/false`                                                    |
| `MODELFORGE_KEYBOARD_HOOK_ENABLED` | 启用键盘钩子 | `true/false`                                               |

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
| 数据库        | PostgreSQL / SQL Server | -    |

## 深入文档指针

| 文档         | 路径                       | 内容                     |
| ------------ | -------------------------- | ------------------------ |
| 技术路线规划 | `docs/技术路线规划.md`     | 🆕 2026 重构架构方案    |
| 功能规划     | `docs/功能规划.md`         | 完整功能清单与优先级矩阵 |
| 实现路径     | `docs/实现路径规划.md`     | 5 个实施阶段与里程碑    |
| 开发环境     | `docs/开发环境搭建.md`     | Sidecar + Web + Backend 搭建 |
| 版权信息     | `docs/版权信息.md`         | 版权声明与许可条款       |

## 开发阶段进度

| 阶段                  | 状态      | 周期 | 关键交付                              |
| --------------------- | --------- | ---- | ------------------------------------- |
| Phase A：基础设施     | 🔄 进行中  | 2周  | Sidecar + Web + 后端框架 + COM 互操作  |
| Phase B：核心功能     | 📋 待启动 | 4周  | Power Tools、快捷键、Visualizations   |
| Phase C：高级功能     | 📋 待启动 | 4周  | 跨应用链接、公式追踪、Prepare to Share |
| Phase D：打磨部署     | 📋 待启动 | 2周  | MSI 安装器、E2E 测试、文档             |

## MVP 边界

| 纳入 MVP | 暂不纳入 MVP |
| -------- | ------------ |
| Sidecar 基础框架 + 原生 COM 互操作      | 完整 SSO、复杂 RBAC、多租户计费 |
| 20 个以内高频快捷键（Win32 全局钩子） | AIWA 真实大模型生产调用 |
| Power Tools 基础版 | 完整 PowerPoint / Word 插件能力 |
| Visualizations 与 Model Check 基础版 | 100+ 快捷键全量覆盖 |
| Prepare to Share 与 Workbook Optimization 基础版 | Brandfetch 等第三方 Logo 数据服务生产集成 |
| Excel → PowerPoint Range / Chart 链接原型 | 私有化部署包和完整企业运维平台 |
| 后端健康检查、配置读写、链接元数据、审计事件 |  |

## 风险警示

1. **Office 版本兼容性**：原生 COM 需覆盖 Office 2016/2019/365 多版本手工回归；Office PIA 仅作为 Windows-only 预编译后备方案
2. **全局键盘钩子**：WH_KEYBOARD_LL 可能触发杀毒软件警告；需 Code-sign Sidecar + 提供关闭选项
3. **跨应用链接稳定性**：增加链接健康检查，提供自动修复向导
4. **AI 数据安全**：支持私有化部署选项，支持 BYOK
5. **COM 进程隔离**：Sidecar 通过 `Marshal.GetActiveObject()` 连接运行中 Office 实例；权限不匹配时记录诊断日志

## 协作流程

- **代码审查**：所有 PR 必须经过代码审查
- **测试要求**：单元测试覆盖率 > 60%
- **文档同步**：代码变更必须同步更新相关文档
- **提交规范**：遵循 Conventional Commits 规范
