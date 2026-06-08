namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// Reformat View — 按属性搜索形状并批量替换格式。
/// </summary>
public static class ReformatView
{
    public sealed class SearchResult
    {
        public int TotalShapes { get; set; }
        public int Matched { get; set; }
        public List<ShapeMatch> Matches { get; } = new();
    }

    public sealed class ShapeMatch
    {
        public int SlideIndex { get; set; }
        public string ShapeName { get; set; } = string.Empty;
        public string? FontName { get; set; }
        public float? FontSize { get; set; }
        public string? FontColorHex { get; set; }
    }

    /// <summary>搜索匹配指定字体名称的形状。</summary>
    public static SearchResult SearchByFont(dynamic pptApp, string fontName)
    {
        var result = new SearchResult();
        dynamic presentation = pptApp.ActivePresentation;

        foreach (dynamic slide in presentation.Slides)
        {
            result.TotalShapes += slide.Shapes.Count;
            foreach (dynamic shape in slide.Shapes)
            {
                try
                {
                    if (shape.HasTextFrame == 0) continue;
                    string shapeFont = shape.TextFrame.TextRange.Font.Name ?? "";
                    if (shapeFont.StartsWith(fontName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Matched++;
                        result.Matches.Add(new ShapeMatch
                        {
                            SlideIndex = slide.SlideIndex,
                            ShapeName = shape.Name,
                            FontName = shapeFont,
                            FontSize = (float)shape.TextFrame.TextRange.Font.Size
                        });
                    }
                }
                catch { }
            }
        }

        return result;
    }

    /// <summary>搜索匹配指定字体大小的形状。</summary>
    public static SearchResult SearchByFontSize(dynamic pptApp, float targetSize, float tolerance = 0.5f)
    {
        var result = new SearchResult();
        dynamic presentation = pptApp.ActivePresentation;

        foreach (dynamic slide in presentation.Slides)
        {
            result.TotalShapes += slide.Shapes.Count;
            foreach (dynamic shape in slide.Shapes)
            {
                try
                {
                    if (shape.HasTextFrame == 0) continue;
                    float shapeSize = (float)shape.TextFrame.TextRange.Font.Size;
                    if (Math.Abs(shapeSize - targetSize) <= tolerance)
                    {
                        result.Matched++;
                        result.Matches.Add(new ShapeMatch
                        {
                            SlideIndex = slide.SlideIndex,
                            ShapeName = shape.Name,
                            FontSize = shapeSize
                        });
                    }
                }
                catch { }
            }
        }

        return result;
    }

    /// <summary>批量替换字体。</summary>
    public static string ReplaceFont(dynamic pptApp, string oldFont, string newFont)
    {
        var found = SearchByFont(pptApp, oldFont);
        dynamic presentation = pptApp.ActivePresentation;
        int replaced = 0;

        foreach (var match in found.Matches)
        {
            try
            {
                dynamic slide = presentation.Slides[match.SlideIndex];
                dynamic shape = FindShapeByName(slide, match.ShapeName);
                if (shape != null)
                {
                    shape.TextFrame.TextRange.Font.Name = newFont;
                    replaced++;
                }
            }
            catch { }
        }

        return $"已将 {replaced} 个形状的字体从 '{oldFont}' 替换为 '{newFont}'。";
    }

    /// <summary>批量替换字体大小。</summary>
    public static string ReplaceFontSize(dynamic pptApp, float oldSize, float newSize)
    {
        var found = SearchByFontSize(pptApp, oldSize);
        dynamic presentation = pptApp.ActivePresentation;
        int replaced = 0;

        foreach (var match in found.Matches)
        {
            try
            {
                dynamic slide = presentation.Slides[match.SlideIndex];
                dynamic shape = FindShapeByName(slide, match.ShapeName);
                if (shape != null)
                {
                    shape.TextFrame.TextRange.Font.Size = newSize;
                    replaced++;
                }
            }
            catch { }
        }

        return $"已将 {replaced} 个形状的字体大小从 {oldSize}pt 替换为 {newSize}pt。";
    }

    private static dynamic? FindShapeByName(dynamic slide, string name)
    {
        foreach (dynamic shape in slide.Shapes)
        {
            if (string.Equals(Convert.ToString(shape.Name), name, StringComparison.OrdinalIgnoreCase))
                return shape;
        }
        return null;
    }
}
