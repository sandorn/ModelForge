namespace ModelForge.Sidecar.Linking;

/// <summary>
/// 链接刷新引擎。刷新 Excel ↔ PowerPoint/Word 之间的 OLE 链接。
/// </summary>
public static class LinkRefresher
{
    public sealed class RefreshResult
    {
        public int TotalLinks { get; set; }
        public int Refreshed { get; set; }
        public int Broken { get; set; }
        public List<string> BrokenDetails { get; } = new();
    }

    /// <summary>
    /// 刷新当前 PowerPoint 演示文稿中所有 OLE 链接。
    /// </summary>
    public static RefreshResult RefreshPowerPointLinks()
    {
        var result = new RefreshResult();

        try
        {
            dynamic pptApp = Interop.ComRuntime.GetActiveObject(Interop.ComRuntime.CLSID.PowerPoint);
            dynamic presentation = pptApp.ActivePresentation;
            if (presentation == null)
            {
                result.BrokenDetails.Add("PowerPoint 中没有打开的演示文稿。");
                return result;
            }

            // 遍历所有幻灯片的所有形状
            foreach (dynamic slide in presentation.Slides)
            {
                foreach (dynamic shape in slide.Shapes)
                {
                    // OLE 链接对象有 LinkFormat 属性
                    try
                    {
                        dynamic linkFormat = shape.LinkFormat;
                        if (linkFormat != null)
                        {
                            result.TotalLinks++;
                            try
                            {
                                linkFormat.Update();
                                result.Refreshed++;
                            }
                            catch
                            {
                                result.Broken++;
                                result.BrokenDetails.Add(
                                    $"Slide {slide.SlideIndex}: '{shape.Name}' ({linkFormat.SourceFullName ?? "未知源"})");
                            }
                        }
                    }
                    catch
                    {
                        // 形状没有 LinkFormat — 不是 OLE 链接
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.BrokenDetails.Add($"PowerPoint 连接失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 刷新当前 Excel 工作簿中所有外部链接。
    /// </summary>
    public static RefreshResult RefreshExcelLinks(dynamic excelApp)
    {
        var result = new RefreshResult();

        try
        {
            dynamic workbook = excelApp.ActiveWorkbook;

            // 使用 Excel 的 LinkSources 枚举外部链接
            try
            {
                dynamic linkSources = workbook.LinkSources(1); // xlExcelLinks
                if (linkSources != null)
                {
                    foreach (dynamic link in linkSources)
                    {
                        result.TotalLinks++;
                        try
                        {
                            workbook.UpdateLink(link, 1); // xlExcelLinks
                            result.Refreshed++;
                        }
                        catch
                        {
                            result.Broken++;
                            result.BrokenDetails.Add($"外部链接断开: {link}");
                        }
                    }
                }
            }
            catch
            {
                // 无外部链接
            }

            // OLE 链接
            try
            {
                dynamic oleLinks = workbook.LinkSources(2); // xlOLELinks
                if (oleLinks != null)
                {
                    foreach (dynamic link in oleLinks)
                    {
                        result.TotalLinks++;
                        try
                        {
                            workbook.UpdateLink(link, 2);
                            result.Refreshed++;
                        }
                        catch
                        {
                            result.Broken++;
                            result.BrokenDetails.Add($"OLE 链接断开: {link}");
                        }
                    }
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            result.BrokenDetails.Add($"Excel 链接刷新失败: {ex.Message}");
        }

        return result;
    }
}
