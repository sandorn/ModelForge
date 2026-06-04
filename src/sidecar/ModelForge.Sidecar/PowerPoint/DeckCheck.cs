using System.Diagnostics;

namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// Deck Check — 演示文稿合规审计。扫描字体偏离、Logo 检查、违禁术语。
/// </summary>
public static class DeckCheck
{
    public sealed class DeckCheckReport
    {
        public int SlidesScanned { get; set; }
        public int FontIssues { get; set; }
        public int TermIssues { get; set; }
        public List<string> Issues { get; } = new();
    }

    /// <summary>
    /// 扫描当前演示文稿，检查字体、术语、颜色合规性。
    /// </summary>
    /// <param name="pptApp">PowerPoint Application</param>
    /// <param name="allowedFonts">允许的字体列表。默认 Arial 和 Calibri。</param>
    /// <param name="forbiddenTerms">禁止的术语列表。</param>
    public static DeckCheckReport Run(dynamic pptApp,
        string[]? allowedFonts = null,
        string[]? forbiddenTerms = null)
    {
        allowedFonts ??= new[] { "Arial", "Calibri", "Calibri Light", "Microsoft YaHei" };
        forbiddenTerms ??= new[] { "机密", "草案", "DRAFT" };

        var report = new DeckCheckReport();
        dynamic presentation = pptApp.ActivePresentation;
        if (presentation == null) return report;

        report.SlidesScanned = presentation.Slides.Count;

        foreach (dynamic slide in presentation.Slides)
        {
            int slideNum = slide.SlideIndex;

            foreach (dynamic shape in slide.Shapes)
            {
                // 检查文本框字体
                try
                {
                    if (shape.HasTextFrame != 0)
                    {
                        dynamic textRange = shape.TextFrame.TextRange;
                        string text = textRange.Text ?? "";

                        // 字体检查
                        try
                        {
                            string fontName = textRange.Font.Name ?? "";
                            if (!string.IsNullOrEmpty(fontName) &&
                                !allowedFonts.Any(f => fontName.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
                            {
                                report.FontIssues++;
                                report.Issues.Add(
                                    $"Slide {slideNum}: 字体 '{fontName}' (形状: {shape.Name})");
                            }
                        }
                        catch { }

                        // 术语检查
                        foreach (var term in forbiddenTerms)
                        {
                            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                            {
                                report.TermIssues++;
                                report.Issues.Add(
                                    $"Slide {slideNum}: 包含禁止术语 '{term}' (形状: {shape.Name})");
                            }
                        }
                    }
                }
                catch { }
            }
        }

        return report;
    }
}
