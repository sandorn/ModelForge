namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// Black-Scholes 期权定价模板插入器。在活动工作表中生成标准 BS 定价模型。
/// </summary>
public static class BlackScholesTemplateInserter
{
    public static string Execute(dynamic excelApp)
    {
        dynamic worksheet = excelApp.ActiveSheet;
        dynamic selection = excelApp.Selection;

        int startRow = selection.Row;
        int startCol = selection.Column;

        // ── BS 模型参数行 ──
        var labels = new (string Label, string Value, string? Format)[]
        {
            ("Black-Scholes 期权定价模型", "", null),
            ("", "", null),
            ("输入参数", "", null),
            ("标的资产现价 (S)", "100", "#,##0.00"),
            ("行权价 (K)", "100", "#,##0.00"),
            ("无风险利率 (r)", "5%", "0.00%"),
            ("波动率 (σ)", "20%", "0.00%"),
            ("到期时间 (T, 年)", "1", "0.00"),
            ("股息率 (q)", "0%", "0.00%"),
            ("", "", null),
            ("期权类型", "Call", null),
            ("", "", null),
            ("中间计算", "", null),
            ("d₁", "", "0.0000"),
            ("d₂", "", "0.0000"),
            ("N(d₁)", "", "0.0000"),
            ("N(d₂)", "", "0.0000"),
            ("N(-d₁)", "", "0.0000"),
            ("N(-d₂)", "", "0.0000"),
            ("", "", null),
            ("定价结果", "", null),
            ("Call 期权价格", "", "#,##0.0000"),
            ("Put 期权价格", "", "#,##0.0000"),
            ("", "", null),
            ("Greeks", "", null),
            ("Delta (Call)", "", "0.0000"),
            ("Delta (Put)", "", "0.0000"),
            ("Gamma", "", "0.0000"),
            ("Vega", "", "0.0000"),
            ("Theta (Call)", "", "0.0000"),
            ("Theta (Put)", "", "0.0000"),
            ("Rho (Call)", "", "0.0000"),
            ("Rho (Put)", "", "0.0000"),
        };

        // BS 公式引用（基于 B 列输入）
        // B4=S, B5=K, B6=r, B7=σ, B8=T, B9=q, B11=Call/Put
        // d₁: =(LN(B4/B5)+(B6-B9+B7^2/2)*B8)/(B7*SQRT(B8))
        // d₂: =B14-B7*SQRT(B8)
        // N(d₁): =NORM.S.DIST(B14,TRUE)
        // N(d₂): =NORM.S.DIST(B15,TRUE)

        var formulas = new Dictionary<int, string>
        {
            { 14, "=(LN(B4/B5)+(B6-B9+B7^2/2)*B8)/(B7*SQRT(B8))" },           // d₁
            { 15, "=B14-B7*SQRT(B8)" },                                         // d₂
            { 16, "=NORM.S.DIST(B14,TRUE)" },                                   // N(d₁)
            { 17, "=NORM.S.DIST(B15,TRUE)" },                                   // N(d₂)
            { 18, "=NORM.S.DIST(-B14,TRUE)" },                                  // N(-d₁)
            { 19, "=NORM.S.DIST(-B15,TRUE)" },                                  // N(-d₂)
            { 22, "=IF(B11=\"Call\",B4*EXP(-B9*B8)*B16-B5*EXP(-B6*B8)*B17,B5*EXP(-B6*B8)*B19-B4*EXP(-B9*B8)*B18)" }, // Call
            { 23, "=IF(B11=\"Call\",B5*EXP(-B6*B8)*B19-B4*EXP(-B9*B8)*B18,B4*EXP(-B9*B8)*B16-B5*EXP(-B6*B8)*B17)" }, // Put
            { 26, "=EXP(-B9*B8)*B16" },                                        // Delta Call
            { 27, "=EXP(-B9*B8)*(B16-1)" },                                    // Delta Put
            { 28, "=EXP(-B9*B8)*NORM.S.DIST(B14,FALSE)/(B4*B7*SQRT(B8))" },   // Gamma (simplified using PDF)
            { 29, "=B4*EXP(-B9*B8)*NORM.S.DIST(B14,FALSE)*SQRT(B8)" },        // Vega (per 1% = need /100)
            { 30, "=-B4*EXP(-B9*B8)*NORM.S.DIST(B14,FALSE)*B7/(2*SQRT(B8))-B6*B5*EXP(-B6*B8)*B17+B9*B4*EXP(-B9*B8)*B16" }, // Theta Call
            { 31, "=-B4*EXP(-B9*B8)*NORM.S.DIST(B14,FALSE)*B7/(2*SQRT(B8))+B6*B5*EXP(-B6*B8)*B19-B9*B4*EXP(-B9*B8)*B18" }, // Theta Put
            { 32, "=B5*B8*EXP(-B6*B8)*B17" },                                  // Rho Call
            { 33, "=-B5*B8*EXP(-B6*B8)*B19" },                                 // Rho Put
        };

        // ── 写入模板 ──
        // 标题
        worksheet.Cells[startRow, startCol].Value = labels[0].Label;
        worksheet.Cells[startRow, startCol].Font.Bold = true;
        worksheet.Cells[startRow, startCol].Font.Size = 14;

        for (int i = 0; i < labels.Length; i++)
        {
            int rowIdx = startRow + 1 + i;

            // B列: 标签
            dynamic labelCell = worksheet.Cells[rowIdx, startCol + 1];
            labelCell.Value = labels[i].Label;

            if (labels[i].Label is "输入参数" or "中间计算" or "定价结果" or "Greeks")
            {
                labelCell.Font.Bold = true;
                labelCell.Interior.ColorIndex = 36; // 浅黄
            }

            // C列: 值/公式
            dynamic valueCell = worksheet.Cells[rowIdx, startCol + 2];
            if (formulas.TryGetValue(i, out var formula))
            {
                valueCell.Formula = formula;
            }
            else if (!string.IsNullOrEmpty(labels[i].Value))
            {
                valueCell.Value = labels[i].Value;
            }

            if (labels[i].Format != null)
            {
                valueCell.NumberFormat = labels[i].Format;
            }

            // 输入行背景色
            if (i >= 3 && i <= 8)
            {
                valueCell.Interior.ColorIndex = 36; // 浅黄 = 用户输入区
            }
        }

        // 列宽
        worksheet.Columns[startCol + 1].ColumnWidth = 22;
        worksheet.Columns[startCol + 2].ColumnWidth = 16;

        return $"Black-Scholes 期权定价模板已插入到 {worksheet.Name}!B{startRow + 1}。" +
               "请在黄色区域填写参数。Call/Put 期权价格会自动计算。";
    }
}
