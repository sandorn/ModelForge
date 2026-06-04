using ModelForge.Sidecar.Visualizations;

namespace ModelForge.Sidecar.Visualizations;

/// <summary>
/// 审计标色器。根据 CellClassifier 的分类结果对单元格应用颜色标记。
/// 硬编码=蓝色, 公式=浅灰, 外部链接=绿色。
/// </summary>
public static class AuditColorMarker
{
    // ModelForge 审计色板
    private const int HardcodedColor = 0x0078D4;   // 蓝色 RGB(0,120,212)
    private const int FormulaColor = 0xF0F0F0;      // 浅灰 RGB(240,240,240)
    private const int ExternalLinkColor = 0x107C10; // 绿色 RGB(16,124,16)

    /// <summary>
    /// 根据分类结果对单元格着色。
    /// </summary>
    public static void Mark(dynamic worksheet, Dictionary<string, CellClassifier.CellType> classifications)
    {
        var hardcodedAddrs = classifications
            .Where(kv => kv.Value == CellClassifier.CellType.Hardcoded)
            .Select(kv => kv.Key).ToList();

        var formulaAddrs = classifications
            .Where(kv => kv.Value == CellClassifier.CellType.Formula)
            .Select(kv => kv.Key).ToList();

        var externalAddrs = classifications
            .Where(kv => kv.Value == CellClassifier.CellType.ExternalLink)
            .Select(kv => kv.Key).ToList();

        // 批量着色：将同类单元格合并为 Range 后一次性设置颜色
        if (hardcodedAddrs.Count > 0)
        {
            dynamic range = worksheet.Range[string.Join(",", hardcodedAddrs)];
            range.Interior.Color = HardcodedColor;
            range.Font.Color = 0xFFFFFF; // 白色字体
        }

        if (formulaAddrs.Count > 0)
        {
            dynamic range = worksheet.Range[string.Join(",", formulaAddrs)];
            range.Interior.Color = FormulaColor;
            range.Font.Color = 0x000000; // 黑色字体（浅灰背景可读）
        }

        if (externalAddrs.Count > 0)
        {
            dynamic range = worksheet.Range[string.Join(",", externalAddrs)];
            range.Interior.Color = ExternalLinkColor;
            range.Font.Color = 0xFFFFFF;
        }
    }

    /// <summary>
    /// 清除指定区域的所有审计标色。
    /// </summary>
    public static void Clear(dynamic range)
    {
        range.Interior.ColorIndex = 0; // xlNone
        range.Font.ColorIndex = 1;     // xlAutomatic
    }
}
