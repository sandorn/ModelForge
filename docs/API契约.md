# ModelForge API 契约

本文档定义 `0.2.0` 后端桥接服务与 Sidecar 的 API 契约，用于连接 Sidecar、Web Add-in、管理员后台和后续自动化/AI 能力。当前目标是统一数据模型、TraceId、命令分发、配置、审计、诊断、术语字典和链接元数据的最小可用接口。

## 1. 基本约定

### 1.1 服务地址

开发环境默认地址：

| 服务        | 地址                    | 说明                                            |
| ----------- | ----------------------- | ----------------------------------------------- |
| Backend API | `http://localhost:5095` | ASP.NET Core 认证、配置、审计、字典、链接元数据 |
| Sidecar API | `http://localhost:5200` | .NET 10 Minimal API，COM 操作与快捷键执行端     |

对应配置：

- 后端：`src/backend/ModelForge.Backend/Properties/launchSettings.json`
- Web Add-in：`src/web/src/services/apiClient.ts` 中默认 `VITE_MODELFORGE_API_URL=http://localhost:5095`
- Sidecar：`src/sidecar/ModelForge.Sidecar/Configuration/SidecarOptions.cs`

### 1.2 统一响应格式

Backend API 返回统一信封；Sidecar `/api/execute`、`/api/status`、`/api/shortcuts/export` 和 `/api/shortcuts/import` 也采用同一信封。Sidecar `/health`、`/api/shortcuts` 仍保留轻量原始 JSON。Backend 业务校验错误已统一返回 `ApiEnvelope<T>`；认证挑战/授权失败等框架级路径可能由 ASP.NET Core 返回空体或标准错误体：

```json
{
  "traceId": "6f4b5b9b9c2d4f0c8e1c8e7d7b3b1f20",
  "data": {},
  "error": null
}
```

字段说明：

| 字段      | 类型          | 必填 | 说明                                                                                                                                                     |
| --------- | ------------- | ---- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `traceId` | `string`      | 是   | 请求追踪 ID，优先沿用请求头 `X-Trace-Id`，否则由后端生成。                                                                                               |
| `data`    | `object/null` | 否   | 成功响应数据。                                                                                                                                           |
| `error`   | `string/null` | 否   | 错误信息。Backend 已实现 `ApiEnvelope<T>`、业务校验错误信封和全局异常中间件；Sidecar `/api/execute` 也已统一返回 `ApiEnvelope<SidecarExecuteResponse>`。 |

### 1.3 TraceId 规则

客户端可传入：

```http
X-Trace-Id: client-generated-id
```

后端会在响应头中返回同名字段：

```http
X-Trace-Id: client-generated-id
```

当前要求：

- Web Add-in 每次请求生成 `crypto.randomUUID()`。
- Sidecar 使用 `Guid.NewGuid().ToString("N")`。
- 后端中间件负责兜底生成 TraceId。

### 1.4 枚举值

当前 C# 契约位于 `src/shared/ModelForge.Contracts/ApiContracts.cs`。`LoginRequest`、`LoginResponse`、`AdminUser*`、`AdminAuditEventsResponse`、`SidecarExecuteRequest`、`SidecarExecuteResponse`、`SidecarStatusResponse`、`ShortcutItem`、`ShortcutImport*`、`ShortcutExportResponse`、`DictionaryTerm`、`DictionaryCheck*`、`DictionaryImport*`、`DictionaryExportResponse` 等跨层模型已集中在 Contracts，Sidecar/Backend 端点文件不再保留重复 DTO。Web 侧 TypeScript 契约位于 `src/web/src/types/contracts.ts`，枚举数值需与 C# 保持一致。

#### OfficeHost

| 名称         |  值 | 说明                    |
| ------------ | --: | ----------------------- |
| `Unknown`    |   0 | 未知宿主。              |
| `Excel`      |   1 | Excel。                 |
| `PowerPoint` |   2 | PowerPoint。            |
| `Word`       |   3 | Word。                  |
| `Web`        |   4 | Web Add-in 或管理页面。 |

#### CommandExecutionTarget

| 名称       |  值 | 说明                 |
| ---------- | --: | -------------------- |
| `Sidecar`  |   0 | 由 Sidecar 执行。    |
| `WebAddIn` |   1 | 由 Web Add-in 执行。 |
| `Backend`  |   2 | 由后端执行。         |

#### CommandStatus

| 名称        |  值 | 说明       |
| ----------- | --: | ---------- |
| `Accepted`  |   0 | 已接收。   |
| `Completed` |   1 | 已完成。   |
| `Failed`    |   2 | 失败。     |
| `Deferred`  |   3 | 延迟执行。 |

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

### 2.2 GET `/api/version`

用于显示后端版本和 API 版本。

响应示例：

