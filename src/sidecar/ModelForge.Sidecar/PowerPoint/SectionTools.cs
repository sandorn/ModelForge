using System.Text.Json;

namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// PPT Section 管理 — 添加、重命名、移动、删除节。
/// </summary>
public static class SectionTools
{
    /// <summary>列出所有 Section。</summary>
    public static string ListSections(dynamic pptApp)
    {
        dynamic presentation = pptApp.ActivePresentation;
        var sections = new List<object>();

        try
        {
            dynamic sp = presentation.SectionProperties;
            int count = sp.Count;
            for (int i = 1; i <= count; i++)
            {
                sections.Add(new
                {
                    index = i,
                    name = (string)sp.Name(i),
                    firstSlide = (int)sp.FirstSlide(i),
                    slideCount = (int)sp.SlidesCount(i)
                });
            }
        }
        catch { }

        return JsonSerializer.Serialize(new { count = sections.Count, sections });
    }

    /// <summary>在当前幻灯片位置添加 Section。</summary>
    public static string AddSection(dynamic pptApp, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
            return "请提供节名称 (sectionName 参数)。";

        dynamic presentation = pptApp.ActivePresentation;
        int slideIndex = pptApp.ActiveWindow.View.Slide.SlideIndex;

        try
        {
            presentation.SectionProperties.AddBeforeSlide(slideIndex, sectionName);
            return $"已在 Slide {slideIndex} 前添加节 '{sectionName}'。";
        }
        catch (Exception ex)
        {
            return $"添加节失败: {ex.Message}";
        }
    }

    /// <summary>重命名指定 Section。</summary>
    public static string RenameSection(dynamic pptApp, int sectionIndex, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return "请提供新名称 (newName 参数)。";

        dynamic presentation = pptApp.ActivePresentation;
        try
        {
            presentation.SectionProperties.Name(sectionIndex, newName);
            return $"已将节 {sectionIndex} 重命名为 '{newName}'。";
        }
        catch (Exception ex)
        {
            return $"重命名节失败: {ex.Message}";
        }
    }

    /// <summary>删除指定 Section（保留幻灯片）。</summary>
    public static string DeleteSection(dynamic pptApp, int sectionIndex)
    {
        dynamic presentation = pptApp.ActivePresentation;
        try
        {
            presentation.SectionProperties.Delete(sectionIndex, false);
            return $"已删除节 {sectionIndex}（幻灯片已保留）。";
        }
        catch (Exception ex)
        {
            return $"删除节失败: {ex.Message}";
        }
    }
}
