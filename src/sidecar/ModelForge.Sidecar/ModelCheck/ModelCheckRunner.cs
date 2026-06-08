namespace ModelForge.Sidecar.ModelCheck;

/// <summary>
/// Model Check 发现的问题条目。
/// </summary>
public sealed class ModelIssue
{
    public string Address { get; init; } = string.Empty;
    public string WorksheetName { get; set; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string? Formula { get; init; }
}

/// <summary>
/// Model Check 完整报告。
/// </summary>
public sealed class ModelCheckReport
{
    public List<ModelIssue> Issues { get; } = new();
    public int ErrorValueCount { get; set; }
    public int ExternalLinkCount { get; set; }
    public int CircularRefCount { get; set; }
    public int HardcodedCount { get; set; }
    public int WorksheetCount { get; set; }
    public int TotalIssues => Issues.Count;
}

/// <summary>
/// 错误值扫描器。检测 #REF!, #N/A, #VALUE!, #DIV/0!, #NUM!, #NAME?, #NULL!。
/// </summary>
public static class ErrorValueScanner
{
    private static readonly HashSet<string> ErrorValues = new(StringComparer.Ordinal)
    {
        "#REF!", "#N/A", "#VALUE!", "#DIV/0!", "#NUM!", "#NAME?", "#NULL!"
    };

    public static List<ModelIssue> Scan(dynamic worksheet, dynamic usedRange)
    {
        var issues = new List<ModelIssue>();

        try
        {
            // 使用 SpecialCells 高效定位错误值
            dynamic errorCells = usedRange.SpecialCells(16); // xlCellTypeFormulas, xlErrors = 16
            foreach (dynamic cell in errorCells)
            {
                string addr = cell.Address;
                string? formula = null;
                try { formula = cell.Formula; } catch { }

                string text = (cell.Text is string s) ? s : "Unknown Error";
                issues.Add(new ModelIssue
                {
                    Address = addr,
                    Category = "公式错误",
                    Detail = text,
                    Formula = formula
                });
            }
        }
        catch
        {
            // SpecialCells 找不到时抛出异常 — 没有错误值
        }

        return issues;
    }
}

/// <summary>
/// 外部链接扫描器。扫描所有引用外部工作簿的公式。
/// </summary>
public static class ExternalLinkScanner
{
    public static List<ModelIssue> Scan(dynamic worksheet, dynamic usedRange)
    {
        var issues = new List<ModelIssue>();

        foreach (dynamic cell in usedRange)
        {
            if (!cell.HasFormula) continue;

            string formula = (cell.Formula as string) ?? "";
            // 检测外部工作簿引用: [Workbook.xlsx] 或 'C:\path\[file.xlsx]'
            if (formula.Contains('[') || (formula.Contains('!') && formula.Contains(":\\")))
            {
                issues.Add(new ModelIssue
                {
                    Address = cell.Address,
                    Category = "外部链接",
                    Detail = "公式引用外部工作簿",
                    Formula = formula
                });
            }
        }
        return issues;
    }
}

/// <summary>
/// 循环引用检测器。利用 Excel 自身的 CircularReference 属性。
/// </summary>
public static class CircularReferenceDetector
{
    public static List<ModelIssue> Scan(dynamic worksheet, dynamic workbook)
    {
        var issues = new List<ModelIssue>();

        try
        {
            dynamic circularRef = workbook.CircularReference;
            if (circularRef != null)
            {
                foreach (dynamic area in circularRef)
                {
                    string addr = area.Address;
                    string? formula = null;
                    try { formula = area.Formula; } catch { }

                    // 根据公式模式生成修复建议
                    var suggestion = BuildSuggestion(addr, formula);
                    issues.Add(new ModelIssue
                    {
                        Address = addr,
                        Category = "循环引用",
                        Detail = suggestion,
                        Formula = formula
                    });
                }
            }
        }
        catch
        {
            // 没有循环引用
        }

        return issues;
    }