```json
{
  "traceId": "trace-id",
  "data": {
    "product": "ModelForge",
    "component": "Backend API",
    "version": "0.2.0",
    "apiVersion": "v1",
    "buildTimestampUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```

## 3. 认证与管理员 API

### 3.1 POST `/api/auth/login`

使用本地账号登录，返回 JWT。当前默认种子账号：`admin/admin123`、`analyst/analyst123`、`auditor/auditor123`。

请求体 (`LoginRequest`)：

```json
{
  "username": "admin",
  "password": "admin123"
}
```

响应体 (`ApiEnvelope<LoginResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "token": "jwt-token",
    "userId": "user-id",
    "username": "admin",
    "role": "Admin",
    "expiresAt": "2026-06-06T18:00:00Z"
  },
  "error": null
}
```

错误：认证失败返回 `401`，响应体为 `ApiEnvelope<object>`，`error="Invalid username or password."`。

### 3.2 GET `/api/auth/me`

返回当前 JWT 用户身份。需要 `Authorization: Bearer <token>`。

### 3.3 GET `/api/admin/users`

管理员查询用户列表（需 Admin 角色 JWT）。

响应体 (`ApiEnvelope<AdminUserResponse[]>`)：

```json
{
  "traceId": "trace-id",
  "data": [
    {
      "id": "user-id",
      "username": "admin",
      "role": "Admin",
      "isActive": true,
      "createdAt": "2026-06-06T10:00:00Z"
    }
  ],
  "error": null
}
```

### 3.4 POST `/api/admin/users`

管理员创建本地用户。当前用于本地/企业后台基础版，生产用户目录仍待接入 SSO/Identity。

请求体 (`AdminUserCreateRequest`)：

```json
{
  "username": "alice",
  "password": "ChangeMe123!",
  "role": "Analyst"
}
```

校验规则：

| 规则                                                                  | 错误状态 |
| --------------------------------------------------------------------- | -------- |
| `username` 必填                                                       | 400      |
| `password` 必填                                                       | 400      |
| `role` 必须为 `Admin` / `Analyst` / `Auditor`，省略时默认为 `Analyst` | 400      |
| `username` 大小写不敏感且不能重复                                     | 409      |

成功响应：`201 Created`，响应体为 `ApiEnvelope<AdminUserResponse>`。

### 3.5 PUT `/api/admin/users/{userId}/toggle`

管理员启用/禁用本地用户。响应中的 `active` 为操作后的真实状态。

响应体 (`ApiEnvelope<AdminUserToggleResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "userId": "user-id",
    "active": false
  },
  "error": null
}
```

用户不存在返回 `404 ApiEnvelope<object>`。

### 3.6 GET `/api/admin/roles`

管理员查询内置 RBAC 角色和权限矩阵（需 Admin 角色 JWT）。当前为只读基础版，用于 Admin Console “角色权限”页；自定义角色、用户组映射和权限编辑仍属于后续生产化任务。

响应体 (`ApiEnvelope<AdminRolesResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "roles": [
      {
        "role": "Admin",
        "permissions": [
          "audit.view",
          "commands.execute",
          "config.write",
          "links.manage",
          "users.manage"
        ],
        "builtIn": true
      },
      {
        "role": "Analyst",
        "permissions": ["aiwa.use", "commands.execute", "links.manage"],
        "builtIn": true
      },
      {
        "role": "Auditor",
        "permissions": ["audit.view", "commands.execute"],
        "builtIn": true
      }
    ]
  },
  "error": null
}
```

### 3.7 GET `/api/admin/audit-events?count=50`

管理员查询最近审计事件（需 Admin 角色 JWT）。`count` 范围会被限制在 `1..500`。该端点支持服务端筛选：`eventType`、`actorId`、`host`、`severity`、`commandId`、`resourceId`、`search`、`sinceUtc`、`untilUtc`、`page`、`pageSize`；当存在筛选条件时，后端会在最近窗口内先过滤再分页，Admin Console 审计页使用同一组参数刷新列表、摘要和 CSV 导出。

响应体 (`ApiEnvelope<AdminAuditEventsResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "items": [
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
    ],
    "pagination": { "page": 1, "pageSize": 50, "total": 1 },
    "query": {
      "count": 50,
      "page": 1,
      "pageSize": 50,
      "eventType": "command.executed",
      "actorId": "user-1",
      "host": 1,
      "severity": 1,
      "search": "fill-down"
    }
  },
  "error": null
}
```

### 3.8 GET `/api/admin/audit-events/export?count=500`

管理员导出最近审计事件 CSV（需 Admin 角色 JWT）。`count` 范围会被限制在 `1..5000`。支持与列表端点相同的筛选参数，便于导出当前 Admin Console 筛选结果。

响应：`text/csv; charset=utf-8`，文件名形如 `modelforge-audit-20260606-120000.csv`。CSV 列为：

```csv
eventId,recordedAtUtc,eventType,actorId,host,severity,commandId,resourceId
```

### 3.9 GET `/api/admin/audit-events/summary?hours=168`

管理员查询最近审计事件统计摘要（需 Admin 角色 JWT）。当前基于最近 `count` 条审计事件聚合，默认窗口为 168 小时；`hours` 限制在 `1..720`，`count` 限制在 `1..5000`。支持与列表端点相同的筛选参数；Admin Console 审计页用同一筛选条件展示事件类型、用户和宿主 Top 10。

响应体 (`ApiEnvelope<AdminAuditSummaryResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "generatedAtUtc": "2026-06-06T14:00:00Z",
    "windowHours": 168,
    "bucketHours": 24,
    "totalEvents": 2,
    "byEventType": [{ "key": "command.executed", "count": 2 }],
    "byHost": [{ "key": "Excel", "count": 2 }],
    "byActor": [{ "key": "admin", "count": 2 }],
    "timeline": [
      {
        "startUtc": "2026-06-06T00:00:00Z",
        "endUtc": "2026-06-07T00:00:00Z",
        "count": 2
      }
    ],
    "heatmap": [
      { "rowKey": "command.executed", "columnKey": "Excel", "count": 2 }
    ],
    "query": { "actorId": "user-1", "host": 1 }
  },
  "error": null
}
```

`bucketHours` 根据窗口自动选择：24 小时内按小时，7 天内按天，更长窗口按周。`timeline` 用于 Admin Console 审计趋势条形图。`heatmap` 为事件类型 × 宿主的基础热力图矩阵，最多取最近窗口内 Top 8 事件类型，用于识别高频功能。

### 3.10 POST `/api/admin/audit-events/retention`

管理员执行审计日志保留策略（需 Admin 角色 JWT）。默认保留天数为 `90`，可通过 `default` 配置 `AuditRetentionDays` 或环境变量 `AuditRetentionDays` 覆盖；请求体传入 `retentionDays` 时优先使用请求值。`retentionDays` 范围限制为 `1..3650`。`dryRun=true` 只返回匹配数量，不删除数据。

请求体：

```json
{
  "retentionDays": 90,
  "dryRun": true
}
```

响应体 (`ApiEnvelope<AdminAuditRetentionResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "retentionDays": 90,
    "cutoffUtc": "2026-03-08T14:00:00Z",
    "matchedEvents": 12,
    "deletedEvents": 0,
    "dryRun": true,
    "executedAtUtc": "2026-06-06T14:00:00Z"
  },
  "error": null
}
```

实际删除（`dryRun=false`）会记录 `admin.audit.retention.pruned` 审计事件，`metadata` 包含保留天数、截止时间、匹配数量和删除数量。当前 InMemory / SQLite / PostgreSQL 审计存储均支持该清理接口。

### 3.11 POST `/api/audit-events` 遥测关闭行为

`/api/audit-events` 会读取 `default` 配置中的 `TelemetryEnabled`：

- `TelemetryEnabled=false`（默认）时，普通 `Information` 级使用统计事件返回 `202 Accepted`，但 `data.recorded=false` 且不会写入审计存储。
- `Warning` 及以上级别、`security.*` / `auth.*` / `admin.*` 事件始终记录，用于保留安全和管理审计。
- `TelemetryEnabled=true` 时，普通信息事件正常写入审计存储。
- Web Add-in 会以 `ui.*` 事件类型 best-effort 上报显式用户点击（导航、命令执行、快捷键配置、Deck Check、链接刷新、Admin 审计/诊断、词典、AIWA 等）。该类事件为 `Information` 级普通使用统计，受 `TelemetryEnabled` 控制；上报失败不阻塞前端交互。

跳过响应示例：

```json
{
  "traceId": "trace-id",
  "data": {
    "eventId": "skipped",
    "recordedAtUtc": "2026-06-06T14:00:00Z",
    "recorded": false,
    "message": "Telemetry is disabled for non-security informational events."
  },
  "error": null
}
```

### 3.12 Backend 管理写操作审计

以下 Backend 写操作会直接写入 `admin.*` 审计事件，不受普通遥测关闭影响：

| 操作                                              | 事件类型                         | resourceId     |
| ------------------------------------------------- | -------------------------------- | -------------- |
| `POST /api/auth/login` 成功                       | `auth.login.succeeded`           | 用户 ID        |
| `POST /api/auth/login` 失败                       | `auth.login.failed`              | 空             |
| `POST /api/admin/users`                           | `admin.user.created`             | 新用户 ID      |
| `PUT /api/admin/users/{userId}/toggle`            | `admin.user.toggled`             | 用户 ID        |
| `PUT /api/config/{scope}`                         | `admin.config.updated`           | 配置 scope     |
| `POST /api/dictionary/`                           | `admin.dictionary.term.upserted` | 术语 ID        |
| `POST /api/dictionary/import`                     | `admin.dictionary.imported`      | 空             |
| `DELETE /api/dictionary/{id}`                     | `admin.dictionary.term.deleted`  | 术语 ID        |
| `POST /api/admin/audit-events/retention` 执行删除 | `admin.audit.retention.pruned`   | `audit-events` |

Admin 操作的 `actorId` 取 JWT `sub`（当前为本地用户 ID），`host=Web`，`severity=Information`。登录成功的 `actorId` 为用户 ID；登录失败的 `actorId` 为尝试登录的用户名，`severity=Warning`。审计写入失败只记录 Backend 日志，不影响原业务响应。

### 3.13 GET `/api/admin/diagnostics`

管理员导出只读诊断摘要（需 Admin 角色 JWT）。该端点用于试点发布排障，配置值会按键名脱敏；不包含 JWT、密码、API Key、工作簿内容或完整日志文件。

响应体 (`ApiEnvelope<AdminDiagnosticsResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "generatedAtUtc": "2026-06-06T14:00:00Z",
    "version": {
      "product": "ModelForge",
      "component": "Backend API",
      "version": "0.2.0",
      "apiVersion": "v1"
    },
    "databaseProvider": "inmemory",
    "databaseConnected": true,
    "commandCount": 42,
    "linkCount": 1,
    "dictionaryTermCount": 10,
    "recentAuditEventCount": 5,
    "auditRetentionDays": 90,
    "auditRetentionCutoffUtc": "2026-03-08T14:00:00Z",
    "auditEventsEligibleForRetentionPrune": 0,
    "configuration": {
      "TelemetryEnabled": "false",
      "DefaultLanguage": "zh-CN",
      "ServiceToken": "[REDACTED]"
    },
    "notes": [
      "Diagnostics intentionally exclude secrets, authentication tokens, and workbook contents."
    ]
  },
  "error": null
}
```

### 3.14 GET `/api/admin/diagnostics/bundle`

管理员下载安全诊断 JSON 包（需 Admin 角色 JWT）。该端点用于试点排障归档，返回 `application/json` 文件，不读取或打包本机日志文件。内容包含诊断摘要、运行时信息和最近审计事件；配置值使用同一脱敏规则。

响应体（文件下载，`AdminDiagnosticsBundleResponse`）：

```json
{
  "generatedAtUtc": "2026-06-06T14:00:00Z",
  "summary": {
    "databaseProvider": "inmemory",
    "databaseConnected": true,
    "commandCount": 42
  },
  "runtime": {
    "frameworkDescription": ".NET 10.0",
    "processArchitecture": "X64"
  },
  "recentAuditEvents": [],
  "notes": [
    "This diagnostics bundle is JSON-only and excludes log files, secrets, authentication tokens, and workbook contents."
  ]
}
```

## 4. 命令目录与命令分发

### 4.1 GET `/api/commands`

返回当前命令目录。当前目录覆盖 Excel / PowerPoint / Word 已实现命令，Web Add-in 命令面板和 Omnibar 直接使用完整命令 ID（如 `excel.fill-down`、`ppt.align-left`、`word.build-cim`）。

响应数据结构：`CommandDefinition[]`

```json
{
  "id": "excel.model-check",
  "displayName": "Model Check",
  "host": 1,
  "target": 0,
  "category": "模型审计",
  "defaultShortcut": "Ctrl+Shift+M",
  "description": "执行模型检查入口。"
}
```

字段说明：

| 字段              | 类型                     | 说明                                       |
| ----------------- | ------------------------ | ------------------------------------------ |
| `id`              | `string`                 | 命令唯一标识，例如 `excel.model-check`。   |
| `displayName`     | `string`                 | 展示名称。                                 |
| `host`            | `OfficeHost`             | 适用宿主：Excel、PowerPoint、Word 或 Web。 |
| `target`          | `CommandExecutionTarget` | 默认执行目标。                             |
| `category`        | `string`                 | 命令分类。                                 |
| `defaultShortcut` | `string/null`            | 默认快捷键。                               |
| `description`     | `string`                 | 命令说明。                                 |

### 4.2 POST `/api/commands/dispatch`

用于 Sidecar Ribbon、快捷键或 Web Add-in 将命令提交给后端桥接层。当前后端仅接收并记录调度状态，真实 Office 执行仍由 Sidecar `/api/execute` 完成。

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
    "message": "命令已由后端桥接接收，目标执行端：Sidecar",
    "acceptedAtUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```

## 5. 配置接口

### 5.1 GET `/api/config/{scope}`

读取指定作用域配置。当前支持 InMemory / SQLite / PostgreSQL Provider，默认 Provider 由部署配置决定。

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

### 5.2 PUT `/api/config/{scope}`

更新指定作用域配置。InMemory 模式重启后丢失；SQLite / PostgreSQL 模式会持久化。

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

## 6. 审计事件接口

### 6.1 POST `/api/audit-events`

记录命令、配置、链接刷新等关键事件。当前支持 InMemory 队列和 SQLite / PostgreSQL 持久化存储。

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

## 7. 链接元数据接口

链接元数据用于记录 Excel 到 PowerPoint/Word 的可刷新关系。当前支持 InMemory / SQLite / PostgreSQL 存储。`refresh` 端点负责记录刷新请求；Sidecar `excel.refresh-links` 会读取 Backend `/api/links`，优先按 LinkMetadata 的 `targetAddress` 精准定位 PowerPoint/Word 目标对象刷新；当后端不可达、无元数据或目标地址不足以定位时，回退本机全量链接刷新。失效链接自动修复仍待补。

### 7.1 GET `/api/links`

返回所有链接元数据。

### 7.2 POST `/api/links`

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

`targetAddress` 推荐格式：

| 目标                      | 格式示例                                | 说明                        |
| ------------------------- | --------------------------------------- | --------------------------- |
| PowerPoint Shape          | `Slide1/Shape3`                         | 第 1 张幻灯片第 3 个形状。  |
| PowerPoint Chart/命名对象 | `Slide2/Chart4`、`Slide3/Revenue Chart` | 支持对象序号或形状名称。    |
| Word Field                | `Field2`                                | 第 2 个 Word 字段链接。     |
| Word InlineShape          | `InlineShape4`                          | 第 4 个内联对象关联字段。   |
| Word Table                | `Table1`                                | 第 1 个表格内首个链接字段。 |

响应状态：`201 Created`

### 7.3 POST `/api/links/{linkId}/refresh`

标记链接刷新请求。该端点不直接操作 Office；用户点击 Sidecar/Web Add-in 的刷新命令时，由 Sidecar 读取 `/api/links` 并执行本机 Office 刷新。

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
    "message": "刷新请求已记录。实际 Office 对象刷新将在 Sidecar 执行端完成。",
    "requestedAtUtc": "2026-06-01T10:00:00+00:00"
  },
  "error": null
}
```

## 8. 企业术语字典

字典服务用于术语合规检查（术语命中检测与自动替换），种子术语涵盖金融行业常用词汇。

### 8.1 GET `/api/dictionary/`

返回所有字典条目。

响应示例：

```json
{
  "traceId": "trace-id",
  "data": [
    {
      "id": "ebitda",
      "term": "EBITDA",
      "replacement": null,
      "regexPattern": "\bebitda\b",
      "category": "Financial",
      "severity": "Info"
    }
  ],
  "error": null
}
```

### 8.2 POST `/api/dictionary/`

新增或更新术语条目。需要 Admin 角色 JWT。`term` 必填；`id` 可省略，省略时由后端生成。缺失 `term` 时返回 `400 ApiEnvelope<object>`。

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

校验规则：`term` 为必填字段（400 BadRequest）。

响应状态：`201 Created`

### 8.3 GET `/api/dictionary/export`

导出完整术语字典。需要 Admin 角色 JWT。当前返回结构化 JSON，供 Admin Console “导出 JSON” 使用。Admin Console 的 CSV/XLSX 模板导出在前端基于同一响应生成，不新增后端二进制端点。

响应体 (`ApiEnvelope<DictionaryExportResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "terms": [
      {
        "id": "ebitda",
        "term": "EBITDA",
        "replacement": null,
        "regexPattern": "\\bebitda\\b",
        "category": "Financial",
        "severity": "Info",
        "updatedAt": "2026-06-06T10:00:00Z"
      }
    ],
    "count": 1,
    "exportedAtUtc": "2026-06-06T10:00:00Z"
  },
  "error": null
}
```

### 8.4 POST `/api/dictionary/import`

批量导入术语。需要 Admin 角色 JWT。支持导入 `DictionaryExportResponse` 中的 `terms` 数组；Admin Console 可将 JSON、CSV 模板或 `.xlsx` 模板解析为同一请求体后提交。`overwrite=false` 时，已有相同 `id` 的术语会跳过。

请求体 (`DictionaryImportRequest`)：

```json
{
  "overwrite": true,
  "terms": [
    {
      "id": "custom-term",
      "term": "自定义术语",
      "replacement": "推荐术语",
      "regexPattern": null,
      "category": "Custom",
      "severity": "Warning"
    }
  ]
}
```

响应体 (`ApiEnvelope<DictionaryImportResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "imported": 1,
    "skipped": 0,
    "errors": [],
    "terms": [
      {
        "id": "custom-term",
        "term": "自定义术语",
        "category": "Custom",
        "severity": "Warning"
      }
    ]
  },
  "error": null
}
```

空 `terms` 返回 `400 ApiEnvelope<object>`。单条术语缺失 `term` 时不会中断整批导入，会计入 `errors[]` 和 `skipped`。

Admin Console CSV/XLSX 模板列：

| 列名           | 必填 | 说明                      |
| -------------- | ---- | ------------------------- |
| `id`           | 否   | 术语 ID，留空时后端生成。 |
| `term`         | 是   | 术语文本。                |
| `replacement`  | 否   | 推荐替换文本。            |
| `regexPattern` | 否   | 可选正则表达式。          |
| `category`     | 否   | 分类，默认 `General`。    |
| `severity`     | 否   | 级别，默认 `Warning`。    |

### 8.5 GET `/api/dictionary/service-export`

供 Sidecar 读取企业词典的服务级导出端点。该端点不使用用户 JWT；仅在 Backend 配置 `ModelForge:ServiceToken` 后启用，并要求请求头 `X-Service-Token` 与配置值完全匹配。未配置或令牌错误时返回 401/403 信封错误。Sidecar `ppt.deck-check` 在未显式传入 `forbiddenTerms` 时会尝试调用该端点，失败时回退到本地默认术语。

请求头：

```http
X-Service-Token: replace-with-random-32-byte-token
```

响应体 (`ApiEnvelope<DictionaryExportResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "terms": [
      {
        "id": "confidential",
        "term": "机密",
        "replacement": null,
        "regexPattern": null,
        "category": "Compliance",
        "severity": "Error",
        "updatedAt": "2026-06-06T10:00:00Z"
      }
    ],
    "count": 1,
    "exportedAtUtc": "2026-06-06T10:00:00Z"
  },
  "error": null
}
```

### 8.6 DELETE `/api/dictionary/{id}`

删除指定术语。需要 Admin 角色 JWT。条目不存在时返回 `404 ApiEnvelope<object>`。

成功时返回 `{"data": {"deleted": "id"}}`；未找到时返回 `404 {"error": "Term 'id' not found."}`。

### 8.7 POST `/api/dictionary/check`

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
      {
        "termId": "draft",
        "term": "DRAFT",
        "matchedText": "DRAFT",
        "position": 10,
        "suggestion": null
      }
    ],
    "matchCount": 3,
    "cleanedText": null
  },
  "error": null
}
```

