# ModelForge API 契约（阶段一）

本文档定义阶段一后端桥接服务的基础 API 契约，用于连接 Sidecar、Web Add-in 与未来的自动化/AI 能力。阶段一目标不是实现完整业务逻辑，而是统一数据模型、TraceId、命令分发、配置、审计和链接元数据的最小可用接口。

## 1. 基本约定

### 1.1 服务地址

开发环境默认地址：

| 服务 | 地址 | 说明 |
|------|------|------|
| Backend API | `http://localhost:5095` | ASP.NET Core 认证、配置、审计、字典、链接元数据 |
| Sidecar API | `http://localhost:5200` | .NET 10 Minimal API，COM 操作与快捷键执行端 |

对应配置：

- 后端：`src/backend/ModelForge.Backend/Properties/launchSettings.json`
- Web Add-in：`src/web/src/services/apiClient.ts` 中默认 `VITE_MODELFORGE_API_URL=http://localhost:5095`
- Sidecar：`src/sidecar/ModelForge.Sidecar/Configuration/SidecarOptions.cs`

### 1.2 统一响应格式

所有 API 返回统一信封：

```json
{
  "traceId": "6f4b5b9b9c2d4f0c8e1c8e7d7b3b1f20",
  "data": {},
  "error": null
}
```

字段说明：

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `traceId` | `string` | 是 | 请求追踪 ID，优先沿用请求头 `X-Trace-Id`，否则由后端生成。 |
| `data` | `object/null` | 否 | 成功响应数据。 |
| `error` | `string/null` | 否 | 错误信息。Sidecar `/api/execute` 已实现结构化错误响应（`ApiEnvelope<SidecarExecuteResponse>`），Backend 已实现全局异常中间件。 |

### 1.3 TraceId 规则

客户端可传入：

```http
X-Trace-Id: client-generated-id
```

后端会在响应头中返回同名字段：

```http
X-Trace-Id: client-generated-id
```

阶段一要求：

- Web Add-in 每次请求生成 `crypto.randomUUID()`。
- Sidecar 使用 `Guid.NewGuid().ToString("N")`。
- 后端中间件负责兜底生成 TraceId。

### 1.4 枚举值

阶段一 C# 契约位于 `src/shared/ModelForge.Contracts/ApiContracts.cs`（含所有跨层 DTO：`SidecarExecuteRequest`、`SidecarExecuteResponse`、`SidecarStatusResponse`）。Sidecar 端点文件不再内嵌 DTO 定义。Web 侧 TypeScript 契约位于 `src/web/src/types/contracts.ts`，枚举数值需与 C# 保持一致。

#### OfficeHost

| 名称 | 值 | 说明 |
| --- | ---: | --- |
| `Unknown` | 0 | 未知宿主。 |
| `Excel` | 1 | Excel。 |
| `PowerPoint` | 2 | PowerPoint。 |
| `Word` | 3 | Word。 |
| `Web` | 4 | Web Add-in 或管理页面。 |

#### CommandExecutionTarget

| 名称 | 值 | 说明 |
| --- | ---: | --- |
| `Sidecar` | 0 | 由 Sidecar 执行。 |
| `WebAddIn` | 1 | 由 Web Add-in 执行。 |
| `Backend` | 2 | 由后端执行。 |

#### CommandStatus

| 名称 | 值 | 说明 |
| --- | ---: | --- |
| `Accepted` | 0 | 已接收。 |
| `Completed` | 1 | 已完成。 |
| `Failed` | 2 | 失败。 |
| `Deferred` | 3 | 延迟执行。 |

## 2. 健康检查与版本


### 2.1 GET `/health` (Enhanced)

健康检查现包含数据库连接状态：

```json
{
  "traceId": "trace-id",
  "data": {
    "status": "Healthy",
    "service": "ModelForge.Backend",
    "timestampUtc": "2026-06-05T10:00:00+00:00",
    "database": { "provider": "postgres", "connected": true }
  },
  "error": null
}
```

### 2.2 GET `/api/admin/audit-events?count=50`

管理员查询最近审计事件（需 Admin 角色 JWT）。

