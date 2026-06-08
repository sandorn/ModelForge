using System.Text.Json;

namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// MasterShapes — 将选中形状保存为可复用形状库。
/// </summary>
public static class MasterShapes
{
    private static readonly string LibraryDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ModelForge", "MasterShapes");

    /// <summary>将当前选中的形状保存为 Master Shape。</summary>
    public static string SaveShape(dynamic pptApp, string shapeName)
    {
        if (string.IsNullOrWhiteSpace(shapeName))
            return "请提供形状名称 (shapeName 参数)。";

        dynamic selection = pptApp.ActiveWindow.Selection;
        if (selection.Type != 2) // ppSelectionShapes
            return "请先选中一个形状。";

        dynamic shape = selection.ShapeRange[1];
        var props = new Dictionary<string, object>();

        try { props["Type"] = (int)shape.Type; } catch { }
        try { props["Left"] = (float)shape.Left; } catch { }
        try { props["Top"] = (float)shape.Top; } catch { }
        try { props["Width"] = (float)shape.Width; } catch { }
        try { props["Height"] = (float)shape.Height; } catch { }
        try { props["Rotation"] = (float)shape.Rotation; } catch { }

        // Fill
        try { props["FillVisible"] = shape.Fill.Visible == -1; } catch { }
        try { props["FillColor"] = (int)shape.Fill.ForeColor.RGB; } catch { }

        // Line
        try { props["LineVisible"] = shape.Line.Visible == -1; } catch { }
        try { props["LineColor"] = (int)shape.Line.ForeColor.RGB; } catch { }
        try { props["LineWeight"] = (float)shape.Line.Weight; } catch { }

        // Text
        try
        {
            if (shape.HasTextFrame == -1)
            {
                props["HasText"] = true;
                props["Text"] = shape.TextFrame.TextRange.Text ?? "";
                try { props["FontName"] = shape.TextFrame.TextRange.Font.Name; } catch { }
                try { props["FontSize"] = (float)shape.TextFrame.TextRange.Font.Size; } catch { }
                try { props["FontBold"] = shape.TextFrame.TextRange.Font.Bold == -1; } catch { }
                try { props["FontColor"] = (int)shape.TextFrame.TextRange.Font.Color.RGB; } catch { }
                try { props["TextAlignment"] = (int)shape.TextFrame.TextRange.ParagraphFormat.Alignment; } catch { }
            }
        }
        catch { }

        Directory.CreateDirectory(LibraryDir);
        string filePath = Path.Combine(LibraryDir, SanitizeFileName(shapeName) + ".json");
        File.WriteAllText(filePath, JsonSerializer.Serialize(props, new JsonSerializerOptions { WriteIndented = true }));

        return $"形状 '{shapeName}' 已保存到 MasterShapes 库。";
    }

    /// <summary>列出所有 Master Shapes。</summary>
    public static string ListShapes()
    {
        Directory.CreateDirectory(LibraryDir);
        var shapes = Directory.GetFiles(LibraryDir, "*.json")
            .Select(f => new
            {
                name = Path.GetFileNameWithoutExtension(f),
                size = new FileInfo(f).Length,
                modified = File.GetLastWriteTime(f)
            })
            .ToArray();

        return JsonSerializer.Serialize(new { count = shapes.Length, shapes });
    }

    /// <summary>在当前幻灯片插入 Master Shape。</summary>
    public static string InsertShape(dynamic pptApp, string shapeName)
    {
        if (string.IsNullOrWhiteSpace(shapeName))
            return "请提供形状名称 (shapeName 参数)。";

        string filePath = Path.Combine(LibraryDir, SanitizeFileName(shapeName) + ".json");
        if (!File.Exists(filePath))
            return $"Master Shape '{shapeName}' 不存在。";

        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        var p = doc.RootElement;

        dynamic slide = pptApp.ActiveWindow.View.Slide;
        int shapeType = p.TryGetProperty("Type", out var t) ? t.GetInt32() : 1; // default rectangle

        float left = GetFloat(p, "Left", 100);
        float top = GetFloat(p, "Top", 100);
        float width = GetFloat(p, "Width", 100);
        float height = GetFloat(p, "Height", 60);

        dynamic shape = slide.Shapes.AddShape(shapeType, left, top, width, height);

        // Rotation
        if (p.TryGetProperty("Rotation", out var rot))
            shape.Rotation = rot.GetSingle();

        // Fill
        if (p.TryGetProperty("FillVisible", out var fv) && fv.GetBoolean())
        {
            try { shape.Fill.ForeColor.RGB = p.GetProperty("FillColor").GetInt32(); } catch { }
        }

        // Line
        if (p.TryGetProperty("LineVisible", out var lv) && lv.GetBoolean())
        {
            try { shape.Line.ForeColor.RGB = p.GetProperty("LineColor").GetInt32(); } catch { }
            try { shape.Line.Weight = GetFloat(p, "LineWeight", 1); } catch { }
        }

        // Text
        if (p.TryGetProperty("HasText", out var ht) && ht.GetBoolean())
        {
            try { shape.TextFrame.TextRange.Text = p.GetProperty("Text").GetString() ?? ""; } catch { }
            try { shape.TextFrame.TextRange.Font.Name = p.GetProperty("FontName").GetString(); } catch { }
            try { shape.TextFrame.TextRange.Font.Size = GetFloat(p, "FontSize", 12); } catch { }
            try { if (p.GetProperty("FontBold").GetBoolean()) shape.TextFrame.TextRange.Font.Bold = -1; } catch { }
            try { shape.TextFrame.TextRange.Font.Color.RGB = p.GetProperty("FontColor").GetInt32(); } catch { }
        }

        return $"已插入 Master Shape '{shapeName}'。";
    }

    private static float GetFloat(JsonElement el, string key, float fallback) =>
        el.TryGetProperty(key, out var v) ? v.GetSingle() : fallback;

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}
