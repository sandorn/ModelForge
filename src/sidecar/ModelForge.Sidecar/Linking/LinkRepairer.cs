namespace ModelForge.Sidecar.Linking;

/// <summary>
/// 链接修复器 — 诊断并尝试修复失效的 Excel↔PowerPoint/Word 链接。
/// </summary>
public static class LinkRepairer
{
    public sealed class RepairResult
    {
        public int Diagnosed { get; set; }
        public int Repaired { get; set; }
        public int Failed { get; set; }
        public List<string> Actions { get; } = new();
        public List<RepairSuggestion> Suggestions { get; } = new();
    }

    public sealed class RepairSuggestion
    {
        public string LinkId { get; init; } = string.Empty;
        public string Issue { get; init; } = string.Empty;
        public string? Fix { get; init; }
        public bool Repaired { get; init; }
    }

    /// <summary>
    /// 诊断并尝试修复 PowerPoint 中的失效链接。
    /// </summary>
    public static RepairResult RepairPowerPointLinks(dynamic pptApp, IEnumerable<LinkRefreshPlanner.PowerPointTarget> targets)
    {
        var result = new RepairResult();
        dynamic presentation = pptApp.ActivePresentation;

        foreach (var target in targets)
        {
            result.Diagnosed++;
            try
            {
                dynamic? shape = FindShapeRobust(presentation, target, result);
                if (shape != null)
                {
                    try
                    {
                        dynamic linkFormat = shape.LinkFormat;
                        if (linkFormat != null)
                        {
                            linkFormat.Update();
                            result.Repaired++;
                            result.Actions.Add($"[已修复] {target.LinkId}: 在 Slide {target.SlideIndex} 定位到形状 '{shape.Name}'，链接已刷新。");
                            result.Suggestions.Add(new RepairSuggestion
                            {
                                LinkId = target.LinkId,
                                Issue = "目标形状定位成功",
                                Fix = $"Slide {target.SlideIndex} -> {shape.Name}",
                                Repaired = true
                            });
                            continue;
                        }
                    }
                    catch
                    {
                        // 形状找到但无法刷新链接
                    }
                }

                result.Failed++;
                result.Suggestions.Add(new RepairSuggestion
                {
                    LinkId = target.LinkId,
                    Issue = $"无法在 Slide {target.SlideIndex} 定位目标形状",
                    Fix = "建议：手动检查演示文稿中是否存在该形状，或重新创建链接。"
                });
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Suggestions.Add(new RepairSuggestion
                {
                    LinkId = target.LinkId,
                    Issue = $"诊断异常: {ex.Message}"
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 尝试修复 Excel 外部链接：搜索常见路径中的源文件。
    /// </summary>
    public static RepairResult RepairExcelLinks(dynamic excelApp)
    {
        var result = new RepairResult();
        dynamic workbook = excelApp.ActiveWorkbook;
        string workbookDir = Path.GetDirectoryName(workbook.FullName) ?? ".";

        try
        {
            dynamic linkSources = workbook.LinkSources(1); // xlExcelLinks
            if (linkSources != null)
            {
                foreach (dynamic link in linkSources)
                {
                    result.Diagnosed++;
                    string linkPath = link?.ToString() ?? "";
                    string fileName = Path.GetFileName(linkPath);

                    // 检查原路径是否存在
                    if (File.Exists(linkPath))
                    {
                        // 原路径存在但链接可能过期，尝试更新
                        try { workbook.UpdateLink(link, 1); result.Repaired++; result.Actions.Add($"[已刷新] {linkPath}"); }
                        catch { result.Failed++; result.Suggestions.Add(new RepairSuggestion { LinkId = linkPath, Issue = "原文件存在但刷新失败", Fix = "检查文件是否被其他进程占用。" }); }
                        continue;
                    }

                    // 在同目录搜索同名文件
                    var found = SearchForFile(fileName, workbookDir);
                    if (found != null)
                    {
                        try
                        {
                            workbook.ChangeLink(linkPath, found, 1);
                            workbook.UpdateLink(found, 1);
                            result.Repaired++;
                            result.Actions.Add($"[已修复] {linkPath} -> {found}");
                            result.Suggestions.Add(new RepairSuggestion { LinkId = linkPath, Issue = "原文件已移动", Fix = $"重新定位到: {found}", Repaired = true });
                        }
                        catch
                        {
                            result.Failed++;
                            result.Suggestions.Add(new RepairSuggestion { LinkId = linkPath, Issue = "找到候选文件但无法重定向", Fix = $"候选文件: {found}，请手动检查。" });
                        }
                    }
                    else
                    {
                        result.Failed++;
                        result.Suggestions.Add(new RepairSuggestion { LinkId = linkPath, Issue = "源文件未找到", Fix = $"在 {workbookDir} 及子目录中未找到 {fileName}。" });
                    }
                }
            }
        }
        catch { }

        return result;
    }

    private static dynamic? FindShapeRobust(dynamic presentation, LinkRefreshPlanner.PowerPointTarget target, RepairResult result)
    {
        // 方法 1: 按 SlideIndex + ShapeIndex
        if (target.SlideIndex.HasValue && target.ShapeIndex.HasValue)
        {
            try
            {
                dynamic slide = presentation.Slides[target.SlideIndex.Value];
                return slide.Shapes[target.ShapeIndex.Value];
            }
            catch { }
        }

        // 方法 2: 按 SlideIndex + ShapeName
        if (target.SlideIndex.HasValue && !string.IsNullOrWhiteSpace(target.ShapeName))
        {
            try
            {
                dynamic slide = presentation.Slides[target.SlideIndex.Value];
                foreach (dynamic shape in slide.Shapes)
                {
                    if (string.Equals(Convert.ToString(shape.Name), target.ShapeName, StringComparison.OrdinalIgnoreCase))
                        return shape;
                }
            }
            catch { }
        }

        // 方法 3: 搜索所有幻灯片的同名形状
        if (!string.IsNullOrWhiteSpace(target.ShapeName))
        {
            foreach (dynamic slide in presentation.Slides)
            {
                foreach (dynamic shape in slide.Shapes)
                {
                    if (string.Equals(Convert.ToString(shape.Name), target.ShapeName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Actions.Add($"[重定位] {target.LinkId}: 形状 '{target.ShapeName}' 从 Slide {target.SlideIndex} 移动到 Slide {slide.SlideIndex}");
                        return shape;
                    }
                }
            }
        }

        return null;
    }

    private static string? SearchForFile(string fileName, string startDir)
    {
        // 搜索当前目录
        var candidate = Path.Combine(startDir, fileName);
        if (File.Exists(candidate)) return candidate;

        // 搜索子目录（一层深度）
        try
        {
            foreach (var dir in Directory.GetDirectories(startDir))
            {
                candidate = Path.Combine(dir, fileName);
                if (File.Exists(candidate)) return candidate;
            }
        }
        catch { }

        // 搜索上级目录
        try
        {
            var parentDir = Directory.GetParent(startDir)?.FullName;
            if (parentDir != null)
            {
                candidate = Path.Combine(parentDir, fileName);
                if (File.Exists(candidate)) return candidate;
            }
        }
        catch { }

        return null;
    }
}
