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
            object? pptApp = Interop.ComRuntime.GetActiveObject(Interop.ComRuntime.CLSID.PowerPoint);
            return RefreshPowerPointLinks(pptApp);
        }
        catch (Exception ex)
        {
            result.BrokenDetails.Add($"PowerPoint 连接失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 按后端 LinkMetadata 指定的 PowerPoint 目标精准刷新；目标地址不足时回退全量扫描。
    /// </summary>
    public static RefreshResult RefreshPowerPointLinks(IEnumerable<LinkRefreshPlanner.PowerPointTarget> targets)
    {
        var result = new RefreshResult();

        try
        {
            object? pptApp = Interop.ComRuntime.GetActiveObject(Interop.ComRuntime.CLSID.PowerPoint);
            return RefreshPowerPointLinks(pptApp, targets);
        }
        catch (Exception ex)
        {
            result.BrokenDetails.Add($"PowerPoint 连接失败: {ex.Message}");
        }

        return result;
    }

    public static RefreshResult RefreshPowerPointLinks(object? pptApp, IEnumerable<LinkRefreshPlanner.PowerPointTarget>? targets = null)
    {
        var result = new RefreshResult();

        if (pptApp == null)
        {
            result.BrokenDetails.Add("无法连接到 PowerPoint。请确认 PowerPoint 已运行。");
            return result;
        }

        try
        {
            dynamic powerPoint = pptApp;
            dynamic presentation = powerPoint.ActivePresentation;
            if (presentation == null)
            {
                result.BrokenDetails.Add("PowerPoint 中没有打开的演示文稿。");
                return result;
            }

            var targetArray = targets?.ToArray() ?? Array.Empty<LinkRefreshPlanner.PowerPointTarget>();
            if (targetArray.Length > 0)
            {
                if (targetArray.Any(target => !target.IsPrecise))
                {
                    var fallback = RefreshAllPowerPointLinks(presentation);
                    fallback.BrokenDetails.Insert(0, "部分 PowerPoint 链接元数据缺少可定位的 targetAddress，已回退全量刷新。");
                    return fallback;
                }

                foreach (var target in targetArray)
                {
                    result.TotalLinks++;
                    string failure;
                    var shape = FindPowerPointShape(presentation, target, out failure);
                    if (shape == null)
                    {
                        result.Broken++;
                        result.BrokenDetails.Add($"Link {target.LinkId}: {failure}");
                        continue;
                    }

                    UpdatePowerPointShapeLink(shape, result, $"Link {target.LinkId}");
                }

                return result;
            }

            return RefreshAllPowerPointLinks(presentation);
        }
        catch (Exception ex)
        {
            result.BrokenDetails.Add($"PowerPoint 链接刷新失败: {ex.Message}");
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

    private static RefreshResult RefreshAllPowerPointLinks(dynamic presentation)
    {
        var result = new RefreshResult();

        foreach (dynamic slide in presentation.Slides)
        {
            foreach (dynamic shape in slide.Shapes)
            {
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
                }
            }
        }

        return result;
    }

    private static dynamic? FindPowerPointShape(
        dynamic presentation,
        LinkRefreshPlanner.PowerPointTarget target,
        out string failure)
    {
        failure = string.Empty;

        try
        {
            dynamic slide = presentation.Slides[target.SlideIndex!.Value];
            if (target.ShapeIndex.HasValue)
            {
                return slide.Shapes[target.ShapeIndex.Value];
            }

            foreach (dynamic shape in slide.Shapes)
            {
                if (string.Equals(Convert.ToString(shape.Name), target.ShapeName, StringComparison.OrdinalIgnoreCase))
                {
                    return shape;
                }
            }

            failure = $"未找到 PowerPoint 目标对象 {target.TargetAddress}。";
            return null;
        }
        catch (Exception ex)
        {
            failure = $"PowerPoint 目标对象定位失败 {target.TargetAddress}: {ex.Message}";
            return null;
        }
    }

    private static void UpdatePowerPointShapeLink(dynamic shape, RefreshResult result, string label)
    {
        try
        {
            dynamic linkFormat = shape.LinkFormat;
            if (linkFormat == null)
            {
                result.Broken++;
                result.BrokenDetails.Add($"{label}: 目标对象不是 OLE 链接。");
                return;
            }

            linkFormat.Update();
            result.Refreshed++;
        }
        catch (Exception ex)
        {
            result.Broken++;
            result.BrokenDetails.Add($"{label}: 链接刷新失败: {ex.Message}");
        }
    }
}
