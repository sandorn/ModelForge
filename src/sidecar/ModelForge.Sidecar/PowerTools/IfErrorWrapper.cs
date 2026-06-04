namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// IFERROR 封装。一键用 =IFERROR(original_formula, fallback) 包裹选区公式。
/// </summary>
public static class IfErrorWrapper
{
    /// <summary>
    /// 对选中区域中所有包含公式的单元格追加 IFERROR 包裹。
    /// </summary>
    /// <param name="excelApp">Excel Application (dynamic COM)</param>
    /// <param name="fallbackValue">IFERROR 的第二个参数，默认 "0"。</param>
    /// <returns>操作结果描述。</returns>
    public static string Execute(dynamic excelApp, string fallbackValue = "0")
    {
        dynamic selection = excelApp.Selection;
        int wrappedCount = 0;
        int skippedCount = 0;

        foreach (dynamic cell in selection)
        {
            // 跳过无公式的单元格
            if (!cell.HasFormula)
            {
                skippedCount++;
                continue;
            }

            string originalFormula = cell.Formula as string ?? string.Empty;

            // 跳过已包裹 IFERROR 的公式
            if (originalFormula.TrimStart().StartsWith("=IFERROR(", StringComparison.OrdinalIgnoreCase))
            {
                skippedCount++;
                continue;
            }

            // 移除前导 "=" 以获取公式体
            string formulaBody = originalFormula.StartsWith("=")
                ? originalFormula.Substring(1)
                : originalFormula;

            // 构建 =IFERROR(formulaBody, fallback)
            cell.Formula = $"=IFERROR({formulaBody},{fallbackValue})";
            wrappedCount++;
        }

        return $"IFERROR 封装完成：{wrappedCount} 个公式已包裹，{skippedCount} 个单元格已跳过。";
    }
}
