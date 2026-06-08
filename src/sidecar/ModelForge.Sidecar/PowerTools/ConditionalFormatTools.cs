namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// Conditional formatting presets — heat maps, data bars, icon sets.
/// </summary>
public static class ConditionalFormatTools
{
    /// <summary>Apply 3-color heat map scale (green-yellow-red).</summary>
    public static string ApplyHeatMap(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        dynamic cf = selection.FormatConditions;
        cf.Delete();
        dynamic cs = cf.AddColorScale(3);
        cs.ColorScaleCriteria[1].Type = 2; // xlConditionValueLowestValue
        cs.ColorScaleCriteria[1].FormatColor.Color = 0x63BE7B; // green
        cs.ColorScaleCriteria[2].Type = 3; // xlConditionValuePercentile
        cs.ColorScaleCriteria[2].Value = 50;
        cs.ColorScaleCriteria[2].FormatColor.Color = 0xFFEB84; // yellow
        cs.ColorScaleCriteria[3].Type = 4; // xlConditionValueHighestValue
        cs.ColorScaleCriteria[3].FormatColor.Color = 0xF8696B; // red

        return $"Applied heat map color scale to {selection.Address}.";
    }

    /// <summary>Apply data bars to selection.</summary>
    public static string ApplyDataBars(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        dynamic cf = selection.FormatConditions;
        cf.Delete();
        dynamic db = cf.AddDatabar();
        db.BarColor.Color = 0x5B9BD5;
        db.ShowValue = true;

        return $"Applied data bars to {selection.Address}.";
    }

    /// <summary>Apply icon set (3 traffic lights).</summary>
    public static string ApplyIconSet(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        dynamic cf = selection.FormatConditions;
        cf.Delete();
        dynamic iset = cf.AddIconSetCondition();
        iset.IconSet = 4; // xl3TrafficLights1
        iset.ShowIconOnly = false;

        return $"Applied traffic light icon set to {selection.Address}.";
    }

    /// <summary>Apply top 10 highlight.</summary>
    public static string ApplyTop10(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        dynamic cf = selection.FormatConditions;
        cf.Delete();
        dynamic top = cf.AddTop10();
        top.TopBottom = 1; // xlTop10Top
        top.Rank = 10;
        top.Percent = false;
        top.Interior.Color = 0xA5D6A7;

        return $"Applied top 10 highlight to {selection.Address}.";
    }

    /// <summary>Clear all conditional formatting from selection.</summary>
    public static string ClearConditionalFormats(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        try { selection.FormatConditions.Delete(); } catch { }
        return $"Cleared conditional formatting from {selection.Address}.";
    }
}
