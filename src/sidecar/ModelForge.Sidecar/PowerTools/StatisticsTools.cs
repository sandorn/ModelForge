namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 统计分析工具 — 相关性、回归等。
/// </summary>
public static class StatisticsTools
{
    /// <summary>计算选中两列数据的相关系数（PEARSON）。</summary>
    public static string Correlation(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        if (selection.Columns.Count < 2)
            return "请至少选中两列数据来计算相关系数。";

        dynamic worksheet = excelApp.ActiveSheet;
        int row = selection.Row + selection.Rows.Count + 2;
        int col = selection.Column;

        // 插入 PEARSON 公式
        dynamic cell = worksheet.Cells[row, col];
        string col1 = GetColumnLetter(selection.Column);
        string col2 = GetColumnLetter(selection.Column + 1);
        int startRow = selection.Row;
        int endRow = selection.Row + selection.Rows.Count - 1;

        cell.Formula = $"=PEARSON({col1}{startRow}:{col1}{endRow},{col2}{startRow}:{col2}{endRow})";
        cell.NumberFormat = "0.0000";
        cell.Font.Bold = true;

        worksheet.Cells[row, col - 1].Value = "相关系数 (r):";

        return $"已计算 {col1}{startRow}:{col1}{endRow} 与 {col2}{startRow}:{col2}{endRow} 的 Pearson 相关系数。";
    }

    /// <summary>插入描述性统计摘要（含标准差、偏度、峰度）。</summary>
    public static string DescriptiveStats(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        dynamic worksheet = excelApp.ActiveSheet;
        int outRow = selection.Row + selection.Rows.Count + 2;
        int outCol = selection.Column;

        string col = GetColumnLetter(selection.Column);
        int start = selection.Row;
        int end = selection.Row + selection.Rows.Count - 1;
        string range = $"{col}{start}:{col}{end}";

        var stats = new (string Label, string Formula)[]
        {
            ("描述性统计", ""),
            ("样本数", $"=COUNT({range})"),
            ("均值", $"=AVERAGE({range})"),
            ("中位数", $"=MEDIAN({range})"),
            ("标准差", $"=STDEV.S({range})"),
            ("最小值", $"=MIN({range})"),
            ("最大值", $"=MAX({range})"),
            ("偏度", $"=SKEW({range})"),
            ("峰度", $"=KURT({range})"),
            ("总和", $"=SUM({range})"),
        };

        for (int i = 0; i < stats.Length; i++)
        {
            worksheet.Cells[outRow + i, outCol].Value = stats[i].Label;
            worksheet.Cells[outRow + i, outCol].Font.Bold = i == 0;
            if (!string.IsNullOrEmpty(stats[i].Formula))
            {
                worksheet.Cells[outRow + i, outCol + 1].Formula = stats[i].Formula;
                worksheet.Cells[outRow + i, outCol + 1].NumberFormat = "0.0000";
            }
        }

        return $"已为 {range} 生成描述性统计摘要（{stats.Length - 1} 项指标）。";
    }

    private static string GetColumnLetter(int col)
    {
        string result = "";
        while (col > 0) { col--; result = (char)('A' + col % 26) + result; col /= 26; }
        return result;
    }
}