### 8.8 预置种子术语

| id              | term           | severity | category   |
| --------------- | -------------- | -------- | ---------- |
| `confidential`  | 机密           | Error    | Compliance |
| `draft`         | 草案           | Warning  | Compliance |
| `internal_only` | 内部使用       | Error    | Compliance |
| `tbd`           | 待定           | Info     | Editorial  |
| `ebitda`        | EBITDA         | Info     | Financial  |
| `revenue`       | 收入/Revenue   | Info     | Financial  |
| `npv`           | NPV/净现值     | Info     | Financial  |
| `irr`           | IRR/内部收益率 | Info     | Financial  |
| `pe_ratio`      | P/E Ratio      | Info     | Financial  |

## 9. Sidecar REST API

Sidecar 暴露 localhost REST API (:5200) 供 Web Add-in 任务窗格调用，执行 COM 深度操作。默认本地开发模式不要求令牌；生产/试点可配置 `Sidecar:LocalApiToken` 保护所有 `/api/*` 端点，请求需携带 `X-ModelForge-Sidecar-Token`。`/health` 保持公开，供服务探活使用。

保护端点通用错误：

```json
{
  "traceId": "trace-id",
  "data": null,
  "error": "Valid Sidecar local API token is required."
}
```

### 9.1 GET `/health`

