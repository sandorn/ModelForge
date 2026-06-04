namespace ModelForge.Sidecar.Formula;

/// <summary>
/// 公式前驱追踪器。使用 Excel 的 Range.DirectPrecedents 和 NavigateArrow。
/// </summary>
public static class PrecedentTracer
{
    /// <summary>
    /// 追踪当前选中单元格的所有直接前驱单元格。
    /// </summary>
    public static List<CellReference> TraceDirectPrecedents(dynamic excelApp)
    {
        var result = new List<CellReference>();
        dynamic selection = excelApp.Selection;

        try
        {
            dynamic precedents = selection.DirectPrecedents;
            foreach (dynamic area in precedents.Areas)
            {
                foreach (dynamic cell in area)
                {
                    result.Add(new CellReference
                    {
                        Address = cell.Address,
                        Value = cell.Value?.ToString(),
                        Formula = cell.HasFormula ? (cell.Formula as string) : null
                    });
                }
            }
        }
        catch
        {
            // 无前驱
        }

        return result;
    }
}

/// <summary>
/// 公式依赖追踪器。使用 Excel 的 Range.DirectDependents。
/// </summary>
public static class DependentTracer
{
    /// <summary>
    /// 追踪当前选中单元格的所有直接依赖单元格。
    /// </summary>
    public static List<CellReference> TraceDirectDependents(dynamic excelApp)
    {
        var result = new List<CellReference>();
        dynamic selection = excelApp.Selection;

        try
        {
            dynamic dependents = selection.DirectDependents;
            foreach (dynamic area in dependents.Areas)
            {
                foreach (dynamic cell in area)
                {
                    result.Add(new CellReference
                    {
                        Address = cell.Address,
                        Value = cell.Value?.ToString(),
                        Formula = cell.HasFormula ? (cell.Formula as string) : null
                    });
                }
            }
        }
        catch
        {
            // 无依赖
        }

        return result;
    }
}

/// <summary>
/// 追踪到的单元格引用。
/// </summary>
public sealed class CellReference
{
    public string Address { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Formula { get; set; }
}
