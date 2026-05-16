# ModelForge - AI Agent 规则手册

## 项目概览

| 属性         | 说明                                           |
| ------------ | ---------------------------------------------- |
| **项目名称** | ModelForge                                     |
| **项目类型** | Office 生产力插件套件                          |
| **技术架构** | VSTO + Office JS API + 后端桥接 (ASP.NET Core) |
| **参考产品** | Macabacus                                      |
| **目标用户** | 投行、私募、风投、FP&A、管理咨询等财务专业人士 |

## 核心架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    Ribbon 功能区 (融合)                           │
├─────────────────────────────┬───────────────────────────────────┤
│       VSTO Commands          │        Web Add-in Commands        │
│    (压力级速度建模)            │     (灵活更新无需安装包)           │
├─────────────────────────────┴───────────────────────────────────┤
│                                                                 │
│  VSTO 负责：                        Web Add-in 负责：             │
│  ├── 快捷键系统 (100+)              ├── Admin Console            │
│  ├── 跨应用数据联动                  ├── Shared Libraries        │
│  ├── 公式追踪 + Model Check         ├── Corporate Dictionary    │
│  ├── 文件优化 + Prepare to Share    ├── Dashboard 统计          │
│  ├── Power Tools                    ├── Omnibar 速查栏           │
│  ├── Visualizations                 ├── AIWA 前端 UI             │
│  ├── 高级图表 Chart Add-ons         └── 企业策略下发            │
│  └── PPT/Word 底层交互                                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 硬边界规则

- **VSTO 职责**：性能敏感功能必须用 VSTO（快捷键、跨应用联动、公式解析）
- **Web Add-in 职责**：灵活更新功能用 Web（管理后台、AIWA、配置管理）
- **通信规范**：VSTO 与 Web Add-in 通过后端 API 桥接通信，禁止直接进程通信
- **文档优先**：新增 API/路由/环境变量必须同步更新 docs/ 相关文档

## 项目结构

```
ModelForge/
├── src/
│   ├── vsto/           # VSTO 插件项目
│   │   ├── ModelForge.Excel/       # Excel VSTO
│   │   ├── ModelForge.PowerPoint/  # PowerPoint VSTO
│   │   └── ModelForge.Word/        # Word VSTO
│   ├── web/            # Web Add-in (React + TypeScript)
│   ├── backend/        # 后端 API 桥接 (ASP.NET Core 8)
│   └── shared/         # 共享类型/常量
├── manifest/           # Add-in Manifest 配置
├── docs/               # 项目文档
│   ├── 功能规划.md      # 功能清单与优先级
│   ├── 实现路径规划.md   # 实施阶段与里程碑
│   └── 版权信息.md       # 版权声明
├── LICENSE             # 专有软件许可证
└── README.md           # 项目说明
```

## 关键路径

| 模块            | 路径                              | 说明               |
| --------------- | --------------------------------- | ------------------ |
| Excel VSTO      | `src/vsto/ModelForge.Excel/`      | 核心财务建模功能   |
| PowerPoint VSTO | `src/vsto/ModelForge.PowerPoint/` | 演示文稿自动化     |
| Word VSTO       | `src/vsto/ModelForge.Word/`       | 文档协作工具       |
| Web Add-in      | `src/web/`                        | 管理后台与 AI 功能 |
| 后端 API        | `src/backend/`                    | 跨插件通信桥接     |

## 命令速查

### 开发环境

```powershell
# 搭建环境
dotnet --version              # 检查 .NET 8
node --version               # 检查 Node.js 20 LTS

# VSTO 开发
dotnet build src/vsto/       # 构建 VSTO 项目
dotnet test src/vsto/        # 运行 VSTO 测试

# Web Add-in 开发
cd src/web
npm install                  # 安装依赖
npm run dev                  # 启动开发服务器
npm run build                # 构建生产版本

# 后端 API
cd src/backend
dotnet run                   # 启动开发服务器
dotnet publish               # 发布生产版本
```

### 部署命令

```powershell
# 打包 VSTO 插件 (WiX Toolset)
msbuild /t:Package /p:Configuration=Release

# 部署 Web Add-in
npm run deploy
```

## 环境变量

| 变量名                     | 说明             | 示例值                                                          |
| -------------------------- | ---------------- | --------------------------------------------------------------- |
| `MODELFORGE_DB_CONNECTION` | 数据库连接字符串 | `Server=localhost;Database=ModelForge;Trusted_Connection=True;` |
| `MODELFORGE_API_URL`       | 后端 API 地址    | `https://api.modelforge.local`                                  |
| `MODELFORGE_AI_API_KEY`    | AI 服务 API Key  | `sk-xxx...`                                                     |
| `MODELFORGE_SSO_ENABLED`   | 是否启用 SSO     | `true/false`                                                    |

## 技术栈

| 层级          | 技术选型                | 版本 |
| ------------- | ----------------------- | ---- |
| VSTO 运行时   | .NET 8                  | 8.0+ |
| Office JS API | Office.js               | 1.1+ |
| 后端框架      | ASP.NET Core            | 8.0+ |
| 前端框架      | React                   | 18+  |
| 前端语言      | TypeScript              | 5.0+ |
| 构建工具      | Vite                    | 6.0+ |
| UI 组件库     | Fluent UI React         | 9.0+ |
| 状态管理      | Zustand                 | 4.0+ |
| 数据库        | PostgreSQL / SQL Server | -    |

## 深入文档指针

| 文档     | 路径                   | 内容                     |
| -------- | ---------------------- | ------------------------ |
| 功能规划 | `docs/功能规划.md`     | 完整功能清单与优先级矩阵 |
| 实现路径 | `docs/实现路径规划.md` | 5个实施阶段与里程碑      |
| 版权信息 | `docs/版权信息.md`     | 版权声明与许可条款       |

## 开发阶段进度

| 阶段                  | 状态      | 周期 | 关键交付                         |
| --------------------- | --------- | ---- | -------------------------------- |
| 阶段一：基础设施      | 📋 待启动 | 2周  | VSTO + Web + 后端框架            |
| 阶段二：Excel VSTO    | 📋 待启动 | 5周  | 快捷键、Power Tools、Model Check |
| 阶段三：Web Add-in    | 📋 待启动 | 3周  | Admin Console、AIWA、Dashboard   |
| 阶段四：PPT/Word VSTO | 📋 待启动 | 4周  | Dynamic Agendas、Deck Check      |
| 阶段五：整合优化      | 📋 待启动 | 3周  | Ribbon融合、企业级功能           |

## 风险警示

1. **Office 版本兼容性**：需覆盖 Office 2016/2019/365，关键功能提供回退方案
2. **快捷键冲突**：内置冲突检测，允许用户自定义映射
3. **跨应用链接稳定性**：增加链接健康检查，提供自动修复向导
4. **AI 数据安全**：支持私有化部署选项（Llama3），支持 BYOK

## 协作流程

- **代码审查**：所有 PR 必须经过代码审查
- **测试要求**：单元测试覆盖率 > 60%
- **文档同步**：代码变更必须同步更新相关文档
- **提交规范**：遵循 Conventional Commits 规范
