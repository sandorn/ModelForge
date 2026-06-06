# ModelForge Sidecar 深度代码审计报告

**审计日期**: 2026-06-05
**审计范围**: `src/sidecar/ModelForge.Sidecar/` (全部 39 个源文件)
**审计人**: Agnes-2.0-Flash (AI Code Audit)

---

## 执行摘要

ModelForge Sidecar 是一个基于 .NET 10 Minimal API 的本地 REST 服务，通过原生 COM P/Invoke 与 Office 应用程序交互。代码整体结构清晰，模块划分合理，但存在 **多个 Critical 级别的安全/稳定性问题**，主要集中在 COM 对象生命周期管理、P/Invoke 资源清理、以及 HTTP 客户端配置上。

| 严重级别    | 数量 | 关键领域                                                  |
| ----------- | ---- | --------------------------------------------------------- |
| 🔴 Critical | 5    | COM 对象泄漏、FinalReleaseComObject 滥用、HTTP 连接池耗尽 |
| 🟠 High     | 12   | 空异常吞没、缺少验证、CORS 配置、COM 引用计数             |
| 🟡 Medium   | 18   | 代码质量、错误处理、可测试性                              |
| 🟢 Low      | 14   | 命名规范、注释、小优化                                    |

---

## 1. 🔴 Critical 问题

### C-01: `OfficeApplicationFactory.Dispose()` 使用 `FinalReleaseComObject` — COM 引用计数灾难

**文件**: `Interop/OfficeApplicationFactory.cs` — 第 57-65 行
**严重级别**: 🔴 Critical

```csharp
// 当前代码 (第 57-65 行)
public void Dispose()
{
    for (int i = _comObjects.Count - 1; i >= 0; i--)
    {
        try
        {
            var obj = _comObjects[i];
            if (obj != null && Marshal.IsComObject(obj))
                Marshal.FinalReleaseComObject(obj);  // ⚠️ 危险
        }
        catch { }
    }
    _comObjects.Clear();
}
```

**问题分析**:

- `FinalReleaseComObject` 会**强制**将 COM 引用计数归零，即使还有其它引用存在。这会导致 Office 应用程序意外崩溃。
- 正确的做法是使用 `ReleaseComObject` 并循环调用直到引用计数为 0，或者更安全的 `SafeRelease` 辅助方法。
- `_comObjects` 列表在 `Track()` 中添加了所有通过 `GetActiveObject` 获取的对象，但这些对象的所有权**不完全属于 Sidecar** — 它们是运行中 Office 实例的引用。Sidecar 不应该释放它们。

**建议修复**:

```csharp
// 方案 A: 完全不释放 GetActiveObject 返回的对象（推荐）
// 因为 GetActiveObject 返回的是运行中 Office 实例的引用，
// Sidecar 不应释放它。移除 _comObjects 跟踪和 Dispose 中的释放逻辑。

// 方案 B: 如果需要安全释放，使用 SafeRelease
public static void SafeRelease<T>(T comObject) where T : class
{
    if (comObject != null && Marshal.IsComObject(comObject))
    {
        while (Marshal.ReleaseComObject(comObject) > 0) { }
    }
}
```

---

### C-02: `ExcelInteropService.Dispose()` 传递释放 `OfficeApplicationFactory`

**文件**: `Interop/ExcelInteropService.cs` — 第 89-92 行
**严重级别**: 🔴 Critical

```csharp
// 当前代码 (第 89-92 行)
public void Dispose()
{
    _factory.Dispose();  // ⚠️ 释放共享的 OfficeApplicationFactory
}
```

**问题分析**:

- `ExcelInteropService` 通过 DI 注入共享的 `OfficeApplicationFactory` 单例。
- `Dispose()` 中调用 `_factory.Dispose()` 会释放**所有** COM 对象引用，包括 PowerPoint 和 Word 的引用。
- 由于 `ExcelInteropService` 是 Singleton，当它被 disposed 时（通常在应用关闭时），会同时销毁所有 Office 实例的连接。
- 更严重的是：`PowerPointInteropService` 和 `WordInteropService` 的 `Dispose()` 是空实现，导致**不一致的释放行为**。

**建议修复**:

```csharp
// ExcelInteropService.Dispose() 不应释放共享的 factory
public void Dispose()
{
    // 仅清理本地缓存的 _excelApp 引用
    if (_excelApp != null && Marshal.IsComObject(_excelApp))
    {
        SafeRelease(_excelApp);
        _excelApp = null;
    }
}
```

---

### C-03: `BackendBridgeClient` 未使用 `IHttpClientFactory` 推荐的 `HttpMessageHandler` 生命周期

**文件**: `Services/BackendBridgeClient.cs` — 全文
**Program.cs** — 第 27-33 行
**严重级别**: 🔴 Critical

```csharp
// Program.cs (第 27-33 行)
builder.Services.AddHttpClient<BackendBridgeClient>(client =>
{
    client.BaseAddress = new Uri(sidecarOptions.BackendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(sidecarOptions.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-Client-Id", "ModelForge.Sidecar");
});
```

**问题分析**:

- `AddHttpClient<T>()` 注册的是 `TypedHttpClient`，其底层 `HttpMessageHandler` (SocketsHttpHandler) 生命周期由 DI 容器管理。
- 在 .NET 中，频繁创建/销毁 `HttpClient` 会导致 **SOCKETS_EXHAUSTED** 问题。虽然 `AddHttpClient` 缓解了这个问题，但 `BackendBridgeClient` 本身是 `sealed class` 且没有实现 `IAsyncDisposable`。
- **关键问题**: `BackendBridgeClient` 的所有方法都使用 `using var request = new HttpRequestMessage(...)` 模式，这是正确的。但 `GetHealthAsync` 和 `DispatchCommandAsync` 返回 `string`，调用方无法区分 HTTP 错误和业务错误。
- `response.EnsureSuccessStatusCode()` 在失败时抛出 `HttpRequestException`，但调用方（如 `KeyboardCommandRouter`）的 catch 块只记录日志，**不传播错误**，导致静默失败。

