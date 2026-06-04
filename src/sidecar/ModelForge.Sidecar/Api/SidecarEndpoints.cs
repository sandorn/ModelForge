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
            var shortcuts = registry.GetAll();
            return Results.Ok(shortcuts.Select(s => new
            {
                s.CommandId,
                s.DisplayName,
                s.Shortcut
            }));
        });

        // POST /api/execute — 核心命令路由
        app.MapPost("/api/execute", async (
            SidecarExecuteRequest request,
            ExcelInteropService excelService,
            OfficeApplicationFactory factory,
            BackendBridgeClient bridgeClient,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Sidecar.Execute");
            logger.LogInformation("执行命令: {CommandId}", request.CommandId);

            try
            {
                string resultMessage;

                // ── PowerPoint Commands ──
                if (request.Host == "powerpoint")
                {
                    var ppt = factory.GetPowerPoint();
                    if (ppt == null)
                        return Results.Problem(title: "PowerPoint 未运行", detail: "请先启动 PowerPoint。", statusCode: 503);

                    resultMessage = request.CommandId switch
                    {
                        PptCommandIds.GenerateAgenda => System.Text.Json.JsonSerializer.Serialize(
                            DynamicAgendas.Generate(ppt, logger)),
                        PptCommandIds.DeckCheck => System.Text.Json.JsonSerializer.Serialize(
                            DeckCheck.Run(ppt)),
                        PptCommandIds.AlignLeft => ShapeTools.AlignLeft(ppt),
                        PptCommandIds.DistributeHorizontal => ShapeTools.DistributeHorizontal(ppt),
                        PptCommandIds.UnifySize => ShapeTools.UnifySize(ppt),
                        _ => $"未知 PowerPoint 命令: {request.CommandId}"
                    };
                }
                // ── Word Commands ──
                else if (request.Host == "word")
                {
                    var word = factory.GetWord();
                    if (word == null)
                        return Results.Problem(title: "Word 未运行", detail: "请先启动 Word。", statusCode: 503);

                    resultMessage = request.CommandId switch
                    {
                        WordCommandIds.BuildDueDiligence =>
                            DocBuilder.Build(word, DocBuilder.CreateDueDiligenceTemplate()),
                        WordCommandIds.EmbedExcelRange =>
                            LinkToExcel.EmbedExcelRange(excelService.GetApplication(), word),
                        WordCommandIds.RefreshLinks =>
                            LinkToExcel.RefreshLinks(word),
                        _ => $"未知 Word 命令: {request.CommandId}"
                    };
                }
                // ── Excel Commands (default) ──
                else
                {
                    var excel = excelService.GetApplication();
                    if (excel == null)
                        return Results.Problem(title: "Excel 未运行", detail: "请先启动 Excel 再执行命令。", statusCode: 503);

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

                    // === Cross-App Linking ===
                    ExcelCommandIds.LinkToPowerPoint => LinkToPowerPoint(excel, factory),
                    ExcelCommandIds.RefreshLinks => RefreshAllLinks(excel),

                    // === Finance Tools ===
                    ExcelCommandIds.ApplyFinanceFormat => ApplyFinanceFormat.Execute(excel,
                        request.Arguments?.GetValueOrDefault("type", "accounting") ?? "accounting"),
                    ExcelCommandIds.ToggleSign => ToggleSign.Execute(excel),
                    ExcelCommandIds.InsertDcfTemplate => DcfTemplateInserter.Execute(excel),
                    ExcelCommandIds.OpenTaskPane => "任务窗格应由 Web Add-in 直接打开。",

                    _ => $"未知命令: {request.CommandId}"
                };
                } // end Excel host else block

                // 异步上报到后端桥接（fire-and-forget，不阻塞 API 响应）
                var reportHost = request.Host switch
                {
                    "powerpoint" => OfficeHost.PowerPoint,
                    "word" => OfficeHost.Word,
                    _ => OfficeHost.Excel
                };
                _ = bridgeClient.DispatchCommandAsync(request.CommandId, reportHost)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                            logger.LogError(t.Exception.InnerException ?? t.Exception,
                                "后端桥接上报失败: {CommandId}", request.CommandId);
                    }, TaskContinuationOptions.OnlyOnFaulted);

                return Results.Ok(new { success = true, commandId = request.CommandId, message = resultMessage });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "命令执行失败: {CommandId}", request.CommandId);
                return Results.Problem(title: "命令执行失败", detail: ex.Message, statusCode: 500);
            }
        });

        // GET /api/excel/info
        app.MapGet("/api/excel/info", (ExcelInteropService excel) =>
        {
            try
            {
                var app = excel.GetApplication();
                if (app == null) return Results.Ok(new { connected = false });

                return Results.Ok(new
                {
                    connected = true,
                    workbook = excel.GetActiveWorkbookName(),
                    worksheet = excel.GetActiveWorksheetName(),
                    selection = excel.GetActiveSelectionAddress(),
                    version = excel.GetVersionInfo()?.Version
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { connected = false, error = ex.Message });
            }
        });
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
}

/// <summary>
/// Sidecar 命令执行请求 DTO。
/// </summary>
public sealed class SidecarExecuteRequest
{
    public string CommandId { get; set; } = string.Empty;
    public string Host { get; set; } = "excel";
    public Dictionary<string, string>? Arguments { get; set; }
}
