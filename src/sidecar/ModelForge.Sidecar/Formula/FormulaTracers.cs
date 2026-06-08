using System.Text.RegularExpressions;

namespace ModelForge.Sidecar.Formula;

public static class PrecedentTracer
{
    public static List<CellReference> TraceDirectPrecedents(dynamic excelApp)
    {
        var result = new List<CellReference>();
        dynamic selection = excelApp.Selection;
        try
        {
            dynamic precedents = selection.DirectPrecedents;
            foreach (dynamic area in precedents.Areas)
                foreach (dynamic cell in area)
                    result.Add(MakeRef(cell, excelApp.ActiveSheet.Name));
        }
        catch { }
        return result;
    }

    public static List<CellReference> TraceAllPrecedents(dynamic excelApp)
    {
        var result = TraceDirectPrecedents(excelApp);
        dynamic selection = excelApp.Selection;
        dynamic workbook = excelApp.ActiveWorkbook;

        if (selection.HasFormula)
        {
            string formula = selection.Formula as string ?? "";
            var externals = ExtractExternalRefs(formula);
            foreach (var ext in externals)
            {
                try
                {
                    dynamic sheet = workbook.Sheets[ext.SheetName];
                    dynamic cell = sheet.Range[ext.CellAddress];
                    result.Add(MakeRef(cell, ext.SheetName));
                }
                catch { }
            }
        }
        return result;
    }

    private static CellReference MakeRef(dynamic cell, string sheetName) => new()
    {
        Address = $"{sheetName}!{cell.Address}",
        Value = cell.Value?.ToString(),
        Formula = cell.HasFormula ? (cell.Formula as string) : null
    };

    private static List<(string SheetName, string CellAddress)> ExtractExternalRefs(string formula)
    {
        var result = new List<(string, string)>();
        var matches = Regex.Matches(formula, @"('?[^'!+\-*/^&=<>(), ]+'?![$]?[A-Z]{1,3}[$]?\d+)");
        foreach (Match match in matches)
        {
            var parts = match.Value.Split('!');
            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0].Trim('\'')))
                result.Add((parts[0].Trim('\''), parts[1]));
        }
        return result;
    }
}

public static class DependentTracer
{
    public static List<CellReference> TraceDirectDependents(dynamic excelApp)
    {
        var result = new List<CellReference>();
        dynamic selection = excelApp.Selection;
        try
        {
            dynamic dependents = selection.DirectDependents;
            foreach (dynamic area in dependents.Areas)
                foreach (dynamic cell in area)
                    result.Add(MakeRef(cell, excelApp.ActiveSheet.Name));
        }
        catch { }
        return result;
    }

    public static List<CellReference> TraceAllDependents(dynamic excelApp)
    {
        var result = TraceDirectDependents(excelApp);
        dynamic selection = excelApp.Selection;
        dynamic workbook = excelApp.ActiveWorkbook;
        string currentSheet = excelApp.ActiveSheet.Name;
        string currentAddr = selection.Address;

        foreach (dynamic sheet in workbook.Worksheets)
        {
            string sheetName = sheet.Name;
            if (sheetName == currentSheet) continue;
            try
            {
                dynamic usedRange = sheet.UsedRange;
                foreach (dynamic cell in usedRange)
                {
                    if (!cell.HasFormula) continue;
                    string formula = cell.Formula as string ?? "";
                    if (formula.Contains(currentSheet, StringComparison.OrdinalIgnoreCase) &&
                        formula.Contains(currentAddr, StringComparison.OrdinalIgnoreCase))
                        result.Add(MakeRef(cell, sheetName));
                }
            }
            catch { }
        }
        return result;
    }

    private static CellReference MakeRef(dynamic cell, string sheetName) => new()
    {
        Address = $"{sheetName}!{cell.Address}",
        Value = cell.Value?.ToString(),
        Formula = cell.HasFormula ? (cell.Formula as string) : null
    };
}

public sealed class CellReference
{
    public string Address { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Formula { get; set; }
}
