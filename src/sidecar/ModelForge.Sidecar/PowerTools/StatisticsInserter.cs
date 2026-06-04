namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 统计摘要插入。在选中区域下方快速生成 MIN/MAX/AVERAGE/COUNT/SUM 公式。
/// </summary>
public static class StatisticsInserter
{
    /// <summary>
    /// 在选中区域的下方插入统计行。
    /// </summary>
    /// <param name="excelApp">Excel Application (dynamic COM)</param>
    /// <param name="stats">要生成的统计类型集合，默认全部。</param>
    /// <returns>操作结果描述。</returns>
    public static string Execute(dynamic excelApp, string[]? stats = null)
    {
        stats ??= new[] { "MIN", "MAX", "AVERAGE", "COUNT", "SUM" };

        dynamic selection = excelApp.Selection;
        int rows = selection.Rows.Count;
        int cols = selection.Columns.Count;

        int startRow = selection.Row;
        int endRow = startRow + rows - 1;
        int startCol = selection.Column;
        int endCol = startCol + cols - 1;

        dynamic worksheet = selection.Worksheet;

        for (int i = 0; i < stats.Length; i++)
        {
            int targetRow = endRow + 1 + i;

            // 标签列
            worksheet.Cells[targetRow, startCol].Value = stats[i];

            // 统计公式列
            for (int c = 1; c < cols; c++)
            {
                int colIndex = startCol + c;
                string colLetter = GetColumnLetter(colIndex);
                string rangeRef = $"{colLetter}{startRow}:{colLetter}{endRow}";

                dynamic formulaCell = worksheet.Cells[targetRow, colIndex];
                formulaCell.Formula = stats[i] switch
                {
                    "MIN" => $"=MIN({rangeRef})",
                    "MAX" => $"=MAX({rangeRef})",
                    "AVERAGE" => $"=AVERAGE({rangeRef})",
                    "COUNT" => $"=COUNT({rangeRef})",
                    "SUM" => $"=SUM({rangeRef})",
                    _ => $"={stats[i]}({rangeRef})"
                };
            }
        }

        // 加粗标签列
        dynamic labelRange = worksheet.Range[
            worksheet.Cells[endRow + 1, startCol],
            worksheet.Cells[endRow + stats.Length, startCol]];
        labelRange.Font.Bold = true;

        return $"统计摘要已插入：{stats.Length} 行 × {cols} 列，位于第 {endRow + 1} 行至第 {endRow + stats.Length} 行。";
    }

    private static string GetColumnLetter(int col)
    {
        string result = "";
        while (col > 0)
        {
            col--;
            result = (char)('A' + col % 26) + result;
            col /= 26;
        }
        return result;
    }
}