**建议修复**:

```csharp
// 添加连接超时和 DNS 缓存控制
builder.Services.AddHttpClient<BackendBridgeClient>(client =>
{
    client.BaseAddress = new Uri(sidecarOptions.BackendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(sidecarOptions.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-Client-Id", "ModelForge.Sidecar");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    ConnectTimeout = TimeSpan.FromSeconds(5),
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
});
```

---

### C-04: `GlobalKeyboardHook` 消息循环中 `Thread.Sleep(10)` — 高延迟 + 无优雅退出

**文件**: `Keyboard/GlobalKeyboardHook.cs` — 第 76-85 行
**严重级别**: 🔴 Critical

```csharp
// 当前代码 (第 76-85 行)
while (!ct.IsCancellationRequested)
{
    NativeMethods.MSG msg;
    if (NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, 1))
    {
        NativeMethods.TranslateMessage(ref msg);
        NativeMethods.DispatchMessage(ref msg);
    }
    else
    {
        Thread.Sleep(10);  // ⚠️ 10ms 轮询间隔
    }
}
```

**问题分析**:

- `PeekMessage` 带 `PM_NOREMOVE (1)` 标志时，如果没有消息会**立即返回 false**，导致空转循环 + `Thread.Sleep(10)`。
- 10ms 的轮询间隔意味着快捷键响应延迟至少 10ms（实际可能更高），对于全局快捷键来说体验不佳。
- `ct.IsCancellationRequested` 只在 `PeekMessage` 返回 false 后才检查，意味着取消请求可能需要最多 10ms + 一次 PeekMessage 才能被检测到。
- 更严重的是：`StopAsync` 调用 `_cts.Cancel()` 后等待 `_hookTask` 完成，但如果消息循环因为某些原因卡住（如 Office 无响应），钩子线程可能永远无法退出。

**建议修复**:

```csharp
// 使用 WaitMessage + PeekMessage 组合，或改用 MsgWaitForMultipleObjects
// 方案: 使用 PeekMessage 的 PM_REMOVE 配合 GetMessage 替代轮询
while (!ct.IsCancellationRequested)
{
    NativeMethods.MSG msg;
    // GetMessage 会阻塞直到有消息，无需 Sleep
    if (!NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0))
        break; // WM_QUIT received
    NativeMethods.TranslateMessage(ref msg);
    NativeMethods.DispatchMessage(ref msg);
}
```

---

### C-05: `PrepareToShare` — 副本关闭逻辑缺陷，可能导致文件锁定

**文件**: `Optimization/PrepareToShare.cs` — 第 108-114 行
**严重级别**: 🔴 Critical

```csharp
// 当前代码 (第 108-114 行)
// 5. 保存并关闭副本
safeWb.Save();
safeWb.Close();  // ⚠️ 可能未真正关闭

result.Actions.Add($"已转换 {result.FormulasConverted} 个公式为值");
// ...
finally
{
    // 确保副本已关闭
    try { safeWb.Close(false); } catch { }  // ⚠️ finally 中再次 Close，可能抛出
}
```

**问题分析**:

- `safeWb.Close()` 在 try 块中调用，如果成功执行，`finally` 块中的 `safeWb.Close(false)` 会尝试关闭一个已经关闭的工作簿，可能抛出 COM 异常。
- `Close(false)` 参数为 `false` 表示**不保存更改**，但这与前面的 `Save()` 矛盾。
- 如果 `safeWb.Close()` 成功，`safeWb` 仍然是有效的 COM 对象引用，再次调用 `Close` 会导致不可预测的行为。
- 没有使用 `Marshal.ReleaseComObject` 释放 `safeWb` COM 引用，可能导致 Excel 进程残留。

**建议修复**:

```csharp
try
{
    safeWb.Save();
}
finally
{
    try { safeWb.Close(false); } catch { }
    finally
    {
        if (safeWb != null && Marshal.IsComObject(safeWb))
            Marshal.ReleaseComObject(safeWb);
    }
}
```

---

## 2. 🟠 High 问题

### H-01: 空 `catch` 块吞没所有异常 — 遍布全代码库

**影响文件** (17 处):
| 文件 | 行号 | 代码 |
|------|------|------|
| `Interop/OfficeApplicationFactory.cs` | 62 | `catch { }` |
| `Interop/ExcelInteropService.cs` | 74 | `catch { return null; }` |
| `Formula/FormulaTracers.cs` | 30, 58 | `catch { }` |
| `ModelCheck/*.cs` | 多处 | 所有 Scanner 的 `catch { }` |
| `Optimization/WorkbookOptimizer.cs` | 39, 40, 58, 69, 70 | `catch { }` |
| `Optimization/PrepareToShare.cs` | 多处 | `catch { }` |
| `Visualizations/AuditColorMarker.cs` | 多处 | `catch { }` |
| `PowerPoint/DeckCheck.cs` | 多处 | `catch { }` |
| `PowerPoint/ShapeTools.cs` | 多处 | 无错误处理 |

**问题分析**:

- 空 `catch` 块是**最严重的代码质量问题之一**。它吞没了所有异常（包括 `OutOfMemoryException`、`StackOverflowException`、`ThreadAbortException`），使得调试几乎不可能。
- 在 COM 互操作场景中，COM 调用可能抛出 `InvalidComObjectException`、`COMException`、`TargetInvocationException` 等，全部被静默吞没。

**建议修复**:

