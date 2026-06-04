namespace ModelForge.Sidecar.Visualizations;

/// <summary>
/// 单元格分类器。将选区中的单元格分为：硬编码(无公式)、公式、外部链接、空单元格。
/// </summary>
public static class CellClassifier
{
    public enum CellType { Empty, Hardcoded, Formula, ExternalLink }

    /// <summary>
    /// 扫描指定工作表区域，返回每个非空单元格的地址与类型。
    /// </summary>
    public static Dictionary<string, CellType> Classify(dynamic worksheet, dynamic range)
    {
        var result = new Dictionary<string, CellType>();

        foreach (dynamic cell in range)
        {
            string addr = cell.Address as string ?? "";
            if (string.IsNullOrEmpty(addr)) continue;

            object? val = cell.Value;
            bool hasFormula = cell.HasFormula;

            if (!hasFormula && (val == null || val is string s && string.IsNullOrWhiteSpace(s)))
            {
                // 空单元格跳过，不标记
                continue;
            }

            if (!hasFormula)
            {
                result[addr] = CellType.Hardcoded;
            }
            else
            {
                string formula = (cell.Formula as string) ?? "";
                bool isExternal = formula.Contains('[') || formula.Contains("'[")
                    || formula.Contains("!") && formula.Contains(":\\");
                result[addr] = isExternal ? CellType.ExternalLink : CellType.Formula;
            }
        }

        return result;
    }
}
