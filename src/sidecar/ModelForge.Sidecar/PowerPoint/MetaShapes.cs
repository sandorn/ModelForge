using System.Text.Json;

namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// Meta Shapes — 为形状附加键值对元数据，支持按元数据搜索。
/// 元数据存储在形状的 AlternativeText（最多 255 字符的 JSON）。
/// </summary>
public static class MetaShapes
{
    /// <summary>为选中形状设置元数据。</summary>
    public static string SetMeta(dynamic pptApp, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "请提供元数据键 (key 参数)。";

        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "请先选中一个形状。";

        dynamic shape = selection.ShapeRange[1];
        var meta = ReadMeta(shape);
        meta[key.Trim()] = value ?? "";
        WriteMeta(shape, meta);

        return $"已为形状 '{shape.Name}' 设置元数据 '{key}' = '{value}'。";
    }

    /// <summary>读取选中形状的元数据。</summary>
    public static string GetMeta(dynamic pptApp)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "请先选中一个形状。";

        dynamic shape = selection.ShapeRange[1];
        var meta = ReadMeta(shape);
        return JsonSerializer.Serialize(new
        {
            shapeName = (string)shape.Name,
            meta,
            count = meta.Count
        });
    }

    /// <summary>搜索所有幻灯片中包含指定元数据的形状。</summary>
    public static string SearchByMeta(dynamic pptApp, string key, string? value = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "请提供搜索键 (key 参数)。";

        dynamic presentation = pptApp.ActivePresentation;
        var results = new List<object>();

        foreach (dynamic slide in presentation.Slides)
        {
            foreach (dynamic shape in slide.Shapes)
            {
                try
                {
                    var meta = ReadMeta(shape);
                    if (meta.TryGetValue(key.Trim(), out string? v) &&
                        (value == null || (v != null && v.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase))))
                    {
                        results.Add(new
                        {
                            slideIndex = slide.SlideIndex,
                            shapeName = (string)shape.Name,
                            meta
                        });
                    }
                }
                catch { }
            }
        }

        return JsonSerializer.Serialize(new { key, value, count = results.Count, results });
    }

    /// <summary>移除选中形状的指定元数据键。</summary>
    public static string RemoveMeta(dynamic pptApp, string key)
    {
        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) return "请先选中一个形状。";

        dynamic shape = selection.ShapeRange[1];
        var meta = ReadMeta(shape);
        bool removed = meta.Remove(key.Trim());
        WriteMeta(shape, meta);

        return removed
            ? $"已从形状 '{shape.Name}' 移除元数据 '{key}'。"
            : $"形状 '{shape.Name}' 没有键 '{key}' 的元数据。";
    }

    private static Dictionary<string, string> ReadMeta(dynamic shape)
    {
        try
        {
            string altText = shape.AlternativeText ?? "";
            if (!string.IsNullOrWhiteSpace(altText) && altText.StartsWith('{'))
                return JsonSerializer.Deserialize<Dictionary<string, string>>(altText) ?? new();
        }
        catch { }
        return new();
    }

    private static void WriteMeta(dynamic shape, Dictionary<string, string> meta)
    {
        shape.AlternativeText = meta.Count > 0
            ? JsonSerializer.Serialize(meta)
            : "";
    }
}