```csharp
catch (COMException comEx)
{
    _logger.LogError(comEx, "COM 操作失败: {Operation}", operationName);
    throw; // 或返回错误状态
}
catch (InvalidComObjectException invEx)
{
    _logger.LogWarning(invEx, "COM 对象已被释放: {Operation}", operationName);
    return null;
}
catch (Exception ex)
{
    _logger.LogError(ex, "未预期的异常: {Operation}", operationName);
    throw;
}
```

---

### H-02: `SidecarEndpoints` — 命令执行路由中缺少输入验证

**文件**: `Api/SidecarEndpoints.cs` — 第 78-82 行
**严重级别**: 🟠 High

```csharp
// 当前代码 (第 78-82 行)
app.MapPost("/api/execute", async (
    SidecarExecuteRequest request,
    ExcelInteropService excelService,
    OfficeApplicationFactory factory,
    BackendBridgeClient bridgeClient,
    ILoggerFactory loggerFactory) =>
```

**问题分析**:

- `SidecarExecuteRequest.CommandId` 没有 `[Required]` 验证，空字符串会通过验证。
- `request.Arguments` 可能为 `null`，代码中多处使用 `request.Arguments?.GetValueOrDefault(...)` 是安全的，但第 141 行的 `DcfTemplateInserter.Execute(excel)` 没有传递 `numYears` 参数，使用默认值 5。
- 命令路由是一个巨大的 switch 表达式（约 40 个分支），违反了**单一职责原则**和**开闭原则**。添加新命令需要修改此方法。

**建议修复**:

```csharp
public sealed class SidecarExecuteRequest
{
    [Required]
    public string CommandId { get; set; } = string.Empty;
    public string Host { get; set; } = "excel";
    public Dictionary<string, string>? Arguments { get; set; }
}
```

---

### H-03: CORS 配置过于宽松

**文件**: `Program.cs` — 第 39-47 行
**严重级别**: 🟠 High

```csharp
// 当前代码 (第 39-47 行)
options.AddPolicy("SidecarLocal", policy =>
{
    policy
        .WithOrigins("https://localhost:5173", "http://localhost:5173",
                     "https://localhost:3000", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod();
});
```

**问题分析**:

- `AllowAnyHeader()` + `AllowAnyMethod()` 组合允许任何来源（在列表中）发送任何请求。
- 虽然限制了 origins 列表，但 `POST /api/execute` 端点没有额外的认证或 CSRF 保护。
- 如果本地开发服务器被 XSS 攻击利用，攻击者可以发送任意命令到 Sidecar。

**建议修复**:

```csharp
policy
    .WithOrigins(...)
    .AllowAnyHeader()
    .AllowMethods("GET", "POST")  // 明确限制方法
    .WithHeaders("X-Client-Id"); // 只允许特定自定义头
```

---

### H-04: `GlobalKeyboardHook` — P/Invoke 缺少 `SetLastError` 错误处理

**文件**: `Keyboard/GlobalKeyboardHook.cs` — 第 109-111 行
**严重级别**: 🟠 High

```csharp
// 当前代码 (第 109-111 行)
_hookId = NativeMethods.SetWindowsHookEx(
    WH_KEYBOARD_LL,
    HookCallback,
    IntPtr.Zero,
    0);

if (_hookId == IntPtr.Zero)
{
    _logger.LogWarning("SetWindowsHookEx 失败，键盘钩子未安装");
    return;  // ⚠️ 没有调用 Marshal.GetLastWin32Error()
}
```

**问题分析**:

- `SetWindowsHookEx` 标记了 `SetLastError = true`，但失败时没有调用 `Marshal.GetLastWin32Error()` 获取具体错误码。
- 可能的失败原因包括：`ERROR_HOOK_NEEDS_HMOD (1440)`、`ERROR_INVALID_PARAMETER (87)` 等，缺少错误码使得调试困难。

**建议修复**:

```csharp
if (_hookId == IntPtr.Zero)
{
    var error = Marshal.GetLastWin32Error();
    _logger.LogWarning("SetWindowsHookEx 失败 (错误码: {ErrorCode})，键盘钩子未安装", error);
    return;
}
```

---

### H-05: `ChordParser` — 虚拟键码映射不完整

**文件**: `Keyboard/ChordParser.cs` — 第 82-122 行
**严重级别**: 🟠 High

**问题分析**:

- `MapKeyName` 只映射了 A-Z、0-9、F1-F24 和少量特殊键。
- **缺失的常见键**: `OemPipe (0xDC)`, `OemBackslash`, `OemQuestion`, `OemPeriod`, `OemComma` 等。
- 实际上 `OemPeriod` 和 `OemComma` 已映射，但 `OemPipe`、`OemBackslash`、`OemQuotes` 等键在快捷键中常用却未映射。
- 当用户尝试注册 `Ctrl+Alt+\` 或 `Ctrl+Alt|` 时，`MapKeyName` 返回 `null`，`BuildChord` 返回空字符串，快捷键被静默忽略。

---

### H-06: `BackendBridgeClient` — 命令分发静默失败

**文件**: `Services/BackendBridgeClient.cs` — 第 44-47 行
**文件**: `Keyboard/KeyboardCommandRouter.cs` — 第 34-38 行
**严重级别**: 🟠 High

```csharp
// BackendBridgeClient.DispatchCommandAsync (第 44-47 行)
using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
response.EnsureSuccessStatusCode();  // 失败时抛出 HttpRequestException
return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

