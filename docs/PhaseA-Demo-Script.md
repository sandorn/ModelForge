# ModelForge Phase A 演示脚本

> 版本: 0.1.1-stage1 | 日期: 2026-06-05 | 目标: 验证 Sidecar + Backend + Web Add-in 三链路联调

## 前置条件

1. **Backend**: `cd publish\Backend && ModelForge.Backend.exe` (监听 `:5095`)
2. **Sidecar**: `cd publish\Sidecar && ModelForge.Sidecar.exe` (监听 `:5200`)
3. **Web Add-in**: `cd publish\Web && npx serve . -p 5173` 或 sideload manifest
4. **Excel**: 打开一个含数据的 .xlsx 文件

## 演示流程 (约 5 分钟)

### 1. 健康检查 (30s)

```powershell
# Backend
curl http://localhost:5095/health
# → {"status":"Healthy","service":"ModelForge.Backend"...}

# Sidecar
curl http://localhost:5200/health
# → {"status":"Healthy","service":"ModelForge.Sidecar"...}

# Web Add-in
start https://localhost:5173
# → 应显示登录页面，调用 Backend /api/version 成功
```

### 2. 命令目录 (30s)

```powershell
curl http://localhost:5095/api/commands
# → 返回 20 个 Excel 高频命令 (FillDown, ModelCheck, LinkToPowerPoint...)
```

在 Web Add-in 中打开 Omnibar (Ctrl+K)，搜索 "model"，应显示 Model Check 命令。

### 3. Sidecar 快捷键执行 (1 min)

在 Excel 中操作：

| 快捷键 | 预期行为 |
|--------|---------|
| `Ctrl+Alt+D` | 快速向下填充 |
| `Ctrl+Alt+E` | 为选中公式添加 IFERROR 包裹 |
| `Ctrl+Alt+S` | 插入统计摘要 (MIN/MAX/AVERAGE/COUNT/SUM) |
| `Ctrl+Alt+N` | 应用财务格式 (千分位逗号) |
| `Ctrl+Alt+M` | 运行 Model Check → 返回 JSON 报告 |

可通过 Sidecar API 调用验证：

```powershell
curl -X POST http://localhost:5200/api/execute ^
  -H "Content-Type: application/json" ^
  -d '{"commandId":"excel.model-check","host":"excel"}'
# → {"success":true,"result":"{\"ErrorValueCount\":0,...}"...}
```

### 4. 审计颜色标记 (30s)

```powershell
# 标记硬编码输入
curl -X POST http://localhost:5200/api/execute ^
  -H "Content-Type: application/json" ^
  -d '{"commandId":"excel.visualize-inputs","host":"excel"}'

# 标记公式
curl -X POST http://localhost:5200/api/execute ^
  -H "Content-Type: application/json" ^
  -d '{"commandId":"excel.visualize-formulas","host":"excel"}'

# 清除
curl -X POST http://localhost:5200/api/execute ^
  -H "Content-Type: application/json" ^
  -d '{"commandId":"excel.clear-visualizations","host":"excel"}'
```

### 5. Excel → PowerPoint 链接 (1 min)

1. 在 Excel 中选择一个数据区域 (如 `A1:D10`)
2. 打开 PowerPoint（空白演示文稿）
3. 执行链接命令：

```powershell
curl -X POST http://localhost:5200/api/execute ^
  -H "Content-Type: application/json" ^
  -d '{"commandId":"excel.link-to-powerpoint","host":"excel"}'
```

预期：在 PowerPoint 当前幻灯片中插入 Excel Range 的链接副本。

### 6. Backend 治理能力 (1 min)

```powershell
# 配置读写
curl http://localhost:5095/api/config/default
# → {"scope":"default","values":{...}}

curl -X PUT http://localhost:5095/api/config/default ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer <admin-token>" ^
  -d '{"values":{"TelemetryEnabled":"true"},"updatedBy":"demo"}'

# 审计事件
curl -X POST http://localhost:5095/api/audit-events ^
  -H "Content-Type: application/json" ^
  -d '{"eventType":"demo.check","actorId":"presenter","host":1,"severity":1}'
# → {"eventId":"..."}

# 企业字典检查
curl -X POST http://localhost:5095/api/dictionary/check ^
  -H "Content-Type: application/json" ^
  -d '{"text":"This document contains confidential TBD items"}'
# → {"matches":[{"term":"机密"...},{"term":"待定","suggestion":"确定"...}]}
```

### 7. Web Add-in 管理界面 (30s)

在浏览器中访问 `https://localhost:5173`：

1. 登录 (admin / admin123)
2. 查看 Dashboard — 显示 Backend 健康状态和命令数
3. 打开 Admin Console — 查看/新增用户
4. 打开 AIWA Chat — 输入文字测试字典检查

## 验证通过标准

- [ ] Backend `/health` 返回 200
- [ ] Sidecar `/health` 返回 200
- [ ] Web Add-in 页面加载无 JS 错误
- [ ] 至少 3 个快捷键在 Excel 中产生预期效果
- [ ] Model Check 返回非空 JSON 报告
- [ ] Excel → PowerPoint 链接创建成功
- [ ] Backend 审计事件记录成功
- [ ] 字典检查命中并给出替换建议