响应示例：

```json
[
  {
    "eventId": "abc123",
    "eventType": "command.executed",
    "actorId": "user-1",
    "host": 1,
    "severity": 1,
    "commandId": "excel.fill-down",
    "resourceId": null,
    "recordedAtUtc": "2026-06-05T10:00:00+00:00"
  }
]
```

### 2.3 GET `/api/version`

用于显示后端版本和 API 版本。

响应示例：

```json
{
  "traceId": "trace-id",
  "data": {
    "product": "ModelForge",
    "component": "Backend API",
    "version": "0.1.0-stage1",
    "apiVersion": "v1",
    "buildTimestampUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```

## 3. 命令目录与命令分发

### 3.1 GET `/api/commands`

返回阶段一 Excel 高频命令目录。阶段一至少包含 20 个 Excel 命令。

响应数据结构：`CommandDefinition[]`

```json
{
  "id": "excel.model-check",
  "displayName": "Model Check",
  "host": 1,
  "target": 0,
  "category": "模型审计",
  "defaultShortcut": "Ctrl+Shift+M",
  "description": "执行阶段一模型检查入口。"
}
```

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | `string` | 命令唯一标识，例如 `excel.model-check`。 |
| `displayName` | `string` | 展示名称。 |
| `host` | `OfficeHost` | 适用宿主。阶段一主要为 `Excel`。 |
| `target` | `CommandExecutionTarget` | 默认执行目标。 |
| `category` | `string` | 命令分类。 |
| `defaultShortcut` | `string/null` | 默认快捷键。 |
| `description` | `string` | 命令说明。 |

### 3.2 POST `/api/commands/dispatch`

用于 Sidecar Ribbon、快捷键或 Web Add-in 将命令提交给后端桥接层。阶段一仅做内存接收和状态返回，后续阶段接入真实执行器。

请求体：

```json
{
  "commandId": "excel.model-check",
  "host": 1,
  "userId": "developer",
  "workbookId": "workbook-local-id",
  "arguments": {
    "selection": "Sheet1!A1:D20"
  }
}
```

响应状态：`202 Accepted`

响应体：

```json
{
  "traceId": "trace-id",
  "data": {
    "dispatchId": "dispatch-guid",
    "commandId": "excel.model-check",
    "status": 0,
    "message": "命令已接收，阶段一以内存方式记录。",
    "acceptedAtUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```

## 4. 配置接口

### 4.1 GET `/api/config/{scope}`

读取指定作用域配置。阶段一默认支持 `default`。

响应示例：

```json
{
  "traceId": "trace-id",
  "data": {
    "scope": "default",
    "values": {
      "TelemetryEnabled": "false",
      "DefaultLanguage": "zh-CN",
      "BackendBridgeMode": "local-development"
    },
    "updatedAtUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```

### 4.2 PUT `/api/config/{scope}`

更新指定作用域配置。阶段一使用内存存储，重启后丢失。

请求体：

```json
{
  "values": {
    "TelemetryEnabled": "false",
    "DefaultLanguage": "zh-CN"
  },
  "updatedBy": "developer"
}
```

## 5. 审计事件接口

### 5.1 POST `/api/audit-events`

记录命令、配置、链接刷新等关键事件。阶段一使用内存队列。

请求体：

```json
{
  "eventType": "command.dispatch.requested",
  "actorId": "developer",
  "host": 1,
  "severity": 1,
  "commandId": "excel.model-check",
  "resourceId": "workbook-local-id",
  "metadata": {
    "source": "Ribbon"
  }
}
```

响应状态：`202 Accepted`

响应体：

```json
{
  "traceId": "trace-id",
  "data": {
    "eventId": "event-guid",
    "recordedAtUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```

## 6. 链接元数据接口

链接元数据用于记录 Excel 到 PowerPoint/Word 的可刷新关系。阶段一只实现内存元数据存储和刷新请求标记。

### 6.1 GET `/api/links`

返回所有链接元数据。

### 6.2 POST `/api/links`

创建链接元数据。

请求体：

