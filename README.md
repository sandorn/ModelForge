# ModelForge

ModelForge 是面向投行、私募、风投、FP&A、管理咨询和企业战略团队的专业 Office 生产力插件套件，覆盖 Excel 财务建模、PowerPoint 材料生产、Word 长文档协作、企业品牌合规与安全外发工作流。

## 核心价值

- **Excel 财务建模效率**：快捷键、Power Tools、Visualizations、Model Check、Workbook Optimization、Prepare to Share。
- **跨应用数据联动**：Excel Range / Chart 与 PowerPoint、Word 深度绑定并支持刷新。
- **PowerPoint 专业材料生产**：Dynamic Agendas、Deck Check、MasterShapes、TurboShapes、品牌模板与合规检查。
- **Word 长文档协作**：Doc Builder、目录/书签管理、Link to Excel、表格与样式增强。
- **企业治理能力**：Admin Console、Corporate Dictionary、Shared Libraries、审计日志、Dashboard、策略下发。

## 技术架构

ModelForge 采用 **Hybrid Sidecar** 架构：

| 层级 | 技术选型 | 说明 |
| ---- | -------- | ---- |
| Sidecar | .NET 9 Worker Service + NetOffice (MIT) | 全局键盘钩子、COM 深度操作、Power Tools、Model Check |
| Web Add-in | Office.js + React + TypeScript | Ribbon UI、任务窗格、Admin Console、AIWA 前端 |
| 后端桥接 | ASP.NET Core 9 | 认证、配置、审计、链接元数据、命令桥接、字典管理 |
| 数据库 | PostgreSQL / SQL Server | 存储配置、字典、审计、遥测和链接元数据 |

> Sidecar 与 Web Add-in 通过 localhost REST (:5200) 通信；Sidecar 与后端通过 HTTP (:5095) 通信；所有通信经后端桥接，禁止直接进程通信。

## MVP 方向

当前 Phase A 交付：

1. Sidecar Worker Service 基础框架 + NetOffice COM 互操作层。
2. 20 个高频快捷键（Win32 全局钩子 + Office.js 双轨）。
3. Office Web Add-in Ribbon 与任务窗格。
4. Power Tools 基础版：快速填充、IFERROR 封装、统计公式插入。
5. Visualizations 与 Model Check 基础版。
6. Prepare to Share 与 Workbook Optimization 基础版。
7. Excel → PowerPoint Range / Chart 链接原型。
8. 后端健康检查、配置读写、链接元数据和审计事件。

## 文档导航

| 文档 | 说明 |
| ---- | ---- |
| [CLAUDE.md](CLAUDE.md) | AI Agent 项目规则、架构边界与协作约定 |
| [docs/技术路线规划.md](docs/技术路线规划.md) | 架构方案、ADR 记录、阶段计划 |
| [docs/功能规划.md](docs/功能规划.md) | 产品定位、用户画像、MVP 范围、功能模块与非功能需求 |
| [docs/实现路径规划.md](docs/实现路径规划.md) | 阶段计划、里程碑、门禁、测试、发布与行动清单 |
| [docs/开发环境搭建.md](docs/开发环境搭建.md) | Sidecar + Web + Backend 搭建与运行 |
| [docs/API契约.md](docs/API契约.md) | API 接口契约、DTO 定义 |
| [docs/版权信息.md](docs/版权信息.md) | 版权、商标、专利、安全漏洞报告和投诉通道 |
| [LICENSE](LICENSE) | 专有软件许可证 |

## 许可证

© 2026 ModelForge Team. 保留所有权利。 All Rights Reserved.

本项目采用专有软件许可证，详见 [LICENSE](LICENSE)。
