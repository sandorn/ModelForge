namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 正负号切换。将选中区域的数值乘以 -1，公式则包裹 = -(formula)。
/// 常用于快速翻转损益表符号方向。
/// </summary>
public static class ToggleSign
{
    /// <summary>
    /// 翻转选中区域中所有数值/公式的符号。
    /// </summary>
    public static string Execute(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        int toggledCount = 0;
        int skippedCount = 0;

        foreach (dynamic cell in selection)
        {
            if (cell.HasFormula)
            {
                string formula = cell.Formula as string ?? "";
                if (string.IsNullOrWhiteSpace(formula)) { skippedCount++; continue; }

                // 去掉前导 =
                string body = formula.TrimStart().StartsWith("=")
                    ? formula.TrimStart()[1..].TrimStart()
                    : formula.TrimStart();

                // 如果已经有一个外层的负号包裹，去掉它
                if (body.StartsWith("-(") && body.EndsWith(")"))
                {
                    cell.Formula = "=" + body[2..^1];
                }
                else
                {
                    cell.Formula = $"=-({body})";
                }
                toggledCount++;
            }
            else
            {
                // 数值：直接乘以 -1
                object? val = cell.Value;
                if (val != null && double.TryParse(val.ToString(), out double num))
                {
                    cell.Value = -num;
                    toggledCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
        }

        return $"正负号切换完成：{toggledCount} 个单元格已翻转，{skippedCount} 个跳过。";
    }
}