// KeyboardCommandRouter.RouteAsync (第 34-38 行)
catch (Exception ex)
{
    _logger.LogError(ex, "命令分发失败: {CommandId}", commandId);
    // ⚠️ 静默失败 — 用户按了快捷键但没有任何反馈
}
```

**问题分析**:

- 快捷键触发后，如果后端不可用，用户不会得到任何反馈（没有 UI 提示，没有声音，没有日志级别高于 Error）。
- 对于全局快捷键来说，**静默失败是严重的用户体验问题**。

---

### H-07: `SidecarEndpoints` — 巨大的命令路由 switch 表达式

**文件**: `Api/SidecarEndpoints.cs` — 第 120-165 行
**严重级别**: 🟠 High

**问题分析**:

- 约 40 个命令分支集中在一个 switch 表达式中，违反了**开闭原则**。
- 添加新命令需要修改此方法，增加测试复杂度。
- 不同主机（Excel/PowerPoint/Word）的命令混在一起，没有清晰的分离。

**建议修复**: 使用策略模式 + 命令处理器注册表:

```csharp
public interface ICommandHandler
{
    string CommandId { get; }
    string Handle(dynamic excelApp, dynamic pptApp, dynamic wordApp, Dictionary<string, string>? args);
}

public class CommandRegistry
{
    private readonly Dictionary<string, ICommandHandler> _handlers = new();
    public void Register(ICommandHandler handler) => _handlers[handler.CommandId] = handler;
    public ICommandHandler? Get(string commandId) => _handlers.GetValueOrDefault(commandId);
}
```

---

### H-08: `appsettings.json` — 缺少敏感配置保护

**文件**: `appsettings.json` — 全文
**严重级别**: 🟠 High

```json
{
  "Sidecar": {
    "BackendBaseUrl": "http://localhost:5095",
    "SidecarPort": 5200,
    "KeyboardHookEnabled": true
  }
}
```

**问题分析**:

- 当前配置不包含敏感信息，但 `CLAUDE.md` 中定义了 `MODELFORGE_AI_API_KEY`、`MODELFORGE_DB_CONNECTION` 等环境变量。
- 这些变量没有在 `appsettings.json` 或 `SidecarOptions` 中映射。
- 如果未来将 API Key 等敏感配置加入 `appsettings.json`，需要确保它被加入 `.gitignore`。

---

### H-09: `PowerPointInteropService` 和 `WordInteropService` 的 `Dispose()` 是空实现

**文件**: `Interop/PowerPointInteropService.cs` — 第 30 行
**文件**: `Interop/WordInteropService.cs` — 第 30 行
**严重级别**: 🟠 High

```csharp
// PowerPointInteropService.cs (第 30 行)
public void Dispose() { }

// WordInteropService.cs (第 30 行)
public void Dispose() { }
```

**问题分析**:

- 与 `ExcelInteropService.Dispose()` 中调用 `_factory.Dispose()` 形成不一致。
- 如果未来这些服务缓存了 COM 对象引用，空 `Dispose()` 会导致泄漏。
- 建议统一 Dispose 模式。

---

### H-10: `FillRight` / `FillDown` — COM 对象逐单元格遍历性能问题

**文件**: `PowerTools/FillRight.cs` — 第 24-30 行
**文件**: `PowerTools/FillDown.cs` — 第 23-29 行
**严重级别**: 🟠 High

```csharp
// FillRight.Execute (第 24-30 行)
for (int r = 1; r <= rows; r++)
{
    dynamic sourceCell = selection.Cells[r, 1];
    dynamic targetRange = selection.Range[selection.Cells[r, 2], selection.Cells[r, cols]];
    sourceCell.AutoFill(targetRange, 0);  // ⚠️ 逐行 COM 调用
}
```

**问题分析**:

- 对于 1000 行的选区，这会触发 1000 次 COM 跨进程调用。
- Excel COM 互操作的每次调用都涉及跨进程封送，1000 次调用可能需要数秒。
- 应该使用 Excel 的 `Range.FillRight` 方法一次性完成。

**建议修复**:

```csharp
// 使用 Excel 内置 FillRight 方法，单次 COM 调用
selection.FillRight();
return $"FillRight 完成：{rows} 行 × {cols} 列。";
```

---

### H-11: `SidecarEndpoints` — 缺少请求速率限制

**文件**: `Api/SidecarEndpoints.cs` — `POST /api/execute`
**严重级别**: 🟠 High

**问题分析**:

- `POST /api/execute` 端点没有速率限制，攻击者（或 buggy 的 Web Add-in）可以无限次发送请求。
- 每次请求可能触发大量的 COM 操作（如 `ModelCheck` 扫描整个工作表），导致 Office 无响应。

**建议修复**: 添加 `Microsoft.AspNetCore.RateLimiting` 中间件:

```csharp
builder.Services.AddRateLimiter(rateLimitOptions =>
{
    rateLimitOptions.AddPolicy("ExecuteRateLimit", context =>
        context.Request.Path.StartsWithSegments("/api/execute")
            ? RateLimitPartition.GetFixedWindowLimiter(
                context.Request.Headers["X-Client-Id"].ToString(),
                fixedWindow =>
                {
                    fixedWindow.PermitLimit = 10;
                    fixedWindow.Window = TimeSpan.FromSeconds(1);
                })
            : null);
});
```

---

### H-12: `Program.cs` — `app.Urls.Add()` 在 `app.Run()` 之前调用但顺序不清晰

**文件**: `Program.cs` — 第 51-53 行
**严重级别**: 🟠 High

```csharp
// 当前代码 (第 51-53 行)
app.MapSidecarEndpoints();

app.Urls.Add($"http://localhost:{sidecarOptions.SidecarPort}");

