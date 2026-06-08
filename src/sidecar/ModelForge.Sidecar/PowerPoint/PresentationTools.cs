namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// Presentation tools — new, templates, layout, export.
/// </summary>
public static class PresentationTools
{
    /// <summary>从 .potx 模板创建新演示文稿。</summary>
    public static string NewFromTemplate(dynamic pptApp, string? templatePath = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(templatePath))
            {
                if (!File.Exists(templatePath))
                    return $"模板文件不存在: {templatePath}";

                pptApp.Presentations.Add(templatePath);
                return $"已从模板创建新演示文稿: {Path.GetFileName(templatePath)}";
            }

            // 无模板：创建空白演示文稿
            pptApp.Presentations.Add();
            return "已创建空白演示文稿。";
        }
        catch (Exception ex)
        {
            return $"创建演示文稿失败: {ex.Message}";
        }
    }

    /// <summary>列出指定目录中的 .potx 模板文件。</summary>
    public static string ListTemplates(string? searchPath = null)
    {
        var dir = searchPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModelForge", "Templates", "PPT");

        if (!Directory.Exists(dir))
            return System.Text.Json.JsonSerializer.Serialize(new { path = dir, templates = Array.Empty<object>() });

        var templates = Directory.GetFiles(dir, "*.potx")
            .Select(f => new
            {
                name = Path.GetFileNameWithoutExtension(f),
                path = f,
                size = new FileInfo(f).Length,
                modified = File.GetLastWriteTime(f)
            })
            .ToArray();

        return System.Text.Json.JsonSerializer.Serialize(new { path = dir, count = templates.Length, templates });
    }

    /// <summary>Apply a specific slide layout by name or index.</summary>
    public static string ApplyLayout(dynamic pptApp, string? layoutName = null, int? layoutIndex = null)
    {
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        dynamic presentation = pptApp.ActivePresentation;
        dynamic master = slide.Design;

        if (layoutIndex.HasValue && layoutIndex > 0)
        {
            try
            {
                dynamic layout = master.SlideMaster.CustomLayouts[layoutIndex.Value];
                slide.CustomLayout = layout;
                return $"Applied layout #{layoutIndex}: {layout.Name}";
            }
            catch { return $"Layout index {layoutIndex} not found."; }
        }

        if (!string.IsNullOrWhiteSpace(layoutName))
        {
            foreach (dynamic layout in master.SlideMaster.CustomLayouts)
            {
                if (string.Equals((string)layout.Name, layoutName, StringComparison.OrdinalIgnoreCase))
                {
                    slide.CustomLayout = layout;
                    return $"Applied layout: {layout.Name}";
                }
            }
            return $"Layout '{layoutName}' not found.";
        }

        // List available layouts
        var layouts = new List<string>();
        int idx = 1;
        foreach (dynamic layout in master.SlideMaster.CustomLayouts)
        {
            layouts.Add($"{idx}: {(string)layout.Name}");
            idx++;
        }
        return "Available layouts:\n" + string.Join("\n", layouts);
    }

    /// <summary>将所有幻灯片导出为 PNG 图片。</summary>
    public static string ExportToImages(dynamic pptApp, string? outputDir = null)
    {
        var dir = outputDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "PPT_Export_" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        Directory.CreateDirectory(dir);
        dynamic presentation = pptApp.ActivePresentation;

        foreach (dynamic slide in presentation.Slides)
        {
            try
            {
                string fileName = $"Slide_{slide.SlideIndex:D3}.png";
                slide.Export(Path.Combine(dir, fileName), "PNG", 1920, 1080);
            }
            catch { }
        }

        return $"Exported {presentation.Slides.Count} slides to: {dir}";
    }

    /// <summary>Duplicate the current slide.</summary>
    public static string DuplicateSlide(dynamic pptApp)
    {
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        slide.Duplicate();
        return $"Duplicated slide {slide.SlideIndex}.";
    }

    /// <summary>Set slide background to a solid color.</summary>
    public static string SetBackgroundColor(dynamic pptApp, string? colorHex = null)
    {
        var hex = colorHex ?? "1F3A5F";
        hex = hex.TrimStart('#');
        int rgb = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);

        dynamic slide = pptApp.ActiveWindow.View.Slide;
        slide.FollowMasterBackground = 0; // msoFalse
        slide.Background.Fill.ForeColor.RGB = rgb;
        slide.Background.Fill.Visible = -1;

        return $"Set slide background to #{hex.ToUpper()}.";
    }

    /// <summary>Apply fade transition to all slides.</summary>
    public static string ApplyTransitionFade(dynamic pptApp)
    {
        dynamic presentation = pptApp.ActivePresentation;
        foreach (dynamic slide in presentation.Slides)
        {
            try
            {
                slide.SlideShowTransition.EntryEffect = 10; // ppEffectFade
                slide.SlideShowTransition.Duration = 1.0f;
            }
            catch { }
        }
        return $"Applied fade transition to {presentation.Slides.Count} slides.";
    }

    /// <summary>Apply push transition to all slides.</summary>
    public static string ApplyTransitionPush(dynamic pptApp)
    {
        dynamic presentation = pptApp.ActivePresentation;
        foreach (dynamic slide in presentation.Slides)
        {
            try
            {
                slide.SlideShowTransition.EntryEffect = 18; // ppEffectPushLeft
                slide.SlideShowTransition.Duration = 0.8f;
            }
            catch { }
        }
        return $"Applied push transition to {presentation.Slides.Count} slides.";
    }

    /// <summary>Reset slide background to follow master.</summary>
    public static string ResetBackground(dynamic pptApp)
    {
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        slide.FollowMasterBackground = -1; // msoTrue
        return "Reset slide background to master.";
    }

    /// <summary>Send selected shape to front (z-order).</summary>
    public static string SendToFront(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "Please select a shape first.";
        selection.ShapeRange.ZOrder(0); // msoBringToFront
        return "Sent shape to front.";
    }

    /// <summary>Send selected shape to back (z-order).</summary>
    public static string SendToBack(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "Please select a shape first.";
        selection.ShapeRange.ZOrder(1); // msoSendToBack
        return "Sent shape to back.";
    }

    /// <summary>Move current slide to a new position.</summary>
    public static string MoveSlide(dynamic pptApp, int toIndex)
    {
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        int fromIndex = slide.SlideIndex;
        slide.MoveTo(toIndex);
        return $"Moved slide from {fromIndex} to {toIndex}.";
    }
}