```json
{
  "sourceType": 0,
  "sourceDocumentId": "excel-workbook-id",
  "sourceAddress": "Sheet1!A1:D20",
  "targetType": 0,
  "targetDocumentId": "ppt-deck-id",
  "targetAddress": "Slide1/Shape3",
  "refreshPolicy": "manual"
}
```

校验规则：`sourceDocumentId`、`sourceAddress`、`targetDocumentId`、`targetAddress` 均为必填（400 BadRequest）。

响应状态：`201 Created`

### 6.3 POST `/api/links/{linkId}/refresh`

标记链接刷新请求。

请求体：

```json
{
  "requestedBy": "developer"
}
```

响应状态：`202 Accepted`

响应体：

```json
{
  "traceId": "trace-id",
  "data": {
    "linkId": "link-guid",
    "status": 0,
    "message": "链接刷新请求已接收，阶段一暂不执行真实 Office 写回。",
    "requestedAtUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```


## 7. 企业术语字典

字典服务用于术语合规检查（术语命中检测与自动替换），种子术语涵盖金融行业常用词汇。

### 7.1 GET `/api/dictionary/`

返回所有字典条目。

响应示例：

```json
{
  "traceId": "trace-id",
  "data": [
    { "id": "ebitda", "term": "EBITDA", "replacement": null, "regexPattern": "\bebitda\b", "category": "Financial", "severity": "Info" }
  ],
  "error": null
}
```

### 7.2 POST `/api/dictionary/`

新增或更新术语条目。需要 Admin 角色 JWT。

请求体：

```json
{
  "id": "npv",
  "term": "NPV/净现值",
  "replacement": null,
  "regexPattern": "\bnpv\b",
  "category": "Financial",
  "severity": "Info"
}
```

校验规则：`id` 和 `term` 为必填字段（400 BadRequest）。

校验规则：`sourceDocumentId`、`sourceAddress`、`targetDocumentId`、`targetAddress` 均为必填（400 BadRequest）。

响应状态：`201 Created`

### 7.3 DELETE `/api/dictionary/{id}`

删除指定术语。需要 Admin 角色 JWT。

成功时返回 `{"data": {"deleted": "id"}}`；未找到时返回 `404 {"error": "Term 'id' not found."}`。

### 7.4 POST `/api/dictionary/check`

检查文本中的术语命中。

请求体：

```json
{
  "text": "This is a DRAFT confidential document with TBD items.",
  "language": "en"
}
```

响应示例：

```json
{
  "traceId": "trace-id",
  "data": {
    "originalText": "This is a DRAFT...",
    "matches": [
      { "termId": "draft", "term": "DRAFT", "matchedText": "DRAFT", "position": 10, "suggestion": null }
    ],
    "matchCount": 3,
    "cleanedText": null
  },
  "error": null
}
```

### 7.5 预置种子术语

| id | term | severity | category |
|----|------|----------|----------|
| `confidential` | 机密 | Error | Compliance |
| `draft` | 草案 | Warning | Compliance |
| `internal_only` | 内部使用 | Error | Compliance |
| `tbd` | 待定 | Info | Editorial |
| `ebitda` | EBITDA | Info | Financial |
| `revenue` | 收入/Revenue | Info | Financial |
| `npv` | NPV/净现值 | Info | Financial |
| `irr` | IRR/内部收益率 | Info | Financial |
| `pe_ratio` | P/E Ratio | Info | Financial |


## 8. Sidecar REST API

Sidecar 暴露 localhost REST API (:5200) 供 Web Add-in 任务窗格调用，执行 COM 深度操作。

### 8.1 GET `/health`

Sidecar 健康检查。

响应示例：

```json
{
  "status": "Healthy",
  "service": "ModelForge.Sidecar",
  "timestampUtc": "2026-06-05T10:00:00+00:00"
}
```

### 8.2 GET `/api/shortcuts`

返回已注册的快捷键列表。

响应示例：

```json
[
  { "commandId": "excel.fill-down", "displayName": "快速向下填充", "shortcut": "Ctrl+Alt+D" }
]
```

### 8.3 POST `/api/execute`

核心命令执行端点。支持 Excel/PowerPoint/Word 命令路由。

