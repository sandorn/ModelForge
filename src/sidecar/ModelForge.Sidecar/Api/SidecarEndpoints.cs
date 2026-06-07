using ModelForge.Contracts;
using ModelForge.Sidecar.Commands;
using ModelForge.Sidecar.Formula;
using ModelForge.Sidecar.Interop;
using ModelForge.Sidecar.Linking;
using ModelForge.Sidecar.ModelCheck;
using ModelForge.Sidecar.Optimization;
using ModelForge.Sidecar.PowerPoint;
using ModelForge.Sidecar.PowerTools;
using ModelForge.Sidecar.Services;
using ModelForge.Sidecar.Visualizations;
using ModelForge.Sidecar.Word;

namespace ModelForge.Sidecar.Api;

/// <summary>
/// Sidecar localhost REST API 端点映射。
/// 供 Office Web Add-in 任务窗格和 function-file 调用。
/// </summary>
public static class SidecarEndpoints
{
    private static readonly HashSet<string> KnownPowerPointCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        PptCommandIds.GenerateAgenda,
        PptCommandIds.DeckCheck,
        PptCommandIds.AlignLeft,
        PptCommandIds.AlignCenter,
        PptCommandIds.AlignRight,
        PptCommandIds.AlignTop,
        PptCommandIds.AlignMiddle,
        PptCommandIds.AlignBottom,
        PptCommandIds.DistributeHorizontal,
        PptCommandIds.DistributeVertical,
        PptCommandIds.UnifyWidth,
        PptCommandIds.UnifyHeight,
        PptCommandIds.UnifySize
    };

    private static readonly HashSet<string> KnownWordCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        WordCommandIds.BuildDueDiligence,
        WordCommandIds.BuildCim,
        WordCommandIds.BuildManagementPresentation,
        WordCommandIds.EmbedExcelRange,
        WordCommandIds.RefreshLinks
    };

    private static readonly HashSet<string> KnownExcelCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        ExcelCommandIds.FillRight,
        ExcelCommandIds.FillDown,
        ExcelCommandIds.WrapIfError,
        ExcelCommandIds.InsertStatistics,
        ExcelCommandIds.VisualizeInputs,
        ExcelCommandIds.VisualizeFormulas,
        ExcelCommandIds.VisualizeLinks,
        ExcelCommandIds.ClearVisualizations,
        ExcelCommandIds.ModelCheck,
        ExcelCommandIds.TracePrecedents,
        ExcelCommandIds.TraceDependents,
        ExcelCommandIds.ClearTrace,
        ExcelCommandIds.OptimizeWorkbook,
        ExcelCommandIds.PrepareToShare,
        ExcelCommandIds.ApplyFinanceFormat,
        ExcelCommandIds.ToggleSign,
        ExcelCommandIds.InsertDcfTemplate,
        ExcelCommandIds.LinkToPowerPoint,
        ExcelCommandIds.RefreshLinks,
        ExcelCommandIds.NamesManager,
        ExcelCommandIds.OpenTaskPane
    };

    public static void MapSidecarEndpoints(this WebApplication app)
    {
        // GET /health
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy",
            service = "ModelForge.Sidecar",
            timestampUtc = DateTime.UtcNow.ToString("o")
        }));

        // GET /api/shortcuts
        app.MapGet("/api/shortcuts", (ShortcutRegistry registry) =>
        {
            return Results.Ok(GetShortcutItems(registry));
        });

        // GET /api/shortcuts/export
        app.MapGet("/api/shortcuts/export", (HttpContext context, ShortcutRegistry registry) =>
        {
            return Results.Ok(ApiEnvelope<ShortcutExportResponse>.Success(
                new ShortcutExportResponse { Shortcuts = GetShortcutItems(registry) },
                GetTraceId(context)));
        });

        // POST /api/shortcuts/import
        app.MapPost("/api/shortcuts/import", (HttpContext context, ShortcutImportRequest request, ShortcutRegistry registry) =>
        {
            var traceId = GetTraceId(context);
            if (request.Shortcuts.Count == 0)
            {
                return Results.BadRequest(ApiEnvelope<object>.Failure("shortcuts is required.", traceId));
            }

            try
            {
                registry.ReplaceAll(request.Shortcuts.Select(item =>
                    new ShortcutDefinition(item.CommandId, item.DisplayName, item.Shortcut)));
                var shortcuts = GetShortcutItems(registry);
                return Results.Ok(ApiEnvelope<ShortcutImportResponse>.Success(
                    new ShortcutImportResponse
                    {
                        Imported = shortcuts.Count,
                        Shortcuts = shortcuts
                    },
                    traceId));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ApiEnvelope<object>.Failure(ex.Message, traceId));
            }
        });

        // POST /api/execute — 核心命令路由
        app.MapPost("/api/execute", async (
            HttpContext context,
            SidecarExecuteRequest request,
            ExcelInteropService excelService,
            OfficeApplicationFactory factory,
            BackendBridgeClient bridgeClient,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Sidecar.Execute");
            var traceId = GetTraceId(context);
            if (string.IsNullOrWhiteSpace(request.CommandId))
            {
                var error = "commandId is required and must not be empty.";
                ReportExecuteAudit(
                    bridgeClient,
                    logger,
                    request,
                    OfficeHost.Excel,
                    "command.validation_failed",
                    AuditSeverity.Warning,
                    error);
                return ExecuteFailure(error, traceId, StatusCodes.Status400BadRequest);
            }

            var host = string.IsNullOrWhiteSpace(request.Host) ? "excel" : request.Host.Trim().ToLowerInvariant();
            if (host is not ("excel" or "powerpoint" or "word"))
            {
                var error = $"Invalid host '{request.Host}'. Valid values: excel, powerpoint, word.";
                ReportExecuteAudit(
                    bridgeClient,
                    logger,
                    request,
                    OfficeHost.Excel,
                    "command.validation_failed",
                    AuditSeverity.Warning,
                    error);
                return ExecuteFailure(error, traceId, StatusCodes.Status400BadRequest);
            }
            var reportHost = ToOfficeHost(host);
            var commandValidationError = ValidateCommandForHost(host, request.CommandId);
            if (commandValidationError != null)
            {
                ReportExecuteAudit(
                    bridgeClient,
                    logger,
                    request,
                    reportHost,
                    "command.validation_failed",
                    AuditSeverity.Warning,
                    commandValidationError);
                return ExecuteFailure(commandValidationError, traceId, StatusCodes.Status400BadRequest);
            }

            logger.LogInformation("执行命令: {CommandId}", request.CommandId);

            try
            {
                string resultMessage;

                // ── PowerPoint Commands ──
                if (host == "powerpoint")
                {
                    var ppt = factory.GetPowerPoint();
                    if (ppt == null)
                    {
                        var error = "PowerPoint 未运行。请先启动 PowerPoint。";
                        ReportExecuteAudit(
                            bridgeClient,
                            logger,
                            request,
                            reportHost,
                            "command.failed",
                            AuditSeverity.Warning,
                            error);
                        return ExecuteFailure(error, traceId, StatusCodes.Status503ServiceUnavailable);
                    }

                    resultMessage = request.CommandId switch
                    {
                        PptCommandIds.GenerateAgenda => System.Text.Json.JsonSerializer.Serialize(
                            DynamicAgendas.Generate(ppt, logger)),
                        PptCommandIds.DeckCheck => System.Text.Json.JsonSerializer.Serialize(
                            await RunDeckCheckAsync(
                                ppt,
                                request.Arguments ?? new Dictionary<string, string>(),
                                bridgeClient,
                                logger)),
                        PptCommandIds.AlignLeft => ShapeTools.AlignLeft(ppt),
                        PptCommandIds.AlignCenter => ShapeTools.AlignCenter(ppt),
                        PptCommandIds.AlignRight => ShapeTools.AlignRight(ppt),
                        PptCommandIds.AlignTop => ShapeTools.AlignTop(ppt),
                        PptCommandIds.AlignMiddle => ShapeTools.AlignMiddle(ppt),
                        PptCommandIds.AlignBottom => ShapeTools.AlignBottom(ppt),
                        PptCommandIds.DistributeHorizontal => ShapeTools.DistributeHorizontal(ppt),
                        PptCommandIds.DistributeVertical => ShapeTools.DistributeVertical(ppt),
                        PptCommandIds.UnifyWidth => ShapeTools.UnifyWidth(ppt),
                        PptCommandIds.UnifyHeight => ShapeTools.UnifyHeight(ppt),
                        PptCommandIds.UnifySize => ShapeTools.UnifySize(ppt),
                        _ => throw new InvalidOperationException($"不支持的 PowerPoint 命令: {request.CommandId}")
                    };
                }
                // ── Word Commands ──
                else if (host == "word")
                {
                    var word = factory.GetWord();
                    if (word == null)
                    {
                        var error = "Word 未运行。请先启动 Word。";
                        ReportExecuteAudit(
                            bridgeClient,
                            logger,
                            request,
                            reportHost,
                            "command.failed",
                            AuditSeverity.Warning,
                            error);
                        return ExecuteFailure(error, traceId, StatusCodes.Status503ServiceUnavailable);
                    }

                    dynamic? wordDoc = null;
                    try { wordDoc = word.ActiveDocument; } catch { }
                    if (wordDoc == null)
                    {
                        wordDoc = word.Documents.Add();
                    }

                    var companyName = request.Arguments?.GetValueOrDefault("companyName", "") ?? "";
                    resultMessage = request.CommandId switch
                    {
                        WordCommandIds.BuildDueDiligence =>
                            DocBuilder.Build(word, DocBuilder.CreateDueDiligenceTemplate(companyName)),
                        WordCommandIds.BuildCim =>
                            DocBuilder.Build(word, DocBuilder.CreateCimTemplate(companyName)),
                        WordCommandIds.BuildManagementPresentation =>
                            DocBuilder.Build(word, DocBuilder.CreateManagementPresentationTemplate(companyName)),
                        WordCommandIds.EmbedExcelRange =>
                            LinkToExcel.EmbedExcelRange(excelService.GetApplication(), word),
                        WordCommandIds.RefreshLinks =>
                            LinkToExcel.RefreshLinks(word),
                        _ => throw new InvalidOperationException($"不支持的 Word 命令: {request.CommandId}")
                    };
                }
                // Excel Commands (default)
                else
                {
                    var excel = excelService.GetApplication();
                    if (excel == null)
                    {
                        var error = "Excel 未运行。请先启动 Excel 再执行命令。";
                        ReportExecuteAudit(
                            bridgeClient,
                            logger,
                            request,
                            reportHost,
                            "command.failed",
                            AuditSeverity.Warning,
                            error);
                        return ExecuteFailure(error, traceId, StatusCodes.Status503ServiceUnavailable);
                    }

                    resultMessage = request.CommandId switch
                {
                    // === Power Tools ===
                    ExcelCommandIds.FillRight => FillRight.Execute(excel),
                    ExcelCommandIds.FillDown => FillDown.Execute(excel),
                    ExcelCommandIds.WrapIfError => IfErrorWrapper.Execute(excel,
                        request.Arguments?.GetValueOrDefault("fallback", "0") ?? "0"),
                    ExcelCommandIds.InsertStatistics => StatisticsInserter.Execute(excel),

                    // === Visualizations ===
                    ExcelCommandIds.VisualizeInputs => Visualize(worksheet: excel.ActiveSheet,
                        selection: excel.Selection, mode: "inputs"),
                    ExcelCommandIds.VisualizeFormulas => Visualize(worksheet: excel.ActiveSheet,
                        selection: excel.Selection, mode: "formulas"),
                    ExcelCommandIds.VisualizeLinks => Visualize(worksheet: excel.ActiveSheet,
                        selection: excel.Selection, mode: "links"),
                    ExcelCommandIds.ClearVisualizations => ClearVisualizations(excel.ActiveSheet, excel.Selection),

                    // === Model Check ===
                    ExcelCommandIds.ModelCheck => RunModelCheck(excel),

                    // === Formula Tracing ===
                    ExcelCommandIds.TracePrecedents => TracePrecedents(excel),
                    ExcelCommandIds.TraceDependents => TraceDependents(excel),
                    ExcelCommandIds.ClearTrace => ClearTracing(excel),

                    // === Workbook Tools ===
                    ExcelCommandIds.OptimizeWorkbook => OptimizeWorkbook(excel),
                    ExcelCommandIds.PrepareToShare => PrepareShare(excel, request.Arguments),
                    ExcelCommandIds.NamesManager => RunNamesManager(excel, request.Arguments),

                    // === Cross-App Linking ===
                    ExcelCommandIds.LinkToPowerPoint => LinkToPowerPoint(excel, factory),
                    ExcelCommandIds.RefreshLinks => await RefreshAllLinksAsync(excel, factory, bridgeClient, logger),

                    // === Finance Tools ===
                    ExcelCommandIds.ApplyFinanceFormat => ApplyFinanceFormat.Execute(excel,
                        request.Arguments?.GetValueOrDefault("type", "accounting") ?? "accounting"),
                    ExcelCommandIds.ToggleSign => ToggleSign.Execute(excel),
                    ExcelCommandIds.InsertDcfTemplate => DcfTemplateInserter.Execute(excel),
                    ExcelCommandIds.OpenTaskPane => "任务窗格应由 Web Add-in 直接打开。",

                    _ => throw new InvalidOperationException($"不支持的 Excel 命令: {request.CommandId}")
                };
                } // end Excel host else block

                // 异步上报到后端桥接（fire-and-forget，不阻塞 API 响应）
                _ = bridgeClient.DispatchCommandAsync(request.CommandId, reportHost)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                            logger.LogError(t.Exception.InnerException ?? t.Exception,
                                "后端桥接上报失败: {CommandId}", request.CommandId);
                    }, TaskContinuationOptions.OnlyOnFaulted);
                ReportExecuteAudit(
                    bridgeClient,
                    logger,
                    request,
                    reportHost,
                    "command.executed",
                    AuditSeverity.Information,
                    "Command executed via Sidecar REST API.");

                return Results.Ok(ApiEnvelope<SidecarExecuteResponse>.Success(
                    new SidecarExecuteResponse
                    {
                        Success = true,
                        CommandId = request.CommandId,
                        Message = resultMessage,
                        Result = resultMessage
                    },
                    traceId));
            }
            catch (Exception ex)
            {
                ReportExecuteAudit(
                    bridgeClient,
                    logger,
                    request,
                    reportHost,
                    "command.failed",
                    AuditSeverity.Error,
                    ex.Message);
                logger.LogError(ex, "命令执行失败: {CommandId}", request.CommandId);
                return ExecuteFailure($"命令执行失败: {ex.Message}", traceId, StatusCodes.Status500InternalServerError);
            }
        });

        // GET /api/status
        app.MapGet("/api/status", (
            HttpContext context,
            ExcelInteropService excel,
            ILoggerFactory loggerFactory) => GetStatus(context, excel, loggerFactory));

        // GET /api/excel/info — legacy alias kept for compatibility.
        app.MapGet("/api/excel/info", (
            HttpContext context,
            ExcelInteropService excel,
            ILoggerFactory loggerFactory) => GetStatus(context, excel, loggerFactory));
    }

    private static IResult GetStatus(
        HttpContext context,
        ExcelInteropService excel,
        ILoggerFactory loggerFactory)
    {
        var traceId = GetTraceId(context);
        var logger = loggerFactory.CreateLogger("Sidecar.Status");
        try
        {
            var app = excel.GetApplication();
            if (app == null)
            {
                return Results.Ok(ApiEnvelope<SidecarStatusResponse>.Success(
                    new SidecarStatusResponse { Connected = false },
                    traceId));
            }

            return Results.Ok(ApiEnvelope<SidecarStatusResponse>.Success(
                new SidecarStatusResponse
                {
                    Connected = true,
                    Workbook = excel.GetActiveWorkbookName(),
                    Worksheet = excel.GetActiveWorksheetName(),
                    Selection = excel.GetActiveSelectionAddress(),
                    Version = excel.GetVersionInfo()?.Version
                },
                traceId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "读取 Office 状态失败。");
            return Results.Ok(ApiEnvelope<SidecarStatusResponse>.Success(
                new SidecarStatusResponse { Connected = false, Error = ex.Message },
                traceId));
        }
    }

    // ─── Command Implementations ───────────────────────────────────────

    private static string Visualize(dynamic worksheet, dynamic selection, string mode)
    {
        Dictionary<string, CellClassifier.CellType> all = CellClassifier.Classify(worksheet, selection);
        IEnumerable<KeyValuePair<string, CellClassifier.CellType>> filtered = mode switch
        {
            "inputs" => all.Where(kv => kv.Value == CellClassifier.CellType.Hardcoded),
            "formulas" => all.Where(kv => kv.Value == CellClassifier.CellType.Formula),
            "links" => all.Where(kv => kv.Value == CellClassifier.CellType.ExternalLink),
            _ => all
        };
        var filteredDict = filtered.ToDictionary(kv => kv.Key, kv => kv.Value);
        AuditColorMarker.Mark(worksheet, filteredDict);
        return $"审计标色完成 (模式: {mode})：标记了 {filteredDict.Count} 个单元格。";
    }

    private static string ClearVisualizations(dynamic worksheet, dynamic selection)
    {
        AuditColorMarker.Clear(selection);
        return "审计标色已清除。";
    }

    private static string RunModelCheck(dynamic excel)
    {
        var report = ModelCheckRunner.Run(excel);
        return System.Text.Json.JsonSerializer.Serialize(report);
    }

    private static string TracePrecedents(dynamic excel)
    {
        var precedents = PrecedentTracer.TraceDirectPrecedents(excel);
        return System.Text.Json.JsonSerializer.Serialize(precedents);
    }

    private static string TraceDependents(dynamic excel)
    {
        var dependents = DependentTracer.TraceDirectDependents(excel);
        return System.Text.Json.JsonSerializer.Serialize(dependents);
    }

    private static string ClearTracing(dynamic excel)
    {
        // 清除所有追踪箭头
        dynamic activeSheet = excel.ActiveSheet;
        try { activeSheet.ClearArrows(); } catch { }
        return "追踪箭头已清除。";
    }

    private static string OptimizeWorkbook(dynamic excel)
    {
        var result = WorkbookOptimizer.Optimize(excel);
        return System.Text.Json.JsonSerializer.Serialize(result);
    }

    private static string PrepareShare(dynamic excel, Dictionary<string, string>? args)
    {
        var outputPath = args?.GetValueOrDefault("outputPath");
        var result = PrepareToShare.Execute(excel, outputPath);
        return System.Text.Json.JsonSerializer.Serialize(result);
    }

    private static string RunNamesManager(dynamic excel, Dictionary<string, string>? args)
    {
        var action = args?.GetValueOrDefault("action", "scan") ?? "scan";
        var report = action.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
                     action.Equals("deleteInvalid", StringComparison.OrdinalIgnoreCase)
            ? NamesManager.DeleteInvalid(excel)
            : NamesManager.Scan(excel);
        return System.Text.Json.JsonSerializer.Serialize(report);
    }

    private static async Task<DeckCheck.DeckCheckReport> RunDeckCheckAsync(
        dynamic ppt,
        Dictionary<string, string> args,
        BackendBridgeClient bridgeClient,
        ILogger logger)
    {
        var effectiveArgs = new Dictionary<string, string>(args, StringComparer.OrdinalIgnoreCase);
        if (!effectiveArgs.ContainsKey("forbiddenTerms") ||
            string.IsNullOrWhiteSpace(effectiveArgs["forbiddenTerms"]))
        {
            try
            {
                var terms = await bridgeClient.GetDictionaryTermsAsync();
                var forbiddenTerms = terms
                    .Select(term => term.RegexPattern ?? term.Term)
                    .Where(term => !string.IsNullOrWhiteSpace(term))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (forbiddenTerms.Length > 0)
                {
                    effectiveArgs["forbiddenTerms"] = string.Join('|', forbiddenTerms);
                    effectiveArgs["dictionarySource"] = "backend";
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "读取 Backend Corporate Dictionary 失败，Deck Check 将使用请求参数或本地默认术语。");
            }
        }

        return DeckCheck.RunWithDictionary(ppt, effectiveArgs);
    }

    // ─── Cross-App Linking ────────────────────────────────────────────

    private static string LinkToPowerPoint(dynamic excel, OfficeApplicationFactory factory)
    {
        var ppt = factory.GetPowerPoint();
        if (ppt == null) return "PowerPoint 未运行。请先启动 PowerPoint。";

        var result = ExcelToPowerPointLinker.LinkRange(excel, ppt);
        return System.Text.Json.JsonSerializer.Serialize(new { success = true, message = result });
    }

    private static string RefreshAllLinks(dynamic excel)
    {
        var excelResult = LinkRefresher.RefreshExcelLinks(excel);
        var pptResult = LinkRefresher.RefreshPowerPointLinks();

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            excel = new { excelResult.TotalLinks, excelResult.Refreshed, excelResult.Broken },
            powerpoint = new { pptResult.TotalLinks, pptResult.Refreshed, pptResult.Broken },
            brokenDetails = excelResult.BrokenDetails.Concat(pptResult.BrokenDetails).ToList()
        });
    }

    private static async Task<string> RefreshAllLinksAsync(
        dynamic excel,
        OfficeApplicationFactory factory,
        BackendBridgeClient bridgeClient,
        ILogger logger)
    {
        IReadOnlyList<LinkMetadata> links;
        try
        {
            links = await bridgeClient.GetLinkMetadataAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "读取后端链接元数据失败，将回退到本机全量链接刷新。");
            return RefreshAllLinks(excel);
        }

        if (links.Count == 0)
        {
            return RefreshAllLinks(excel);
        }

        var plan = LinkRefreshPlanner.Create(links);
        var excelResult = LinkRefresher.RefreshExcelLinks(excel);
        LinkRefresher.RefreshResult? pptResult = null;
        string? wordResult = null;
        var skippedTargets = new List<string>();

        if (plan.RefreshPowerPoint)
        {
            pptResult = LinkRefresher.RefreshPowerPointLinks(plan.PowerPointTargetObjects);
        }

        if (plan.RefreshWord)
        {
            var word = factory.GetWord();
            if (word == null)
            {
                skippedTargets.Add("Word 未运行，跳过后端元数据中的 Word 链接刷新。");
            }
            else
            {
                var wordRefreshResult = LinkToExcel.RefreshLinkFields(word, plan.WordTargetObjects);
                wordResult = System.Text.Json.JsonSerializer.Serialize(new
                {
                    wordRefreshResult.TotalLinks,
                    wordRefreshResult.Refreshed,
                    wordRefreshResult.Broken,
                    wordRefreshResult.BrokenDetails
                });
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            metadataDriven = true,
            metadata = new
            {
                plan.MetadataCount,
                plan.PowerPointTargets,
                plan.WordTargets,
                plan.PrecisePowerPointTargets,
                plan.PreciseWordTargets,
                linkIds = plan.LinkIds
            },
            excel = new { excelResult.TotalLinks, excelResult.Refreshed, excelResult.Broken },
            powerpoint = pptResult == null
                ? null
                : new { pptResult.TotalLinks, pptResult.Refreshed, pptResult.Broken },
            word = wordResult,
            skippedTargets,
            brokenDetails = excelResult.BrokenDetails
                .Concat(pptResult?.BrokenDetails ?? Enumerable.Empty<string>())
                .Concat(skippedTargets)
                .ToList()
        });
    }

    private static string GetTraceId(HttpContext context)
    {
        var headerTraceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(headerTraceId)
            ? context.TraceIdentifier
            : headerTraceId;
    }

    private static OfficeHost ToOfficeHost(string host) => host switch
    {
        "powerpoint" => OfficeHost.PowerPoint,
        "word" => OfficeHost.Word,
        _ => OfficeHost.Excel
    };

    private static string? ValidateCommandForHost(string host, string commandId)
    {
        return host switch
        {
            "powerpoint" when !KnownPowerPointCommands.Contains(commandId) =>
                $"不支持的 PowerPoint 命令: {commandId}",
            "word" when !KnownWordCommands.Contains(commandId) =>
                $"不支持的 Word 命令: {commandId}",
            "excel" when !KnownExcelCommands.Contains(commandId) =>
                $"不支持的 Excel 命令: {commandId}",
            _ => null
        };
    }

    private static void ReportExecuteAudit(
        BackendBridgeClient bridgeClient,
        ILogger logger,
        SidecarExecuteRequest request,
        OfficeHost host,
        string eventType,
        AuditSeverity severity,
        string message)
    {
        var auditEvent = new AuditEventRequest
        {
            EventType = eventType,
            ActorId = "local-sidecar",
            Host = host,
            Severity = severity,
            CommandId = request.CommandId,
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "sidecar.api.execute",
                ["message"] = message
            }
        };

        _ = bridgeClient.ReportAuditEventAsync(auditEvent)
            .ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    logger.LogError(
                        t.Exception.InnerException ?? t.Exception,
                        "Audit event report failed: {EventType} {CommandId}",
                        eventType,
                        request.CommandId);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private static IReadOnlyList<ShortcutItem> GetShortcutItems(ShortcutRegistry registry) =>
        registry.GetAll()
            .OrderBy(shortcut => shortcut.CommandId, StringComparer.OrdinalIgnoreCase)
            .Select(shortcut => new ShortcutItem
            {
                CommandId = shortcut.CommandId,
                DisplayName = shortcut.DisplayName,
                Shortcut = shortcut.Shortcut
            })
            .ToArray();

    private static IResult ExecuteFailure(string error, string traceId, int statusCode)
    {
        var response = new SidecarExecuteResponse
        {
            Success = false,
            Message = error
        };
        var envelope = ApiEnvelope<SidecarExecuteResponse>.Failure(error, traceId);
        envelope.Data = response;

        return Results.Json(
            envelope,
            statusCode: statusCode);
    }
}
