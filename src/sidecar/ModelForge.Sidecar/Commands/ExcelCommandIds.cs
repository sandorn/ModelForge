namespace ModelForge.Sidecar.Commands;

/// <summary>
/// Excel 命令 ID 常量定义。从原 VSTO 项目不变移植。
/// 这些 ID 作为 Sidecar REST API 和 Office.js manifest 之间的命令契约。
/// </summary>
public static class ExcelCommandIds
{
    public const string FillRight = "excel.fill-right";
    public const string FillDown = "excel.fill-down";
    public const string WrapIfError = "excel.wrap-iferror";
    public const string InsertStatistics = "excel.insert-statistics";
    public const string VisualizeInputs = "excel.visualize-inputs";
    public const string VisualizeFormulas = "excel.visualize-formulas";
    public const string VisualizeLinks = "excel.visualize-links";
    public const string ClearVisualizations = "excel.clear-visualizations";
    public const string ModelCheck = "excel.model-check";
    public const string TracePrecedents = "excel.trace-precedents";
    public const string TraceDependents = "excel.trace-dependents";
    public const string ClearTrace = "excel.clear-trace";
    public const string OptimizeWorkbook = "excel.optimize-workbook";
    public const string PrepareToShare = "excel.prepare-to-share";
    public const string ApplyFinanceFormat = "excel.apply-finance-format";
    public const string ToggleSign = "excel.toggle-sign";
    public const string AnalyzeFormula = "excel.analyze-formula";
    public const string SaveTemplate = "excel.save-template";
    public const string ListTemplates = "excel.list-templates";
    public const string InsertTemplate = "excel.insert-template";
    public const string Correlation = "excel.correlation";
    public const string DescriptiveStats = "excel.descriptive-stats";
    public const string HideEmptySheets = "excel.hide-empty-sheets";
    public const string InsertXirrTemplate = "excel.insert-xirr-template";
    public const string InsertLboTemplate = "excel.insert-lbo-template";
    public const string AddDropdown = "excel.add-dropdown";
    public const string AddNumericRange = "excel.add-numeric-range";
    public const string ClearValidation = "excel.clear-validation";
    public const string ApplyHeatMap = "excel.apply-heat-map";
    public const string ApplyDataBars = "excel.apply-data-bars";
    public const string ApplyIconSet = "excel.apply-icon-set";
    public const string ApplyTop10 = "excel.apply-top-10";
    public const string ClearConditionalFormats = "excel.clear-conditional-formats";
    public const string PasteValues = "excel.paste-values";
    public const string RemoveDuplicates = "excel.remove-duplicates";
    public const string FreezePanes = "excel.freeze-panes";
    public const string UnfreezePanes = "excel.unfreeze-panes";
    public const string SetPrintArea = "excel.set-print-area";
    public const string ClearPrintArea = "excel.clear-print-area";
    public const string AutoSum = "excel.auto-sum";
    public const string InsertDcfTemplate = "excel.insert-dcf-template";
    public const string InsertBsTemplate = "excel.insert-bs-template";
    public const string LinkToPowerPoint = "excel.link-to-powerpoint";
    public const string LinkChartToPowerPoint = "excel.link-chart-to-powerpoint";
    public const string LinkToWord = "excel.link-to-word";
    public const string RefreshLinks = "excel.refresh-links";
    public const string RepairLinks = "excel.repair-links";
    public const string NamesManager = "excel.names-manager";
    public const string UnhideAllSheets = "excel.unhide-all-sheets";
    public const string ListSheets = "excel.list-sheets";
    public const string InsertColumnChart = "excel.insert-column-chart";
    public const string InsertLineChart = "excel.insert-line-chart";
    public const string UnifyChartSizes = "excel.unify-chart-sizes";
    public const string UnifyChartStyles = "excel.unify-chart-styles";
    public const string AddSeriesToChart = "excel.add-series-to-chart";
    public const string OpenTaskPane = "excel.open-task-pane";
}
