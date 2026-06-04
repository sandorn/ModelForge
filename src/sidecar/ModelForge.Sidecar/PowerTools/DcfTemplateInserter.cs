namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// DCF 估值模板插入器。在活动工作表中生成标准贴现现金流分析模板。
/// </summary>
public static class DcfTemplateInserter
{
    /// <summary>
    /// 在当前选中位置插入 DCF 模板。
    /// </summary>
    /// <param name="excelApp">Excel Application</param>
    /// <param name="numYears">预测期年数，默认 5。</param>
    public static string Execute(dynamic excelApp, int numYears = 5)
    {
        dynamic worksheet = excelApp.ActiveSheet;
        dynamic selection = excelApp.Selection;

        int startRow = selection.Row;
        int startCol = selection.Column;

        // ── DCF 模板行定义 ──
        var rows = new (string Label, string? FormulaTemplate, bool Bold, double? ColorLevel)[]
        {
            // 标题行: 年份
            ("DCF 估值分析", null, true, null),
            ("", null, false, null),
            ("假设", null, true, null),
            ("收入增长率 (%)", null, false, null),
            ("EBITDA 利润率 (%)", null, false, null),
            ("折旧 (% of Revenue)", null, false, null),
            ("CAPEX (% of Revenue)", null, false, null),
            ("营运资本变动 (% of ΔRevenue)", null, false, null),
            ("税率 (%)", null, false, null),
            ("WACC (%)", null, false, null),
            ("永续增长率 (%)", null, false, null),
            ("", null, false, null),
            ("预测", null, true, null),
            ("收入", null, false, null),
            ("EBITDA", "=B14*B4", true, null),
            ("折旧", "=B14*B5", false, null),
            ("EBIT", "=B15-B16", true, null),
            ("所得税", "=B17*B9", false, null),
            ("NOPAT", "=B17-B18", true, null),
            ("加回: 折旧", "=B16", false, null),
            ("减: CAPEX", "=B14*B7", false, null),
            ("减: 营运资本变动", "=(B14-B[PREV]14)*B8", false, null), // 需要年份偏移处理
            ("自由现金流 (FCF)", "=B19+B20-B21-B22", true, null),
            ("", null, false, null),
            ("估值", null, true, null),
            ("贴现因子", "=1/(1+B10)^(COL()-COL(B26)+1)", false, null),
            ("贴现 FCF", "=B23*B26", false, null),
            ("", null, false, null),
            ("终值", "=B23*(1+B11)/(B10-B11)", true, null),
            ("终值现值", "=B29*B26", true, null),
            ("", null, false, null),
            ("企业价值", "=SUM(B27:B[LAST])+B30", true, null),
        };

        // ── 写入模板 ──
        // 设置年份标题
        worksheet.Cells[startRow, startCol].Value = rows[0].Label;
        worksheet.Cells[startRow, startCol].Font.Bold = true;
        worksheet.Cells[startRow, startCol].Font.Size = 14;

        for (int i = 0; i <= numYears; i++)
        {
            int col = startCol + 1 + i;
            if (i == 0)
                worksheet.Cells[startRow, col].Value = "历史";
            else
                worksheet.Cells[startRow, col].Value = $"Year {i}";
            worksheet.Cells[startRow, col].Font.Bold = true;
            worksheet.Cells[startRow, col].HorizontalAlignment = -4152; // xlCenter
        }

        // 写入模板行
        for (int r = 0; r < rows.Length; r++)
        {
            int rowIdx = startRow + 2 + r;
            dynamic labelCell = worksheet.Cells[rowIdx, startCol];
            labelCell.Value = rows[r].Label;

            if (rows[r].Bold)
            {
                labelCell.Font.Bold = true;
            }
        }

        // 设置列宽
        worksheet.Columns[startCol].ColumnWidth = 28;

        // 设置数字格式
        for (int c = 0; c <= numYears; c++)
        {
            int col = startCol + 1 + c;
            worksheet.Columns[col].ColumnWidth = 14;

            // 百分比行
            for (int r = 3; r <= 10; r++)
            {
                worksheet.Cells[startRow + 2 + r, col].NumberFormat = "0.0%";
            }
            // 货币行
            for (int r = 13; r <= 31; r++)
            {
                worksheet.Cells[startRow + 2 + r, col].NumberFormat = "#,##0.0";
            }
        }

        return $"DCF 模板已插入到 {worksheet.Name}!{GetColumnLetter(startCol)}{startRow}，" +
               $"{numYears} 年预测期。请填写假设行（黄色标记）的百分比数值。";
    }

    private static string GetColumnLetter(int col)
    {
        string result = "";
        while (col > 0) { col--; result = (char)('A' + col % 26) + result; col /= 26; }
        return result;
    }
}
