using System.Text.Json;

namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// 用户模板管理器 — 保存选中区域为可复用模板。
/// </summary>
public static class TemplateManager
{
    private static readonly string TemplateDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ModelForge", "Templates");

    public sealed class TemplateInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        public DateTime SavedAt { get; set; }
    }

    /// <summary>将选中区域另存为用户模板。</summary>
    public static string SaveSelection(dynamic excelApp, string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return "请提供模板名称 (templateName 参数)。";

        dynamic selection = excelApp.Selection;
        int rows = selection.Rows.Count;
        int cols = selection.Columns.Count;

        // 读取选中区域的单元格数据
        var cells = new List<List<CellData>>();
        for (int r = 1; r <= rows; r++)
        {
            var row = new List<CellData>();
            for (int c = 1; c <= cols; c++)
            {
                try
                {
                    dynamic cell = selection.Cells[r, c];
                    row.Add(new CellData
                    {
                        Value = cell.Value?.ToString(),
                        Formula = cell.HasFormula ? (cell.Formula as string) : null,
                        Bold = cell.Font.Bold,
                        NumberFormat = cell.NumberFormat as string,
                        ColumnWidth = (double)cell.ColumnWidth
                    });
                }
                catch
                {
                    row.Add(new CellData());
                }
            }
            cells.Add(row);
        }

        // 确保模板目录存在
        Directory.CreateDirectory(TemplateDir);

        var template = new
        {
            name = templateName,
            rows,
            columns = cols,
            cells
        };

        string fileName = SanitizeFileName(templateName) + ".json";
        File.WriteAllText(Path.Combine(TemplateDir, fileName),
            JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true }));

        return $"模板 '{templateName}' 已保存（{rows} 行 × {cols} 列）。";
    }

    /// <summary>列出所有已保存的用户模板。</summary>
    public static string ListTemplates()
    {
        Directory.CreateDirectory(TemplateDir);
        var templates = new List<TemplateInfo>();

        foreach (var file in Directory.GetFiles(TemplateDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                templates.Add(new TemplateInfo
                {
                    Name = root.GetProperty("name").GetString() ?? "Unknown",
                    FileName = Path.GetFileName(file),
                    Rows = root.GetProperty("rows").GetInt32(),
                    Columns = root.GetProperty("columns").GetInt32(),
                    SavedAt = File.GetLastWriteTime(file)
                });
            }
            catch { }
        }

        return JsonSerializer.Serialize(new { count = templates.Count, templates });
    }

    /// <summary>在当前选中位置插入已保存的模板。</summary>
    public static string InsertTemplate(dynamic excelApp, string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return "请提供模板名称 (templateName 参数)。";

        string fileName = SanitizeFileName(templateName) + ".json";
        string filePath = Path.Combine(TemplateDir, fileName);
        if (!File.Exists(filePath))
            return $"模板 '{templateName}' 不存在。可用模板请使用 excel.list-templates 查看。";

        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        dynamic worksheet = excelApp.ActiveSheet;
        dynamic selection = excelApp.Selection;
        int startRow = selection.Row;
        int startCol = selection.Column;

        var cells = root.GetProperty("cells");
        int rowIdx = 0;
        foreach (var row in cells.EnumerateArray())
        {
            int colIdx = 0;
            foreach (var cellData in row.EnumerateArray())
            {
                dynamic target = worksheet.Cells[startRow + rowIdx, startCol + colIdx];
                try
                {
                    string? formula = cellData.GetProperty("formula").GetString();
                    if (!string.IsNullOrWhiteSpace(formula))
                        target.Formula = formula;
                    else
                    {
                        string? value = cellData.GetProperty("value").GetString();
                        if (value != null) target.Value = value;
                    }

                    if (cellData.GetProperty("bold").GetBoolean())
                        target.Font.Bold = true;

                    string? format = cellData.GetProperty("numberFormat").GetString();
                    if (!string.IsNullOrWhiteSpace(format))
                        target.NumberFormat = format;
                }
                catch { }
                colIdx++;
            }
            rowIdx++;
        }

        return $"模板 '{templateName}' 已插入到 {worksheet.Name}!{GetColumnLetter(startCol)}{startRow}（{root.GetProperty("rows").GetInt32()} 行 × {root.GetProperty("columns").GetInt32()} 列）。";
    }

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

    private static string GetColumnLetter(int col)
    {
        string result = "";
        while (col > 0) { col--; result = (char)('A' + col % 26) + result; col /= 26; }
        return result;
    }

    private sealed class CellData
    {
        public string? Value { get; set; }
        public string? Formula { get; set; }
        public bool Bold { get; set; }
        public string? NumberFormat { get; set; }
        public double ColumnWidth { get; set; }
    }
}
