namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 快速图表插入器。基于选中数据区域插入柱状图或折线图。
/// </summary>
public static class ChartInserter
{
    /// <summary>插入簇状柱形图。</summary>
    public static string InsertColumnChart(dynamic excelApp, string? chartTitle = null)
    {
        return InsertChart(excelApp, 51, chartTitle ?? "柱状图"); // xlColumnClustered = 51
    }

    /// <summary>插入折线图。</summary>
    public static string InsertLineChart(dynamic excelApp, string? chartTitle = null)
    {
        return InsertChart(excelApp, 4, chartTitle ?? "折线图"); // xlLine = 4
    }

    private static string InsertChart(dynamic excelApp, int chartType, string label)
    {
        dynamic worksheet = excelApp.ActiveSheet;
        dynamic selection = excelApp.Selection;

        // 检查是否有足够数据
        try
        {
            if (selection.Rows.Count < 2 && selection.Columns.Count < 2)
                return "请选中至少包含标题行和一列数据的区域来创建图表。";
        }
        catch { return "请先选中数据区域再插入图表。"; }

        // 确定图表位置（选中区域右侧或下方）
        int chartLeft = selection.Left + selection.Width + 20;
        int chartTop = selection.Top;
        int chartWidth = 480;
        int chartHeight = 300;

        // 添加图表
        dynamic chartObj = worksheet.ChartObjects().Add(chartLeft, chartTop, chartWidth, chartHeight);
        dynamic chart = chartObj.Chart;
        chart.ChartType = chartType;
        chart.SetSourceData(selection);

        // 设置标题
        try
        {
            chart.HasTitle = true;
            chart.ChartTitle.Text = label;
            chart.ChartTitle.Font.Size = 14;
            chart.ChartTitle.Font.Bold = true;
        }
        catch { }

        // 设置图表样式
        try { chart.ChartStyle = 2; } catch { }

        return $"Inserted {label} on {worksheet.Name} (data: {selection.Address}).";
    }

    public static string UnifyChartSizes(dynamic excelApp, float? width = null, float? height = null)
    {
        float w = width ?? 480, h = height ?? 300;
        int count = 0;
        dynamic workbook = excelApp.ActiveWorkbook;
        foreach (dynamic sheet in workbook.Worksheets)
        {
            try { foreach (dynamic co in sheet.ChartObjects()) { co.Width = w; co.Height = h; count++; } } catch { }
        }
        return $"Unified {count} charts to {w}x{h}.";
    }

    public static string UnifyChartStyles(dynamic excelApp, int? chartStyle = null)
    {
        int style = Math.Clamp(chartStyle ?? 2, 1, 48);
        int count = 0;
        dynamic workbook = excelApp.ActiveWorkbook;
        foreach (dynamic sheet in workbook.Worksheets)
        {
            try { foreach (dynamic co in sheet.ChartObjects()) { co.Chart.ChartStyle = style; count++; } } catch { }
        }
        return $"Applied style #{style} to {count} charts.";
    }

    public static string AddSeriesToChart(dynamic excelApp, string? seriesName = null)
    {
        dynamic selection = excelApp.Selection;
        dynamic worksheet = excelApp.ActiveSheet;
        dynamic? chart = null;
        try
        {
            foreach (dynamic co in worksheet.ChartObjects())
                try { if (co.Chart.SeriesCollection().Count > 0) { chart = co.Chart; break; } } catch { }
        }
        catch { }
        if (chart == null) return "No chart found on active sheet.";

        dynamic series = chart.SeriesCollection().NewSeries();
        series.Values = selection;
        series.Name = string.IsNullOrWhiteSpace(seriesName)
            ? $"Series {chart.SeriesCollection().Count}" : seriesName;
        return $"Added series '{series.Name}' to chart (data: {selection.Address}).";
    }
}