Sidecar 健康检查。

响应示例：

```json
{
  "status": "Healthy",
  "service": "ModelForge.Sidecar",
  "timestampUtc": "2026-06-05T10:00:00+00:00"
}
```

### 9.2 GET `/api/shortcuts`

返回已注册的快捷键列表。该兼容端点保留原始数组格式，供旧客户端读取。

响应示例：

```json
[
  {
    "commandId": "excel.fill-down",
    "displayName": "快速向下填充",
    "shortcut": "Ctrl+Alt+D"
  }
]
```

### 9.3 GET `/api/shortcuts/export`

导出当前 Sidecar 快捷键映射，返回结构化信封。Web Add-in Excel 面板“快捷键配置”使用此端点导出 JSON。

响应体 (`ApiEnvelope<ShortcutExportResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "shortcuts": [
      {
        "commandId": "excel.fill-down",
        "displayName": "快速向下填充",
        "shortcut": "Ctrl+Alt+D"
      }
    ],
    "count": 1,
    "exportedAtUtc": "2026-06-06T10:00:00Z"
  },
  "error": null
}
```

### 9.4 POST `/api/shortcuts/import`

导入并替换当前 Sidecar 快捷键映射。导入前会先验证必填字段和快捷键冲突；校验失败时不会清空现有注册表。

请求体 (`ShortcutImportRequest`)：

```json
{
  "shortcuts": [
    {
      "commandId": "excel.fill-down",
      "displayName": "快速向下填充",
      "shortcut": "Ctrl+Alt+D"
    }
  ]
}
```

响应体 (`ApiEnvelope<ShortcutImportResponse>`)：

```json
{
  "traceId": "trace-id",
  "data": {
    "imported": 1,
    "shortcuts": [
      {
        "commandId": "excel.fill-down",
        "displayName": "快速向下填充",
        "shortcut": "Ctrl+Alt+D"
      }
    ]
  },
  "error": null
}
```

错误：空 `shortcuts` 或重复快捷键返回 `400 ApiEnvelope<object>`。

### 9.5 POST `/api/execute`

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

| 字段        | 类型     | 必填 | 说明                                    |
| ----------- | -------- | ---- | --------------------------------------- |
| `commandId` | `string` | 是   | 命令 ID，如 `excel.model-check`         |
| `host`      | `string` | 是   | 目标宿主：`excel`、`powerpoint`、`word` |
| `arguments` | `object` | 否   | 命令参数（格式类型、年数等）            |

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

| HTTP 状态 | 场景                                                                                                                                             |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| 400       | `commandId` 为空、`host` 值非法（合法值：`excel`、`powerpoint`、`word`）或该宿主不支持该命令 ID                                                  |
| 503       | 目标 Office 应用未运行，或本机 ROT 指向不支持的 WPS/旧 Office COM 兼容对象（需启动 Microsoft Office 2016+/Office 2024 的 Excel/PowerPoint/Word） |
| 500       | COM 操作执行异常（`/api/execute` catch 路径会记录错误日志）                                                                                      |

审计行为：

| 路径          | 事件类型                    | 严重级别      | 说明                                                                                      |
| ------------- | --------------------------- | ------------- | ----------------------------------------------------------------------------------------- |
| 执行成功      | `command.executed`          | `Information` | 上报命令 ID、宿主和 `actorId=local-sidecar`；Backend 按 `TelemetryEnabled` 决定是否落库。 |
| 输入校验失败  | `command.validation_failed` | `Warning`     | 包含校验错误消息；不依赖 Office COM。                                                     |
| Office 未运行 | `command.failed`            | `Warning`     | 目标宿主未启动时记录，不阻塞结构化 503 响应。                                             |
| COM 执行异常  | `command.failed`            | `Error`       | catch 路径记录异常消息。                                                                  |

> **当前实现说明**：`/api/execute` 已在入口处执行 `commandId` 非空验证、`host` 白名单过滤（`excel` / `powerpoint` / `word`）和宿主命令白名单验证。未知命令不再返回“成功 + 未知命令消息”，而是返回 `400 ApiEnvelope<SidecarExecuteResponse>`；错误时 `error` 与 `data.message` 同步描述失败原因。Sidecar 会异步上报 Backend `/api/audit-events`，Backend 按遥测策略决定是否落库；上报失败仅写日志，不影响本机命令响应。

### 9.6 GET `/api/status`

查询当前 Office 连接状态和活动文档信息。`/api/excel/info` 作为兼容别名保留，返回同样的信封结构。

响应体：

```json
{
  "traceId": "trace-id",
  "data": {
    "connected": true,
    "workbook": "financial-model.xlsx",
    "worksheet": "Sheet1",
    "selection": "$A$1:$D$20",
    "version": "16.0"
  },
  "error": null
}
```

当 Excel 未运行或 COM 连接失败时：

```json
{
  "traceId": "trace-id",
  "data": {
    "connected": false,
    "error": "Exception message"
  },
  "error": null
}
```

> **注意**：异常场景会通过 `ILogger` 记录 Warning 日志，并在 `data.error` 中返回简短错误信息。

### 9.7 Word 命令 ID

通过 `POST /api/execute` 调用，`host` 设为 `"word"`：

| commandId                            | 说明                          |
| ------------------------------------ | ----------------------------- |
| `word.build-due-diligence`           | 生成尽调清单模板              |
| `word.build-cim`                     | 生成保密信息备忘录 (CIM) 模板 |
| `word.build-management-presentation` | 生成管理层演示大纲模板        |
| `word.embed-excel-range`             | 嵌入 Excel 区域到 Word        |
| `word.refresh-links`                 | 刷新 Word 文档中的 Excel 链接 |

#### `ppt.deck-check` 参数

`arguments` 支持以下可选项：

| 参数                                     | 说明                                                                                   |
| ---------------------------------------- | -------------------------------------------------------------------------------------- | ------- | ------------------ |
| `allowedFonts`                           | 允许字体，使用竖线分隔，例如 `Arial                                                    | Calibri | Microsoft YaHei`。 |
| `forbiddenTerms`                         | 禁止术语，使用竖线分隔。                                                               |
| `checkLogos`                             | `true` 时检查每页是否存在名称包含 `logo` 或图片类型形状的 Logo。                       |
| `exportPdf`                              | `true` 时导出 Deck Check PDF 报告基础版。                                              |
| `reportPath`                             | 可选 PDF 输出路径；省略时输出到 `%LOCALAPPDATA%\ModelForge\Reports\deck-check-*.pdf`。 |
| `templateName`                           | 可选企业模板名称，写入报告和返回结果。                                                 |
| `reportTitle`                            | 可选报告标题，写入 HTML/PDF 报告和返回结果。                                           |
| `brandPrimaryColor` / `brandAccentColor` | 可选品牌主色/强调色，接受 `#RRGGBB` 或 `RRGGBB`；非法值回退默认色。                    |
| `logoMaxLeft` / `logoMaxTop`             | 可选 Logo 左上角最大偏移阈值（PowerPoint 点数）。                                      |
| `logoMaxWidth` / `logoMaxHeight`         | 可选 Logo 最大宽高阈值（PowerPoint 点数）。                                            |

响应 JSON 的 `result` 字段为序列化后的 `DeckCheckReport`，包含 `SlidesScanned`、`FontIssues`、`TermIssues`、`MissingSlideNumbers`、`DenseTextSlides`、`LogoIssues`、`LogoPositionIssues`、`TemplateName`、`ReportTitle`、`BrandPrimaryColor`、`BrandAccentColor`、`TotalIssues`、`OverallStatus`、`ReportPath` 和 `Issues`。

### 9.8 PowerPoint 命令 ID

通过 `POST /api/execute` 调用，`host` 设为 `"powerpoint"`：

| commandId                   | 说明                                                                                             |
| --------------------------- | ------------------------------------------------------------------------------------------------ |
| `ppt.generate-agenda`       | 自动生成目录幻灯片                                                                               |
| `ppt.deck-check`            | 演示文稿合规审计（字体、术语、编号、文本密度、Logo 存在性/位置检查、品牌化 HTML/PDF 报告基础版） |
| `ppt.align-left`            | 选中形状左对齐                                                                                   |
| `ppt.align-center`          | 选中形状水平居中对齐                                                                             |
| `ppt.align-right`           | 选中形状右对齐                                                                                   |
| `ppt.align-top`             | 选中形状顶端对齐                                                                                 |
| `ppt.align-middle`          | 选中形状垂直居中对齐                                                                             |
| `ppt.align-bottom`          | 选中形状底端对齐                                                                                 |
| `ppt.distribute-horizontal` | 选中形状水平均分                                                                                 |
| `ppt.distribute-vertical`   | 选中形状垂直均分                                                                                 |
| `ppt.unify-size`            | 选中形状统一尺寸                                                                                 |
| `ppt.unify-width`           | 选中形状统一宽度                                                                                 |
| `ppt.unify-height`          | 选中形状统一高度                                                                                 |

## 10. 安全与兼容性边界

- 默认本地开发不上传工作簿内容；企业部署需按环境配置外部数据库和网络访问边界。
- 配置、审计、链接元数据支持 InMemory / SQLite / PostgreSQL；用户身份仍为内存存储。
- Sidecar 与 Web Add-in 通过 localhost REST 通信；Backend 通过命令桥接和元数据服务提供企业能力。
- Sidecar `/api/*` 支持可选本地令牌鉴权（`Sidecar:LocalApiToken` + `X-ModelForge-Sidecar-Token`）；默认空值保持开发兼容。Sidecar `/api/execute` 已添加输入校验、结构化错误响应和关键执行路径审计自动上报。
- 当前接口未实现 OpenAPI 自动生成，后续可引入 Swagger 或 NSwag。

## 11. 后续演进

阶段二建议补充：

1. 进一步标准化 Sidecar `/health`、`/api/shortcuts` 是否也纳入 `ApiEnvelope<T>`。
2. OpenAPI 文档和契约测试。
3. 命令执行器抽象，将 Sidecar/Web/Backend 执行目标拆分为独立适配器。
4. SignalR 或本地轮询机制，用于 Web Add-in 与 Sidecar 的异步状态同步。
5. 本地持久化配置、审计日志完整性保护和敏感信息脱敏。
