namespace ModelForge.Sidecar.ModelCheck;

/// <summary>
/// Model Check 发现的问题条目。
/// </summary>
public sealed class ModelIssue
{
    public string Address { get; init; } = string.Empty;
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
            // Excel 在工作簿级别追踪循环引用
            dynamic circularRef = workbook.CircularReference;
            if (circularRef != null)
            {
                // 遍历所有含循环引用的工作表区域
                foreach (dynamic area in circularRef)
                {
                    string addr = area.Address;
                    issues.Add(new ModelIssue
                    {
                        Address = addr,
                        Category = "循环引用",
                        Detail = "该单元格参与循环引用链",
                        Formula = null
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
        dynamic worksheet = excelApp.ActiveSheet;

        // 确定实际使用范围
        dynamic usedRange;
        try { usedRange = worksheet.UsedRange; }
        catch { return report; }

        // 1. 错误值扫描
        var errorIssues = ErrorValueScanner.Scan(worksheet, usedRange);
        report.ErrorValueCount = errorIssues.Count;
        report.Issues.AddRange(errorIssues);

        // 2. 外部链接扫描
        var linkIssues = ExternalLinkScanner.Scan(worksheet, usedRange);
        report.ExternalLinkCount = linkIssues.Count;
        report.Issues.AddRange(linkIssues);

        // 3. 循环引用检测
        var circIssues = CircularReferenceDetector.Scan(worksheet, workbook);
        report.CircularRefCount = circIssues.Count;
        report.Issues.AddRange(circIssues);

        // 4. 硬编码值扫描
        var hcIssues = HardcodedValueScanner.Scan(worksheet, usedRange);
        report.HardcodedCount = hcIssues.Count;
        report.Issues.AddRange(hcIssues);

        return report;
    }
}
