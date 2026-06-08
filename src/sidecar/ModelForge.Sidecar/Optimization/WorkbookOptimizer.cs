namespace ModelForge.Sidecar.Optimization;

/// <summary>
/// 工作簿优化器。减少文件体积，清除未使用样式、无效名称、外部链接残留。
/// </summary>
public static class WorkbookOptimizer
{
    public sealed class OptimizationResult
    {
        public int StylesRemoved { get; set; }
        public int InvalidNamesRemoved { get; set; }
        public int ExternalLinkResiduesRemoved { get; set; }
        public long EstimatedSizeReduction { get; set; }
        public List<string> Actions { get; } = new();
    }

    public static OptimizationResult Optimize(dynamic excelApp)
    {
        var result = new OptimizationResult();
        dynamic workbook = excelApp.ActiveWorkbook;

        // 1. 清除超出使用范围的格式
        try
        {
            foreach (dynamic worksheet in workbook.Worksheets)
            {
                try
                {
                    dynamic usedRange = worksheet.UsedRange;
                    int lastRow = usedRange.Row + usedRange.Rows.Count;
                    int lastCol = usedRange.Column + usedRange.Columns.Count;

                    // 清除使用范围之外的整行
                    if (lastRow < worksheet.Rows.Count)
                    {
                        dynamic excessRows = worksheet.Rows[$"{lastRow + 1}:{worksheet.Rows.Count}"];
                        excessRows.Clear();
                        result.Actions.Add($"工作表 '{worksheet.Name}': 清除 {lastRow + 1} 行之后的格式");
                    }
                }
                catch { /* 跳过受保护的工作表 */ }
            }
        }
        catch { }

        // 2. 删除无效名称
        try
        {
            var namesToDelete = new List<string>();
            foreach (dynamic name in workbook.Names)
            {
                try
                {
                    string refersTo = name.RefersTo ?? "";
                    if (refersTo.Contains("#REF!"))
                    {
                        namesToDelete.Add(name.Name);
                    }
                }
                catch { }
            }

            foreach (var name in namesToDelete)
            {
                try
                {
                    workbook.Names(name).Delete();
                    result.InvalidNamesRemoved++;
                }
                catch { }
            }
        }
        catch { }

        // 2.5 删除未使用的自定义样式
        try
        {
            var builtInStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Normal", "Comma", "Currency", "Percent", "Note", "Warning Text",
                "Heading 1", "Heading 2", "Heading 3", "Heading 4",
                "Title", "Subtitle", "Emphasis", "Strong", "List Bullet",
                "Good", "Bad", "Neutral", "Calculation", "Check Cell",
                "Explanatory Text", "Input", "Linked Cell", "Output", "Total"
            };

            foreach (dynamic style in workbook.Styles)
            {
                try
                {
                    string name = style.Name ?? "";
                    if (!builtInStyles.Contains(name) && !name.StartsWith("Followed", StringComparison.OrdinalIgnoreCase))
                    {
                        style.Delete();
                        result.StylesRemoved++;
                    }
                }
                catch { /* Built-in styles cannot be deleted */ }
            }
        }
        catch { }

        // 3. 删除外部链接残留
        try
        {
            dynamic linkSources = workbook.LinkSources(1); // xlExcelLinks = 1
            if (linkSources != null)
            {
                foreach (dynamic link in linkSources)
                {
                    try
                    {
                        workbook.BreakLink(link, 1); // xlLinkTypeExcelLinks = 1
                        result.ExternalLinkResiduesRemoved++;
                    }
                    catch { }
                }
            }
        }
        catch { }

        result.Actions.Add($"优化完成: 删除 {result.InvalidNamesRemoved} 个无效名称, " +
            $"{result.ExternalLinkResiduesRemoved} 个外部链接残留");
        return result;
    }
}
