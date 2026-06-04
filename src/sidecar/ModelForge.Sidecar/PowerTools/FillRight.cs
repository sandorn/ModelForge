namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 快速向右填充。读取选中区域最左列，向右复制公式和格式。
/// </summary>
public static class FillRight
{
    /// <summary>
    /// 对 Excel 选中区域执行 FillRight 操作。
    /// </summary>
    /// <param name="excelApp">Excel Application (dynamic COM)</param>
    /// <returns>操作结果描述。</returns>
    public static string Execute(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;

        // 获取选中区域的列数和行数
        int rows = selection.Rows.Count;
        int cols = selection.Columns.Count;

        if (cols <= 1)
        {
            return "FillRight 需要选中多列。当前只有 1 列，无需操作。";
        }

        // 逐行向右填充
        for (int r = 1; r <= rows; r++)
        {
            dynamic sourceCell = selection.Cells[r, 1];
            dynamic targetRange = selection.Range[selection.Cells[r, 2], selection.Cells[r, cols]];
            sourceCell.AutoFill(targetRange, 0); // xlFillDefault = 0
        }

        return $"FillRight 完成：{rows} 行 × {cols - 1} 列已从最左列向右填充。";
    }
}