    private static string BuildSuggestion(string addr, string? formula)
    {
        if (formula != null && formula.Contains(addr, StringComparison.OrdinalIgnoreCase))
            return $"单元格 {addr} 的公式直接引用了自身。建议：检查公式范围是否意外包含自身，或将迭代逻辑移到辅助列。";

        if (formula != null && (formula.Contains("OFFSET", StringComparison.OrdinalIgnoreCase) ||
                                 formula.Contains("INDIRECT", StringComparison.OrdinalIgnoreCase)))
            return $"单元格 {addr} 使用了 OFFSET/INDIRECT 易导致循环引用。建议：改用 INDEX/MATCH 或直接单元格引用。";

        return $"单元格 {addr} 参与了循环引用链 (A→B→...→A)。建议：1) 检查公式链的逻辑流向，" +
               "2) 考虑拆分迭代计算为多个步骤，3) 如确需迭代，请在「文件→选项→公式」中启用迭代计算。";
    }
}

/// <summary>
/// 硬编码值扫描器。检测非公式的数字/文本常量。
/// </summary>
public static class HardcodedValueScanner
{
    public static List<ModelIssue> Scan(dynamic worksheet, dynamic usedRange)
    {
        var issues = new List<ModelIssue>();

        try
        {
            // SpecialCells: xlCellTypeConstants = 2
            dynamic constants = usedRange.SpecialCells(2);
            foreach (dynamic cell in constants)
            {
                object? val = cell.Value;
                if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                {
                    issues.Add(new ModelIssue
                    {
                        Address = cell.Address,
                        Category = "硬编码值",
                        Detail = $"硬编码常量: {val}",
                        Formula = null
                    });
                }
            }
        }
        catch
        {
            // 无常量
        }

        return issues;
    }
}

/// <summary>
/// Model Check 编排器。执行全部四项扫描并生成汇总报告。
/// </summary>
public static class ModelCheckRunner
{
    public static ModelCheckReport Run(dynamic excelApp)
    {
        var report = new ModelCheckReport();
        dynamic workbook = excelApp.ActiveWorkbook;

        // 遍历所有工作表
        int sheetCount = 0;
        foreach (dynamic worksheet in workbook.Worksheets)
        {
            sheetCount++;
            string sheetName = worksheet.Name;

            // 跳过隐藏工作表（性能优化）
            try
            {
                if (worksheet.Visible != -1) continue; // xlSheetVisible = -1
            }
            catch { /* 非关键，继续扫描 */ }

            dynamic usedRange;
            try { usedRange = worksheet.UsedRange; }
            catch { continue; }

            // 1. 错误值扫描
            var errorIssues = ErrorValueScanner.Scan(worksheet, usedRange);
            foreach (var issue in errorIssues) issue.WorksheetName = sheetName;
            report.ErrorValueCount += errorIssues.Count;
            report.Issues.AddRange(errorIssues);

            // 2. 外部链接扫描
            var linkIssues = ExternalLinkScanner.Scan(worksheet, usedRange);
            foreach (var issue in linkIssues) issue.WorksheetName = sheetName;
            report.ExternalLinkCount += linkIssues.Count;
            report.Issues.AddRange(linkIssues);

            // 3. 硬编码值扫描
            var hcIssues = HardcodedValueScanner.Scan(worksheet, usedRange);
            foreach (var issue in hcIssues) issue.WorksheetName = sheetName;
            report.HardcodedCount += hcIssues.Count;
            report.Issues.AddRange(hcIssues);
        }

        // 4. 循环引用检测（工作簿级别，仅执行一次）
        dynamic firstSheet = excelApp.ActiveSheet;
        var circIssues = CircularReferenceDetector.Scan(firstSheet, workbook);
        report.CircularRefCount = circIssues.Count;
        report.Issues.AddRange(circIssues);

        report.WorksheetCount = sheetCount;
        return report;
    }
}
