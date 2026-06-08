namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// Logo 库 — 在幻灯片中插入企业 Logo。
/// </summary>
public static class LogoLibrary
{
    /// <summary>从文件路径插入 Logo 图片到当前幻灯片。</summary>
    public static string InsertLogo(dynamic pptApp, string logoPath,
        float? left = null, float? top = null, float? width = null, float? height = null)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
            return "请提供 Logo 文件路径 (logoPath 参数)。";

        if (!File.Exists(logoPath))
            return $"Logo 文件不存在: {logoPath}";

        var ext = Path.GetExtension(logoPath).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".svg"))
            return $"不支持的图片格式: {ext}。支持的格式: PNG, JPG, BMP, GIF, SVG。";

        try
        {
            dynamic slide = pptApp.ActiveWindow.View.Slide;
            float slideWidth = pptApp.ActivePresentation.PageSetup.SlideWidth;
            float insertLeft = left ?? slideWidth - 180;
            float insertTop = top ?? 10;
            float insertWidth = width ?? 120;
            float insertHeight = height ?? 40;

            dynamic shape = slide.Shapes.AddPicture(
                logoPath,
                0, // msoFalse = link to file
                -1, // msoTrue = save with document
                insertLeft, insertTop, insertWidth, insertHeight);

            return $"已插入 Logo: {Path.GetFileName(logoPath)}（位置: {insertLeft},{insertTop}，尺寸: {insertWidth}×{insertHeight}）。";
        }
        catch (Exception ex)
        {
            return $"插入 Logo 失败: {ex.Message}";
        }
    }

    /// <summary>在当前幻灯片右下角插入 Logo。</summary>
    public static string InsertLogoBottomRight(dynamic pptApp, string logoPath)
    {
        dynamic presentation = pptApp.ActivePresentation;
        float slideWidth = presentation.PageSetup.SlideWidth;
        float slideHeight = presentation.PageSetup.SlideHeight;
        return InsertLogo(pptApp, logoPath, slideWidth - 150, slideHeight - 60, 120, 40);
    }

    /// <summary>为所有幻灯片批量添加 Logo。</summary>
    public static string AddLogoToAllSlides(dynamic pptApp, string logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath) || !File.Exists(logoPath))
            return "Logo 文件不存在。";

        dynamic presentation = pptApp.ActivePresentation;
        float slideWidth = presentation.PageSetup.SlideWidth;
        int count = 0;

        foreach (dynamic slide in presentation.Slides)
        {
            try
            {
                slide.Shapes.AddPicture(logoPath, 0, -1, slideWidth - 150, 10, 120, 40);
                count++;
            }
            catch { }
        }

        return $"已为 {count}/{presentation.Slides.Count} 张幻灯片添加 Logo。";
    }
}