app.Run();
```

**问题分析**:

- `app.Urls.Add()` 在 `app.Build()` 之后调用，但 `WebApplication.CreateSlimBuilder` 已经创建了默认的 `WebApplicationBuilder`，其 URL 绑定可能已经被初始化。
- 在 .NET 9+ 中，`app.Urls` 在 `app.Run()` 之前修改是有效的，但顺序不直观，容易误导维护者。
- 应该在 `builder` 阶段配置 URL:

```csharp
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(sidecarOptions.SidecarPort);
});
```

---

## 3. 🟡 Medium 问题

### M-01: 所有 Interop 服务使用 `dynamic` — 失去编译时类型检查

**影响文件**: 全部 `Interop/*.cs`、`PowerTools/*.cs`、`Visualizations/*.cs`、`Optimization/*.cs`、`Linking/*.cs`、`PowerPoint/*.cs`、`Word/*.cs`
**严重级别**: 🟡 Medium

**问题分析**:

- 全部使用 `dynamic` 与 Office COM 交互，意味着:
  - 属性/方法名拼写错误只在运行时发现
  - 无法使用 IDE 的智能感知
  - 重构时无法安全重命名 COM 属性
  - 静态分析工具无法检测问题

**建议**: 考虑使用 [Dynamic COM Interop with interfaces](https://learn.microsoft.com/en-us/office/vba/api/overview/using-com-interop) 或 [MicroCom](https://github.com/SimonCropp/MicroCom) 生成静态 COM 接口。

---

### M-02: `CellClassifier.Classify` — 逐单元格遍历性能问题

**文件**: `Visualizations/CellClassifier.cs` — 第 22-42 行
**严重级别**: 🟡 Medium

```csharp
foreach (dynamic cell in range)  // ⚠️ 逐单元格 COM 调用
{
    string addr = cell.Address as string ?? "";
    // ...
}
```

**问题分析**:

- 对于 10,000 个单元格的使用范围，这会触发 10,000 次 COM 调用。
- 应该使用 `Range.Value2` 一次性获取所有值，然后在 .NET 端处理。

---

### M-03: `StatisticsInserter` — 列字母计算重复代码

**文件**: `PowerTools/StatisticsInserter.cs` — 第 68-75 行
**文件**: `PowerTools/DcfTemplateInserter.cs` — 第 108-111 行
**严重级别**: 🟡 Medium

**问题分析**:

- `GetColumnLetter` 方法在两个文件中重复实现，应提取为共享工具方法。

---

### M-04: `IfErrorWrapper` — 公式包裹逻辑可能破坏复杂公式

**文件**: `PowerTools/IfErrorWrapper.cs` — 第 33-45 行
**严重级别**: 🟡 Medium

```csharp
string formulaBody = originalFormula.StartsWith("=")
    ? originalFormula.Substring(1)
    : originalFormula;

cell.Formula = $"=IFERROR({formulaBody},{fallbackValue})";
```

**问题分析**:

- 如果公式包含嵌套的 `=` 符号（如在字符串中），简单的 `StartsWith("=")` 可能不正确。
- 如果公式是数组公式或包含换行符，拼接可能产生无效的 Excel 公式。
- 没有处理 `#REF!` 等已错误公式的情况。

---

### M-05: `ToggleSign` — 公式符号切换逻辑脆弱

**文件**: `PowerTools/ToggleSign.cs` — 第 30-42 行
**严重级别**: 🟡 Medium

```csharp
if (body.StartsWith("-(") && body.EndsWith(")"))
{
    cell.Formula = "=" + body[2..^1];
}
else
{
    cell.Formula = $"=-({body})";
}
```

**问题分析**:

- 如果公式是 `=- (A1+B1)` (有空格)，`StartsWith("-(")` 会失败。
- 如果公式是 `=A1+- (B1+C1)` (内部有负号)，逻辑可能不正确。
- 没有处理文本单元格、空单元格等情况的边界条件。

---

### M-06: `ModelCheckRunner` — 全表扫描性能问题

**文件**: `ModelCheck/ModelCheckRunner.cs` — `ModelCheckRunner.Run` 方法
**严重级别**: 🟡 Medium

**问题分析**:

- `ExternalLinkScanner.Scan` 逐单元格遍历 `usedRange`，对于大型工作表（100,000+ 单元格）性能极差。
- `HardcodedValueScanner.Scan` 使用 `SpecialCells(2)` 是高效的，但 `ExternalLinkScanner` 没有使用类似优化。
- 没有提供进度反馈或取消机制。

---

### M-07: `BackendBridgeClient` — 缺少重试机制

**文件**: `Services/BackendBridgeClient.cs` — 全文
**严重级别**: 🟡 Medium

**问题分析**:

- 所有 HTTP 请求没有重试逻辑。网络抖动或后端短暂不可用会导致命令失败。
- 建议使用 `Polly` 库添加重试策略:

```csharp
builder.Services.AddHttpClient<BackendBridgeClient>(...)
    .AddPolicyHandler(GetRetryPolicy()); // Polly retry
```

---

### M-08: `GlobalKeyboardHook` — 钩子回调中启动异步操作

**文件**: `Keyboard/GlobalKeyboardHook.cs` — 第 107-108 行
**严重级别**: 🟡 Medium

```csharp
_ = _commandRouter.RouteAsync(shortcut.CommandId);  // ⚠️ fire-and-forget
return (IntPtr)1;
```

**问题分析**:

- 键盘钩子回调是同步的，返回非零值会阻止按键继续传递。
- `RouteAsync` 是异步方法，`_ =` 忽略了 `Task`，如果异步操作抛出异常，会导致进程崩溃（在 .NET Core 中，未处理的异步异常默认不终止进程，但日志可能丢失）。
- 钩子回调应该尽快返回，长时间操作应排队处理。

---

### M-09: `SidecarEndpoints` — 健康检查端点过于简单

**文件**: `Api/SidecarEndpoints.cs` — 第 28-33 行
**严重级别**: 🟡 Medium

```csharp
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "ModelForge.Sidecar",
    timestampUtc = DateTime.UtcNow.ToString("o")
}));
```

**问题分析**:

- 健康检查不检查依赖项（后端 API、Excel 连接）。
- 生产环境的健康检查应该检查所有关键依赖。

---

### M-10: `ShortcutRegistry` — 快捷键冲突检测在 `ReplaceAll` 中可能失败

**文件**: `Commands/ShortcutRegistry.cs` — 第 53-59 行
**严重级别**: 🟡 Medium

```csharp
public void ReplaceAll(IEnumerable<ShortcutDefinition> shortcuts)
{
    _shortcutsByChord.Clear();
    foreach (var shortcut in shortcuts)
    {
        Register(shortcut);  // ⚠️ 如果 shortcuts 内部有冲突，会抛出异常
    }
}
```

**问题分析**:

- 如果传入的 `shortcuts` 中有重复的快捷键，`Register` 会抛出 `InvalidOperationException`，导致部分注册成功、部分失败。
- 应该先验证所有快捷键无冲突，再执行注册。

---

### M-11: `Program.cs` — 缺少优雅关闭处理

**文件**: `Program.cs` — 全文
**严重级别**: 🟡 Medium

**问题分析**:

- 没有注册 `ApplicationStopping` 事件来清理 COM 对象和键盘钩子。
- 当应用收到 SIGTERM/SIGINT 时，COM 对象可能不会被正确释放。

---

### M-12: `appsettings.json` — 缺少 `TimeoutSeconds` 配置

**文件**: `appsettings.json` — 全文
**文件**: `Configuration/SidecarOptions.cs` — 第 20-22 行
**严重级别**: 🟡 Medium

```csharp
// SidecarOptions.cs (第 20-22 行)
public int TimeoutSeconds { get; set; } = 10;
```

**问题分析**:

- `SidecarOptions.TimeoutSeconds` 有默认值 10，但 `appsettings.json` 中没有此配置项。
- 如果用户需要自定义超时，必须修改 `appsettings.json`，但文档中没有说明此配置项。

---

### M-13: `BackendBridgeClient` — TraceId 使用 `Guid.NewGuid()` 而非关联请求

**文件**: `Services/BackendBridgeClient.cs` — 多处
**严重级别**: 🟡 Medium

**问题分析**:

- 每个请求生成新的 `Guid.NewGuid()` 作为 `X-Trace-Id`，导致无法将多个相关请求关联到同一个用户操作。
- 应该从请求上下文获取 TraceId（如果是命令分发的响应）或生成一次并在相关请求中复用。

---

### M-14: `PrepareToShare` — 公式转值逻辑冗余

**文件**: `Optimization/PrepareToShare.cs` — 第 58-63 行
**严重级别**: 🟡 Medium

```csharp
foreach (dynamic cell in usedRange)
{
    if (cell.HasFormula)
    {
        cell.Value = cell.Value;  // ⚠️ 这行代码实际上没有转换公式为值
        result.FormulasConverted++;
    }
}
```

**问题分析**:

- `cell.Value = cell.Value` **不会**将公式转换为值。Excel COM 中，要将公式转换为值，需要使用 `cell.Formula = cell.Value` 或 `cell.Copy()` + `cell.PasteSpecial(XlPasteType.xlPasteValues)`。
- 当前代码实际上是一个无操作，`result.FormulasConverted` 计数是错误的。

---

### M-15: `LinkToPowerPoint` — `PasteSpecial` 参数使用命名参数但顺序可能不匹配

**文件**: `Linking/ExcelToPowerPointLinker.cs` — 第 47-49 行
**严重级别**: 🟡 Medium

```csharp
dynamic shape = slide.Shapes.PasteSpecial(
    0,  // ppPasteDefault = 0
    1,  // ppPasteOLEObject = 1
    link: true);  // ⚠️ 命名参数
```

**问题分析**:

- `PasteSpecial` 的完整签名是 `PasteSpecial(DataType, Link, DisplayAsIcon, Placement)`。
- 代码传递了 3 个参数，但使用了命名参数 `link: true`。在 dynamic 调用中，命名参数可能不会被正确解析。
- 应该使用位置参数或明确的参数名。

---

### M-16: `DcfTemplateInserter` — 模板行中的 `[PREV]` 和 `[LAST]` 占位符未处理

**文件**: `PowerTools/DcfTemplateInserter.cs` — 第 44, 47 行
**严重级别**: 🟡 Medium

```csharp
("减: 营运资本变动", "=(B14-B[PREV]14)*B8", false, null),
("企业价值", "=SUM(B27:B[LAST])+B30", true, null),
```

**问题分析**:

- 公式中包含 `[PREV]` 和 `[LAST]` 占位符，但代码中没有处理这些占位符的逻辑。
- 这些占位符会被原样写入 Excel，导致公式错误。

---

### M-17: `ModelForge.Sidecar.csproj` — 引用了未使用的包

**文件**: `ModelForge.Sidecar.csproj` — 第 15 行
**严重级别**: 🟡 Medium

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />
</ItemGroup>
```

**问题分析**:

- `Microsoft.Extensions.Hosting.WindowsServices` 提供 `AddWindowsService()` 扩展方法，用于将应用注册为 Windows 服务。
- `Program.cs` 中没有使用 `AddWindowsService()`，而是直接使用 `WebApplication.CreateSlimBuilder()`。
- 如果计划支持 Windows 服务模式，应在 `Program.cs` 中添加相应支持。

---

### M-18: 缺少结构化日志的日志级别控制

**文件**: `appsettings.json` — 全文
**严重级别**: 🟡 Medium

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "ModelForge": "Debug"
    }
  }
}
```

**问题分析**:

- `ModelForge` 日志级别为 `Debug`，在生产环境中可能产生大量日志。
- 应该区分 `ModelForge` 和 `ModelForge.Interop` 的日志级别。

---

## 4. 🟢 Low 问题

### L-01: `ChordParser.VKey` 中 `OemQuestion` 映射到 `/` 但 vkCode 是 `0xBF`

**文件**: `Keyboard/ChordParser.cs` — 第 40, 118 行
**严重级别**: 🟢 Low

**问题分析**:

- `OemQuestion (0xBF)` 映射到 `/`，但在某些键盘布局中，`0xBF` 可能是 `?` 而非 `/`。
- 建议使用 `ToUpperInvariant()` 统一大小写，或在快捷键匹配时忽略大小写。

---

### L-02: `SidecarEndpoints` 中 `Visualize` 方法的参数命名不一致

**文件**: `Api/SidecarEndpoints.cs` — 第 189 行
**严重级别**: 🟢 Low

```csharp
ExcelCommandIds.VisualizeInputs => Visualize(worksheet: excel.ActiveSheet,
    selection: excel.Selection, mode: "inputs"),
```

**问题分析**:

- 方法调用使用了命名参数，但 `Visualize` 方法定义也使用了命名参数，导致冗余。
- 建议统一使用位置参数。

---

### L-03: `DefaultShortcutMap` 中快捷键 `Ctrl+Alt+1` 可能与 IME 冲突

**文件**: `Commands/DefaultShortcutMap.cs` — 第 26 行
**严重级别**: 🟢 Low

**问题分析**:

- `Ctrl+Alt+1` 在某些 IME（输入法编辑器）中用于切换输入法样式。
- 全局键盘钩子可能无法正确捕获此组合键，因为它被 IME 拦截。

---

### L-04: `AuditColorMarker` 中颜色常量使用十六进制但注释说明 RGB

**文件**: `Visualizations/AuditColorMarker.cs` — 第 12-14 行
**严重级别**: 🟢 Low

```csharp
private const int HardcodedColor = 0x0078D4;   // 蓝色 RGB(0,120,212)
```

**问题分析**:

- `0x0078D4` 实际上是 RGB(0x00, 0x78, 0xD4) = RGB(0, 120, 212)，注释正确。
- 但 Excel COM 中 `Interior.Color` 期望的是 BGR 顺序（Blue-Green-Red），即 `0x0078D4` 会被解释为 RGB(0xD4, 0x78, 0x00)。
- 需要确认颜色是否正确。建议使用 `RGB(red, green, blue)` 函数或 `System.Drawing.Color.ToArgb()`。

---

### L-05: `GlobalKeyboardHook` 中 `KBDLLHOOKSTRUCT` 缺少 `time` 字段的注释

**文件**: `Keyboard/GlobalKeyboardHook.cs` — 第 155-161 行
**严重级别**: 🟢 Low

**问题分析**:

- `MSG` 结构体的 `pt_x` 和 `pt_y` 字段没有使用 `Point` 结构，而是使用 `int`。
- 虽然功能上正确，但使用 `Point` 结构更清晰。

---

### L-06: `BackendBridgeClient` 中 `GetCommandsAsync` 返回 `IReadOnlyList` 但实际返回 `Array.Empty`

**文件**: `Services/BackendBridgeClient.cs` — 第 62-66 行
**严重级别**: 🟢 Low

**问题分析**:

- `Array.Empty<T>()` 返回的是缓存的空数组，每次调用返回相同实例。
- 这是正确的做法，但注释中应该说明。

---

### L-07: `Program.cs` 中 `NoWarn>CS1591` 禁用了缺少 XML 文档的警告

**文件**: `ModelForge.Sidecar.csproj` — 第 11 行
**严重级别**: 🟢 Low

```xml
<NoWarn>$(NoWarn);CS1591</NoWarn>
```

**问题分析**:

- `CS1591` 禁用所有缺少 XML 文档的警告。
- 这导致开发者可能忽略添加 XML 文档，降低代码可维护性。
- 建议仅在开发阶段禁用，生产构建时启用。

---

### L-08: `PowerTools` 中 `FillRight` / `FillDown` 的返回值格式不一致

**文件**: `PowerTools/FillRight.cs` — 第 32 行
**文件**: `PowerTools/FillDown.cs` — 第 31 行
**严重级别**: 🟢 Low

**问题分析**:

- `FillRight` 返回 `"FillRight 完成：{rows} 行 × {cols - 1} 列已从最左列向右填充。"`
- `FillDown` 返回 `"FillDown 完成：{cols} 列 × {rows - 1} 行已从首行向下填充。"`
- 参数顺序不一致（rows × cols vs cols × rows），建议统一。

---

### L-09: `CellClassifier` 中 `ExternalLink` 检测逻辑可能误判

**文件**: `Visualizations/CellClassifier.cs` — 第 35-36 行
**严重级别**: 🟢 Low

```csharp
bool isExternal = formula.Contains('[') || formula.Contains("'[")
    || formula.Contains("!") && formula.Contains(":\\"));
```

**问题分析**:

- `formula.Contains("!") && formula.Contains(":\\"))` 的优先级问题：`&&` 优先级高于 `||`，但这里的括号确保了正确的优先级。
- 然而，`formula.Contains('[')` 可能误判包含 `[` 的本地公式（如命名包含 `[` 的情况，虽然罕见）。

---

### L-10: `SidecarEndpoints` 中 `ClearTracing` 方法使用 try-catch 调用 `ClearArrows`

**文件**: `Api/SidecarEndpoints.cs` — 第 214-216 行
**严重级别**: 🟢 Low

```csharp
private static string ClearTracing(dynamic excel)
{
    dynamic activeSheet = excel.ActiveSheet;
    try { activeSheet.ClearArrows(); } catch { }
    return "追踪箭头已清除。";
}
```

**问题分析**:

- `ClearArrows()` 只清除当前选中的箭头，如果用户没有选中任何箭头，调用会失败。
- 应该先检查是否有箭头存在。

---

### L-11: `Optimization/WorkbookOptimizer` 中 `EstimatedSizeReduction` 字段未设置

**文件**: `Optimization/WorkbookOptimizer.cs` — 第 14 行
**严重级别**: 🟢 Low

```csharp
public long EstimatedSizeReduction { get; set; }
```

**问题分析**:

- `OptimizationResult` 包含 `EstimatedSizeReduction` 属性，但 `Optimize` 方法中从未设置此值。
- 始终返回默认值 0。

---

### L-12: `PowerPoint/DeckCheck` 中 `allowedFonts` 和 `forbiddenTerms` 默认值硬编码

**文件**: `PowerPoint/DeckCheck.cs` — 第 35-36 行
**严重级别**: 🟢 Low

```csharp
allowedFonts ??= new[] { "Arial", "Calibri", "Calibri Light", "Microsoft YaHei" };
forbiddenTerms ??= new[] { "机密", "草案", "DRAFT" };
```

**问题分析**:

- 默认值硬编码在方法中，应该从配置或策略中读取。
- 建议通过 `SidecarOptions` 或独立的 `DeckCheckOptions` 配置。

---

### L-13: `Linking/LinkRefresher` 中 `RefreshPowerPointLinks` 未释放 COM 对象

**文件**: `Linking/LinkRefresher.cs` — 第 24-26 行
**严重级别**: 🟢 Low

```csharp
dynamic? pptApp = Interop.ComRuntime.GetActiveObject(Interop.ComRuntime.CLSID.PowerPoint);
```

**问题分析**:

- `GetActiveObject` 返回的 COM 对象没有被跟踪和释放。
- 虽然 `GetActiveObject` 返回的是运行中实例的引用，但多次调用会累积引用。

---

### L-14: `Formula/FormulaTracers.cs` 中 `CellReference` 类缺少 `Equals` / `GetHashCode`

**文件**: `Formula/FormulaTracers.cs` — 第 68-72 行
**严重级别**: 🟢 Low

```csharp
public sealed class CellReference
{
    public string Address { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Formula { get; set; }
}
```

**问题分析**:

- 如果 `CellReference` 未来需要用于集合操作（如 `Distinct()`、`Contains()`），缺少值语义会导致问题。
- 建议实现 `IEquatable<CellReference>`。

---

## 5. 架构建议

### A-01: 命令处理器架构重构

**当前问题**: `SidecarEndpoints.cs` 中的 switch 表达式约 40 个分支，违反开闭原则。

**建议架构**:

```
Sidecar/
├── Commands/
│   ├── ICommandHandler.cs          # 接口定义
│   ├── Excel/
│   │   ├── FillRightHandler.cs
│   │   ├── FillDownHandler.cs
│   │   └── ...
│   ├── PowerPoint/
│   │   ├── DeckCheckHandler.cs
│   │   └── ...
│   └── CommandRegistry.cs          # 处理器注册表
```

### A-02: COM 对象生命周期管理统一

**当前问题**: COM 对象释放策略不一致，`FinalReleaseComObject` 滥用。

**建议**:

```csharp
public static class ComHelper
{
    public static void SafeRelease(object comObject)
    {
        if (comObject != null && Marshal.IsComObject(comObject))
        {
            while (Marshal.ReleaseComObject(comObject) > 0) { }
        }
    }

    public static T? GetOrCreate<T>(ref T? cache, Func<T> factory) where T : class
    {
        return cache ??= factory();
    }
}
```

### A-03: 添加集成测试框架

**当前问题**: `tests/sidecar/` 目录存在但未见测试文件。

**建议**:

- 使用 `Microsoft.Extensions.Hosting` 的 `Host.CreateDefaultBuilder()` 搭建测试宿主
- 使用 `Moq` 模拟 `ILogger` 和 `HttpClient`
- 使用 `ExcelDna` 或 `Microsoft.Office.Interop.Excel` 的 mockable 包装器进行 COM 操作测试

---

## 6. 优先级修复路线图

| 优先级 | 问题编号            | 估计工时 | 风险                          |
| ------ | ------------------- | -------- | ----------------------------- |
| P0     | C-01, C-02, C-05    | 4h       | 高 — COM 泄漏导致 Office 崩溃 |
| P0     | C-03                | 2h       | 高 — SOCKETS_EXHAUSTED        |
| P1     | H-01 (空 catch)     | 8h       | 高 — 调试困难                 |
| P1     | H-06, H-10          | 4h       | 中 — 用户体验                 |
| P1     | M-14 (公式转值 bug) | 2h       | 中 — 功能不正确               |
| P2     | A-01 (命令重构)     | 16h      | 中 — 架构改进                 |
| P2     | M-02, M-06 (性能)   | 8h       | 低 — 性能优化                 |
| P3     | L-04 (颜色 BGR)     | 1h       | 低 — UI 修正                  |

---

## 7. 总结

ModelForge Sidecar 的代码基础良好，模块划分清晰，DI 配置合理。主要风险集中在 **COM 对象生命周期管理** 和 **P/Invoke 资源清理** 上。建议优先修复 P0 级别问题（特别是 `FinalReleaseComObject` 滥用和 `PrepareToShare` 的文件锁定问题），然后逐步重构命令路由架构。

**最关键的一句话**: 在 COM 互操作中，**每个 `dynamic` 调用都是一次跨进程调用，每个 COM 对象引用都需要被正确释放**。这两点是 Sidecar 稳定性的基石。
