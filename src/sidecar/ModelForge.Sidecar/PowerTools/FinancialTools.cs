namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 金融函数工具 — XIRR、NPV 快速模板。
/// </summary>
public static class FinancialTools
{
    /// <summary>插入 XIRR 计算模板（日期 + 现金流 → 内部收益率）。</summary>
    public static string InsertXirrTemplate(dynamic excelApp, int numPeriods = 6)
    {
        numPeriods = Math.Clamp(numPeriods, 2, 20);
        dynamic worksheet = excelApp.ActiveSheet;
        dynamic selection = excelApp.Selection;
        int startRow = selection.Row;
        int col = selection.Column;

        // Header
        worksheet.Cells[startRow, col].Value = "XIRR 计算";
        worksheet.Cells[startRow, col].Font.Bold = true;
        worksheet.Cells[startRow, col].Font.Size = 14;

        worksheet.Cells[startRow, col + 1].Value = "日期";
        worksheet.Cells[startRow, col + 2].Value = "现金流";

        // Sample data
        var today = DateTime.Today;
        var sampleFlows = new[] { -1000, -500, 200, 300, 400, 600, 800, 1000 };

        for (int i = 0; i < numPeriods; i++)
        {
            int row = startRow + 1 + i;
            worksheet.Cells[row, col + 1].Value = today.AddMonths(i * 6).ToOADate();
            worksheet.Cells[row, col + 1].NumberFormat = "yyyy-mm-dd";
            worksheet.Cells[row, col + 2].Value = i < sampleFlows.Length ? sampleFlows[i] : 200;
            worksheet.Cells[row, col + 2].NumberFormat = "#,##0";
        }

        // XIRR formula
        int lastRow = startRow + numPeriods;
        int formulaRow = lastRow + 1;
        string dateCol = GetColumnLetter(col + 1);
        string flowCol = GetColumnLetter(col + 2);

        worksheet.Cells[formulaRow, col].Value = "XIRR:";
        worksheet.Cells[formulaRow, col].Font.Bold = true;
        worksheet.Cells[formulaRow, col + 1].Formula =
            $"=XIRR({flowCol}{startRow + 1}:{flowCol}{lastRow},{dateCol}{startRow + 1}:{dateCol}{lastRow})";
        worksheet.Cells[formulaRow, col + 1].NumberFormat = "0.00%";

        // NPV
        int npvRow = formulaRow + 1;
        worksheet.Cells[npvRow, col].Value = "NPV (10%):";
        worksheet.Cells[npvRow, col].Font.Bold = true;
        worksheet.Cells[npvRow, col + 1].Formula =
            $"=NPV(0.1,{flowCol}{startRow + 1}:{flowCol}{lastRow})";
        worksheet.Cells[npvRow, col + 1].NumberFormat = "#,##0.00";

        // 列宽
        worksheet.Columns[col].ColumnWidth = 14;
        worksheet.Columns[col + 1].ColumnWidth = 14;
        worksheet.Columns[col + 2].ColumnWidth = 14;

        return $"已插入 XIRR 模板（{numPeriods} 期），请在日期和现金流列填入实际数据。";
    }

    /// <summary>插入简化 LBO 模型模板。</summary>
    public static string InsertLboTemplate(dynamic excelApp)
    {
        dynamic worksheet = excelApp.ActiveSheet;
        dynamic selection = excelApp.Selection;
        int r = selection.Row;
        int c = selection.Column;

        var labels = new[] {
            ("LBO 模型", true, 14),
            ("", false, 0),
            ("交易假设", true, 0),
            ("收购价格 ($M)", false, 0), ("债务融资 (%)", false, 0), ("股权融资 (%)", false, 0),
            ("融资利率 (%)", false, 0), ("退出倍数 (x)", false, 0), ("持有期 (年)", false, 0),
            ("", false, 0),
            ("经营预测", true, 0),
            ("收入 ($M)", false, 0), ("EBITDA ($M)", false, 0), ("EBITDA 利润率 (%)", false, 0),
            ("D&A ($M)", false, 0), ("CAPEX ($M)", false, 0), ("NWC 变动 ($M)", false, 0),
            ("", false, 0),
            ("债务与回报", true, 0),
            ("期初债务 ($M)", false, 0), ("偿还 ($M)", false, 0), ("期末债务 ($M)", false, 0),
            ("自由现金流 ($M)", false, 0), ("股权价值 ($M)", false, 0),
            ("MOIC (x)", false, 0), ("IRR (%)", false, 0),
        };

        // Year headers
        worksheet.Cells[r, c + 1].Value = "Year 0";
        for (int i = 1; i <= 5; i++)
            worksheet.Cells[r, c + 1 + i].Value = $"Year {i}";

        for (int i = 0; i < labels.Length; i++)
        {
            int row = r + 1 + i;
            worksheet.Cells[row, c].Value = labels[i].Item1;
            if (labels[i].Item2) worksheet.Cells[row, c].Font.Bold = true;
            if (labels[i].Item3 > 0) worksheet.Cells[row, c].Font.Size = labels[i].Item3;
        }

        worksheet.Columns[c].ColumnWidth = 22;
        for (int i = 0; i <= 5; i++) worksheet.Columns[c + 1 + i].ColumnWidth = 14;

        return "LBO model template inserted. Fill in the assumptions (highlighted rows) with actual deal data.";
    }

    private static string GetColumnLetter(int col)
    {
        string result = "";
        while (col > 0) { col--; result = (char)('A' + col % 26) + result; col /= 26; }
        return result;
    }
}
