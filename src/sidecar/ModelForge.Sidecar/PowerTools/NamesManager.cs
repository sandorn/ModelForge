namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// Excel Names Manager — scans and manages named ranges in the active workbook.
/// Lists, validates, and batch-deletes invalid or broken names.
/// </summary>
public static class NamesManager
{
    public sealed class NameInfo
    {
        public string Name { get; init; } = string.Empty;
        public string RefersTo { get; set; } = string.Empty;
        public bool IsVisible { get; init; }
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }

    public sealed class NamesReport
    {
        public List<NameInfo> AllNames { get; init; } = new();
        public List<NameInfo> InvalidNames { get; init; } = new();
        public int TotalCount => AllNames.Count;
        public int InvalidCount => InvalidNames.Count;
        public int DeletedCount { get; set; }
        public List<string> DeleteErrors { get; init; } = new();
    }

    /// <summary>
    /// Scan all named ranges in the active workbook and return a report.
    /// </summary>
    public static NamesReport Scan(dynamic excelApp)
    {
        var report = new NamesReport();
        dynamic workbook = excelApp.ActiveWorkbook;
        if (workbook == null)
        {
            report.DeleteErrors.Add("No active workbook. Please open a workbook and try again.");
            return report;
        }

        foreach (dynamic name in workbook.Names)
        {
            var info = new NameInfo
            {
                Name = name.Name ?? "",
                IsVisible = name.Visible,
            };

            try
            {
                info.RefersTo = name.RefersTo ?? "";
                // Validate by attempting to resolve the reference
                try
                {
                    dynamic range = name.RefersToRange;
                    info.IsValid = range != null;
                }
                catch
                {
                    info.IsValid = false;
                    info.Error = "Reference cannot be resolved (may refer to deleted sheet or range).";
                }
            }
            catch (Exception ex)
            {
                info.IsValid = false;
                info.Error = $"Error reading reference: {ex.Message}";
            }

            if (!info.IsValid)
                report.InvalidNames.Add(info);

            report.AllNames.Add(info);
        }

        return report;
    }

    /// <summary>
    /// Delete all invalid (broken) named ranges from the active workbook.
    /// </summary>
    public static NamesReport DeleteInvalid(dynamic excelApp)
    {
        var report = Scan(excelApp);
        dynamic workbook = excelApp.ActiveWorkbook;

        foreach (var invalid in report.InvalidNames)
        {
            try
            {
                // Find and delete the name object
                foreach (dynamic name in workbook.Names)
                {
                    if (string.Equals(name.Name as string, invalid.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        name.Delete();
                        report.DeletedCount++;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                report.DeleteErrors.Add($"Failed to delete '{invalid.Name}': {ex.Message}");
            }
        }

        return report;
    }
}