namespace ModelForge.Sidecar.Linking;

/// <summary>
/// Excel → PowerPoint 链接器。
/// 将 Excel 选中区域或图表复制为 OLE 链接对象粘贴到 PowerPoint 幻灯片。
/// </summary>
public static class ExcelToPowerPointLinker
{
    /// <summary>
    /// 将 Excel Range 链接到 PowerPoint 指定幻灯片。
    /// </summary>
    /// <param name="excelApp">Excel Application</param>
    /// <param name="pptApp">PowerPoint Application。若为 null 则自动尝试连接。</param>
    /// <param name="slideIndex">幻灯片序号（1-based）。默认追加到末尾。</param>
    /// <returns>操作结果描述。</returns>
    public static string LinkRange(dynamic excelApp, dynamic? pptApp = null, int slideIndex = -1)
    {
        // Connect to PowerPoint
        if (pptApp == null)
        {
            try { pptApp = Interop.ComRuntime.GetActiveObject(Interop.ComRuntime.CLSID.PowerPoint); }
            catch (Exception ex)
            {
                return $"无法连接到 PowerPoint：{ex.Message}。请确认 PowerPoint 已启动。";
            }
        }

        if (pptApp == null) return "PowerPoint 未运行。";

        dynamic selection = excelApp.Selection;
        string rangeAddr = selection.Address;
        dynamic workbook = excelApp.ActiveWorkbook;
        string sheetName = excelApp.ActiveSheet.Name;

        // 复制 Excel Range
        selection.Copy();

        // 获取目标幻灯片
        dynamic presentation = pptApp.ActivePresentation;
        if (presentation == null) return "PowerPoint 中没有打开的演示文稿。";

        dynamic slide;
        if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
        {
            // 追加到末尾
            slide = presentation.Slides.Add(presentation.Slides.Count + 1, 12); // ppLayoutBlank = 12
        }
        else
        {
            slide = presentation.Slides[slideIndex];
        }

        // 粘贴为 OLE 链接对象
        dynamic shape = slide.Shapes.PasteSpecial(
            0,  // ppPasteDefault = 0
            1,  // ppPasteOLEObject = 1
            link: true);

        if (shape != null)
        {
            string shapeName = shape.Name;
            return $"Range '{workbook.Name}!{sheetName}!{rangeAddr}' 已链接到 " +
                   $"'{presentation.Name}' Slide {slide.SlideIndex}，形状: {shapeName}。";
        }

        return "粘贴链接失败。请确认 Excel 和 PowerPoint 版本兼容。";
    }

    /// <summary>
    /// 将 Excel Chart 链接到 PowerPoint。
    /// </summary>
    public static string LinkChart(dynamic excelApp, dynamic? pptApp = null, string? chartName = null, int slideIndex = -1)
    {
        if (pptApp == null)
        {
            try { pptApp = Interop.ComRuntime.GetActiveObject(Interop.ComRuntime.CLSID.PowerPoint); }
            catch (Exception ex) { return $"无法连接到 PowerPoint：{ex.Message}。"; }
        }

        if (pptApp == null) return "PowerPoint 未运行。";

        dynamic workbook = excelApp.ActiveWorkbook;
        dynamic worksheet = excelApp.ActiveSheet;

        // 选择图表
        dynamic chart;
        if (!string.IsNullOrWhiteSpace(chartName))
        {
            chart = worksheet.ChartObjects(chartName);
        }
        else
        {
            // 尝试使用当前选中的图表
            dynamic selection = excelApp.Selection;
            try
            {
                // ChartObjects 或 HasChart 属性检测
                if ((bool)selection.HasChart)
                {
                    chart = selection;
                }
                else
                {
                    return "未选中图表。请先点击一个图表或指定 chartName 参数。";
                }
            }
            catch
            {
                return "未选中图表。请先点击一个图表或指定 chartName 参数。";
            }
        }

        chart.Copy();

        dynamic presentation = pptApp.ActivePresentation;
        if (presentation == null) return "PowerPoint 中没有打开的演示文稿。";

        dynamic slide;
        if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            slide = presentation.Slides.Add(presentation.Slides.Count + 1, 12);
        else
            slide = presentation.Slides[slideIndex];

        dynamic shape = slide.Shapes.PasteSpecial(0, 1, link: true);

        if (shape != null)
            return $"图表已链接到 '{presentation.Name}' Slide {slide.SlideIndex}，形状: {shape.Name}。";

        return "图表链接粘贴失败。";
    }
}
