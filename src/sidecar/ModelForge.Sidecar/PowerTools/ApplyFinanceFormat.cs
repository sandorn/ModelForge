namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 财务格式应用。一键为选中区域应用会计格式、千分位或百分比格式。
/// </summary>
public static class ApplyFinanceFormat
{
    public enum FinanceFormatType { Accounting, Percent, Comma, Currency }

    /// <summary>
    /// 对选中区域应用财务格式。默认会计格式。
    /// </summary>
    /// <param name="excelApp">Excel Application</param>
    /// <param name="formatType">格式类型: accounting / percent / comma / currency</param>
    public static string Execute(dynamic excelApp, string formatType = "accounting")
    {
        var type = formatType.ToLowerInvariant() switch
        {
            "percent" => FinanceFormatType.Percent,
            "comma" => FinanceFormatType.Comma,
            "currency" => FinanceFormatType.Currency,
            _ => FinanceFormatType.Accounting
        };

        dynamic selection = excelApp.Selection;
        int cellCount = selection.Cells.Count;

        switch (type)
        {
            case FinanceFormatType.Accounting:
                // 会计格式: $#,###.00 或 _($* #,##0.00_);...
                selection.NumberFormat = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
                selection.Style = "Comma";
                break;

            case FinanceFormatType.Percent:
                // 百分比: 0.00% 带一位小数
                selection.NumberFormat = "0.0%";
                break;

            case FinanceFormatType.Comma:
                // 千分位: #,##0.00
                selection.NumberFormat = "#,##0.00";
                selection.Style = "Comma";
                break;

            case FinanceFormatType.Currency:
                // 货币: $#,##0.00
                selection.NumberFormat = "$#,##0.00";
                break;
        }

        return $"已对 {cellCount} 个单元格应用 {type} 格式。";
    }
}
