namespace ModelForge.Sidecar.PowerTools;

/// <summary>
/// Data validation helpers — dropdown lists, input restrictions.
/// </summary>
public static class DataValidationTools
{
    /// <summary>Add a simple dropdown list to selected cells.</summary>
    public static string AddDropdown(dynamic excelApp, string options)
    {
        if (string.IsNullOrWhiteSpace(options))
            return "Please provide comma-separated options (e.g. 'Yes,No,Maybe').";

        var items = options.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (items.Length == 0) return "No valid options provided.";

        dynamic selection = excelApp.Selection;
        dynamic validation = selection.Validation;
        validation.Delete();
        validation.Add(3, 1, 1, string.Join(",", items)); // xlValidateList, xlValidAlertStop, xlBetween
        validation.InCellDropdown = true;
        validation.IgnoreBlank = true;

        return $"Added dropdown with {items.Length} options to {selection.Address}.";
    }

    /// <summary>Add numeric range validation (min/max).</summary>
    public static string AddNumericRange(dynamic excelApp, double min, double max)
    {
        dynamic selection = excelApp.Selection;
        dynamic validation = selection.Validation;
        validation.Delete();
        validation.Add(1, 1, 1, min, max); // xlValidateDecimal, xlValidAlertStop, xlBetween
        validation.InputMessage = $"Enter a value between {min} and {max}";

        return $"Added numeric validation ({min}-{max}) to {selection.Address}.";
    }

    /// <summary>Clear all data validation from selection.</summary>
    public static string ClearValidation(dynamic excelApp)
    {
        dynamic selection = excelApp.Selection;
        try { selection.Validation.Delete(); } catch { }
        return $"Cleared validation from {selection.Address}.";
    }
}