请求体 (`SidecarExecuteRequest`)：

```json
{
  "commandId": "excel.model-check",
  "host": "excel",
  "arguments": { "selection": "Sheet1!A1:D20" }
}
```

字段说明：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `commandId` | `string` | 是 | 命令 ID，如 `excel.model-check` |
| `host` | `string` | 是 | 目标宿主：`excel`、`powerpoint`、`word` |
| `arguments` | `object` | 否 | 命令参数（格式类型、年数等） |

响应体 (`ApiEnvelope<SidecarExecuteResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "success": true,
    "commandId": "excel.model-check",
    "message": "Command executed successfully.",
    "result": "{...}"
  },
  "error": null
}
```

错误场景：

| HTTP 状态 | 场景 |
|-----------|------|
| 400 | `commandId` 为空或 `host` 值非法（合法值：`excel`、`powerpoint`、`word`） |
| 503 | 目标 Office 应用未运行（需先启动 Excel/PowerPoint/Word） |
| 500 | COM 操作执行异常（已记录完整异常日志） |

> **新增**：`/api/execute` 端点已在入口处执行输入校验——`commandId` 非空验证和 `host` 白名单过滤（`HashSet<string>(StringComparer.OrdinalIgnoreCase)`），无效请求立即返回 400 BadRequest。

### 8.4 GET `/api/excel/info`

查询当前 Excel 连接状态和活动文档信息。这是实际的 Sidecar 端点路径。

响应体：

```json
{
  "connected": true,
  "workbook": "financial-model.xlsx",
  "worksheet": "Sheet1",
  "selection": "$A$1:$D$20",
  "version": "16.0"
}
```

当 Excel 未运行或 COM 连接失败时：

```json
{
  "connected": false,
  "error": "Exception message"
}
```

> **注意**：异常场景（`connected: false`）现在会通过 `ILogger` 记录 Warning 级别日志，不再静默吞没。


### 8.5 Word 命令 ID

通过 `POST /api/execute` 调用，`host` 设为 `"word"`：

| commandId | 说明 |
|-----------|------|
| `word.build-due-diligence` | 生成尽调清单模板 |
| `word.build-cim` | 生成保密信息备忘录 (CIM) 模板 |
| `word.build-management-presentation` | 生成管理层演示大纲模板 |
| `word.embed-excel-range` | 嵌入 Excel 区域到 Word |
| `word.refresh-links` | 刷新 Word 文档中的 Excel 链接 |


### 8.6 PowerPoint 命令 ID

通过 `POST /api/execute` 调用，`host` 设为 `"powerpoint"`：

| commandId | 说明 |
|-----------|------|
| `ppt.generate-agenda` | 自动生成目录幻灯片 |
| `ppt.deck-check` | 演示文稿合规审计（字体、术语、编号、文本密度） |
| `ppt.align-left` | 选中形状左对齐 |
| `ppt.distribute-horizontal` | 选中形状水平均分 |
| `ppt.unify-size` | 选中形状统一尺寸 |

## 9. 安全与兼容性边界

- 阶段一不接入外部网络服务，不上传工作簿内容。
- 配置、审计、链接元数据均为内存实现，便于验证 API 形态。
- Sidecar 与 Web Add-in 不直接通信，必须通过后端桥接或 localhost REST。
- 当前接口未启用鉴权，后续阶段将增加本地令牌、租户隔离和企业策略。Sidecar `/api/execute` 端点已添加结构化错误响应和输入校验。
- 当前接口未实现 OpenAPI 自动生成，后续可引入 Swagger 或 NSwag。

## 10. 后续演进

阶段二建议补充：

1. 全局异常处理中间件进一步标准化。Sidecar `/api/execute` 已采用结构化错误响应。
2. OpenAPI 文档和契约测试。
3. 命令执行器抽象，将 Sidecar/Web/Backend 执行目标拆分为独立适配器。
4. SignalR 或本地轮询机制，用于 Web Add-in 与 Sidecar 的异步状态同步。
5. 本地持久化配置、审计日志落盘和敏感信息脱敏。