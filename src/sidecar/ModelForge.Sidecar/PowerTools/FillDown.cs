namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 快速向下填充。读取选中区域首行，向下复制公式和格式。
/// </summary>
public static class FillDown
{
    /// <summary>
    /// 对 Excel 选中区域执行 FillDown 操作。
    /// </summary>
    public static string Execute(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;

        int rows = selection.Rows.Count;
        int cols = selection.Columns.Count;

        if (rows <= 1)
        {
            return "FillDown 需要选中多行。当前只有 1 行，无需操作。";
        }

        // 逐列向下填充
        for (int c = 1; c <= cols; c++)
        {
            dynamic sourceCell = selection.Cells[1, c];
            dynamic targetRange = selection.Range[selection.Cells[2, c], selection.Cells[rows, c]];
            sourceCell.AutoFill(targetRange, 0);
        }

        return $"FillDown 完成：{cols} 列 × {rows - 1} 行已从首行向下填充。";
    }
}
