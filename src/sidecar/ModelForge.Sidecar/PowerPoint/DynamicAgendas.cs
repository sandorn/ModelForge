using System.Diagnostics;

namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// Dynamic Agendas — 读取 PPT Section 结构，自动生成/更新目录幻灯片。
/// </summary>
public static class DynamicAgendas
{
    public sealed class AgendaResult
    {
        public int SectionsFound { get; set; }
        public int SlidesGenerated { get; set; }
        public List<string> SectionTitles { get; } = new();
    }

    /// <summary>
    /// 扫描当前演示文稿的所有 Section，生成目录幻灯片（追加到第 2 张位置）。
    /// </summary>
    public static AgendaResult Generate(dynamic pptApp)
    {
        var result = new AgendaResult();
        dynamic presentation = pptApp.ActivePresentation;
        if (presentation == null)
        {
            Debug.WriteLine("PPT: 无活动演示文稿");
            return result;
        }

        // 收集 Section 信息
        var sections = new List<(string Name, int FirstSlideIndex)>();
        try
        {
            foreach (dynamic section in presentation.SectionProperties)
            {
                string name = section.Name ?? $"Section {section.index}";
                int firstSlide = section.FirstSlideIndex;
                sections.Add((name, firstSlide));
                result.SectionTitles.Add(name);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PPT Section 扫描异常: {ex.Message}");
        }
        result.SectionsFound = sections.Count;

        if (sections.Count == 0) return result;

        // 删除旧目录幻灯片（如果存在）
        try
        {
            foreach (dynamic slide in presentation.Slides)
            {
                try
                {
                    if (slide.Name == "ModelForge_Agenda")
                    {
                        slide.Delete();
                        break;
                    }
                }
                catch { }
            }
        }
        catch { }

        // 在第 2 张位置创建目录幻灯片
        int agendaIndex = 2;
        dynamic agendaSlide = presentation.Slides.Add(agendaIndex, 1); // ppLayoutTitle = 1
        agendaSlide.Name = "ModelForge_Agenda";

        // 设置标题
        try
        {
            agendaSlide.Shapes[1].TextFrame.TextRange.Text = "目录 / Agenda";
        }
        catch { }

        // 添加文本框列出所有 Section
        float left = 72;  // 1 inch
        float top = 144;  // 2 inches
        float width = 600;
        float height = 16;

        foreach (var (name, index) in sections)
        {
            dynamic textBox = agendaSlide.Shapes.AddTextbox(1, left, top, width, height); // msoTextOrientationHorizontal
            textBox.TextFrame.TextRange.Text = name;
            textBox.TextFrame.TextRange.Font.Size = 14;
            top += 24;
        }

        result.SlidesGenerated = 1;
        return result;
    }
}
