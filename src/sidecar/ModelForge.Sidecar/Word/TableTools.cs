namespace ModelForge.Sidecar.Word;

/// <summary>
/// Word 表格快速编辑工具。
/// </summary>
public static class TableTools
{
    /// <summary>在当前单元格上方插入一行。</summary>
    public static string InsertRowAbove(dynamic wordApp)
    {
        dynamic table = GetActiveTable(wordApp);
        if (table == null) return "请先将光标放在表格内。";

        dynamic cell = wordApp.Selection.Cells[1];
        int rowIndex = cell.RowIndex;
        dynamic row = table.Rows.Add(table.Rows[rowIndex]);
        return $"已在第 {rowIndex} 行上方插入一行。";
    }

    /// <summary>在当前单元格下方插入一行。</summary>
    public static string InsertRowBelow(dynamic wordApp)
    {
        dynamic table = GetActiveTable(wordApp);
        if (table == null) return "请先将光标放在表格内。";

        dynamic cell = wordApp.Selection.Cells[1];
        int rowIndex = cell.RowIndex;
        dynamic row = table.Rows.Add(table.Rows[rowIndex + 1]);
        return $"已在第 {rowIndex} 行下方插入一行。";
    }

    /// <summary>在当前单元格左侧插入一列。</summary>
    public static string InsertColumnLeft(dynamic wordApp)
    {
        dynamic table = GetActiveTable(wordApp);
        if (table == null) return "请先将光标放在表格内。";

        dynamic cell = wordApp.Selection.Cells[1];
        int colIndex = cell.ColumnIndex;
        dynamic column = table.Columns.Add(table.Columns[colIndex]);
        return $"已在第 {colIndex} 列左侧插入一列。";
    }

    /// <summary>在当前单元格右侧插入一列。</summary>
    public static string InsertColumnRight(dynamic wordApp)
    {
        dynamic table = GetActiveTable(wordApp);
        if (table == null) return "请先将光标放在表格内。";

        dynamic cell = wordApp.Selection.Cells[1];
        int colIndex = cell.ColumnIndex;
        dynamic column = table.Columns.Add(table.Columns[colIndex + 1]);
        return $"已在第 {colIndex} 列右侧插入一列。";
    }

    /// <summary>在当前单元格插入 =SUM(ABOVE) 公式。</summary>
    public static string InsertSumFormula(dynamic wordApp)
    {
        dynamic table = GetActiveTable(wordApp);
        if (table == null) return "请先将光标放在表格内。";

        dynamic cell = wordApp.Selection.Cells[1];
        dynamic range = wordApp.Selection.Range;
        range.Text = "";

        dynamic field = range.Fields.Add(range, -1, "=SUM(ABOVE)", false);
        field.Update();

        return "已在当前单元格插入 =SUM(ABOVE) 公式。";
    }

    private static dynamic? GetActiveTable(dynamic wordApp)
    {
        try
        {
            return wordApp.Selection.Tables.Count > 0
                ? wordApp.Selection.Tables[1]
                : null;
        }
        catch
        {
            return null;
        }
    }
}
