namespace ModelForge.Sidecar.Word;

/// <summary>
/// Word 文档大纲导航。
/// </summary>
public static class DocumentOutline
{
    public sealed class Heading
    {
        public int Level { get; set; }
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; }
    }

    /// <summary>列出文档中所有标题（Heading 1-4），生成大纲。</summary>
    public static string GetOutline(dynamic wordApp)
    {
        dynamic document = wordApp.ActiveDocument;
        var headings = new List<Heading>();

        foreach (dynamic paragraph in document.Paragraphs)
        {
            try
            {
                int style = paragraph.Style;
                string styleName = ((string)paragraph.Style.NameLocal).Trim();

                // Word 内置标题样式: "Heading 1" / "标题 1"
                int? level = styleName switch
                {
                    "Heading 1" or "标题 1" => 1,
                    "Heading 2" or "标题 2" => 2,
                    "Heading 3" or "标题 3" => 3,
                    "Heading 4" or "标题 4" => 4,
                    _ => null
                };

                if (level.HasValue)
                {
                    string text = paragraph.Range.Text?.Trim() ?? "";
                    if (text.Length > 0)
                    {
                        headings.Add(new Heading
                        {
                            Level = level.Value,
                            Text = text.Length > 80 ? text[..77] + "..." : text,
                            PageNumber = paragraph.Range.Information(3) // wdActiveEndPageNumber = 3
                        });
                    }
                }
            }
            catch { }
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            count = headings.Count,
            headings
        });
    }

    /// <summary>跳转到指定标题。</summary>
    public static string GoToHeading(dynamic wordApp, int headingIndex)
    {
        dynamic document = wordApp.ActiveDocument;
        int found = 0;

        foreach (dynamic paragraph in document.Paragraphs)
        {
            try
            {
                string styleName = ((string)paragraph.Style.NameLocal).Trim();
                if (styleName is "Heading 1" or "标题 1" or "Heading 2" or "标题 2" or
                    "Heading 3" or "标题 3" or "Heading 4" or "标题 4")
                {
                    found++;
                    if (found == headingIndex)
                    {
                        paragraph.Range.Select();
                        return $"已跳转到: {paragraph.Range.Text.Trim()[..Math.Min(80, paragraph.Range.Text.Trim().Length)]}";
                    }
                }
            }
            catch { }
        }

        return $"未找到第 {headingIndex} 个标题（文档共有 {found} 个标题）。";
    }
}
