namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 工作表批量管理工具。取消隐藏、批量重命名等。
/// </summary>
public static class SheetManager
{
    /// <summary>取消隐藏所有隐藏的工作表。</summary>
    public static string UnhideAll(dynamic excelApp)
    {
        dynamic workbook = excelApp.ActiveWorkbook;
        int count = 0;
        var names = new List<string>();

        foreach (dynamic sheet in workbook.Sheets)
        {
            try
            {
                int visibility = sheet.Visible;
                if (visibility == 0) // xlSheetHidden
                {
                    sheet.Visible = -1; // xlSheetVisible
                    names.Add(sheet.Name);
                    count++;
                }
            }
            catch { }
        }

        return count == 0
            ? "当前工作簿没有隐藏的工作表。"
            : $"已取消隐藏 {count} 个工作表：{string.Join("、", names)}。";
    }

    /// <summary>为所有工作表添加统一前缀/后缀。</summary>
    public static string BatchRename(dynamic excelApp, string? prefix = null, string? suffix = null)
    {
        if (string.IsNullOrWhiteSpace(prefix) && string.IsNullOrWhiteSpace(suffix))
            return "请指定 prefix 或 suffix 参数。";

        dynamic workbook = excelApp.ActiveWorkbook;
        int count = 0;

        foreach (dynamic sheet in workbook.Sheets)
        {
            try
            {
                string original = sheet.Name;
                string renamed = (prefix ?? "") + original + (suffix ?? "");
                if (renamed != original && renamed.Length <= 31) // Excel 工作表名最长 31 字符
                {
                    sheet.Name = renamed;
                    count++;
                }
            }
            catch { }
        }

        return $"已重命名 {count} 个工作表。";
    }

    /// <summary>列出所有工作表及其可见性状态。</summary>
    public static string ListSheets(dynamic excelApp)
    {
        dynamic workbook = excelApp.ActiveWorkbook;
        var sheets = new List<object>();

        foreach (dynamic sheet in workbook.Sheets)
        {
            try
            {
                int visibility = sheet.Visible;
                sheets.Add(new
                {
                    name = (string)sheet.Name,
                    visible = visibility == -1,
                    visibility = visibility == -1 ? "visible" : visibility == 0 ? "hidden" : "veryHidden"
                });
            }
            catch { }
        }

        return System.Text.Json.JsonSerializer.Serialize(new { count = sheets.Count, sheets });
    }

    /// <summary>隐藏所有空白工作表（无数据）。</summary>
    public static string HideEmptySheets(dynamic excelApp)
    {
        dynamic workbook = excelApp.ActiveWorkbook;
        int count = 0;
        var names = new List<string>();

        foreach (dynamic sheet in workbook.Sheets)
        {
            try
            {
                if (sheet.Visible != -1) continue; // 跳过已隐藏
                dynamic usedRange = sheet.UsedRange;
                // 判断是否为空：只有 A1 有值且为空
                bool isEmpty = usedRange.Rows.Count == 1 && usedRange.Columns.Count == 1
                    && (usedRange.Cells[1, 1].Value == null || string.IsNullOrWhiteSpace(usedRange.Cells[1, 1].Value?.ToString()));
                if (isEmpty)
                {
                    sheet.Visible = 0; // xlSheetHidden
                    names.Add(sheet.Name);
                    count++;
                }
            }
            catch { }
        }

        return count == 0 ? "No empty sheets found." : $"Hid {count} empty sheets: {string.Join(", ", names)}.";
    }

    /// <summary>Remove duplicate rows from selection based on all columns.</summary>
    public static string RemoveDuplicates(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        if (selection.Rows.Count < 2) return "Please select at least 2 rows (including header).";

        try
        {
            selection.RemoveDuplicates();
            return $"Removed duplicates from {selection.Address}.";
        }
        catch (Exception ex)
        {
            return $"Remove duplicates failed: {ex.Message}";
        }
    }

    /// <summary>Freeze panes at the selected cell.</summary>
    public static string FreezePanes(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        dynamic window = excelApp.ActiveWindow;
        selection.Activate();
        window.FreezePanes = true;
        return $"Froze panes at {selection.Address}.";
    }

    /// <summary>Unfreeze all panes.</summary>
    public static string UnfreezePanes(dynamic excelApp)
    {
        dynamic window = excelApp.ActiveWindow;
        window.FreezePanes = false;
        return "Unfroze all panes.";
    }

    /// <summary>Set print area to selection.</summary>
    public static string SetPrintArea(dynamic excelApp)
    {
        dynamic worksheet = excelApp.ActiveSheet;
        dynamic selection = excelApp.Selection;
        worksheet.PageSetup.PrintArea = selection.Address;
        return $"Set print area to {selection.Address}.";
    }

    /// <summary>Clear print area.</summary>
    public static string ClearPrintArea(dynamic excelApp)
    {
        dynamic worksheet = excelApp.ActiveSheet;
        worksheet.PageSetup.PrintArea = "";
        return "Cleared print area.";
    }

    /// <summary>Insert AutoSum formula below/right of selection.</summary>
    public static string AutoSum(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        dynamic worksheet = excelApp.ActiveSheet;
        int lastRow = selection.Row + selection.Rows.Count;
        int col = selection.Column;

        for (int c = 0; c < selection.Columns.Count; c++)
        {
            int targetCol = col + c;
            worksheet.Cells[lastRow, targetCol].Formula =
                $"=SUM({GetColumnLetter(targetCol)}{selection.Row}:{GetColumnLetter(targetCol)}{lastRow - 1})";
        }
        return $"Inserted SUM formulas in row {lastRow}.";
    }

    private static string GetColumnLetter(int col)
    {
        string result = "";
        while (col > 0) { col--; result = (char)('A' + col % 26) + result; col /= 26; }
        return result;
    }
}
