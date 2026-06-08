namespace ModelForge.Sidecar.Word;

/// <summary>
/// Word document tools — page breaks, section breaks, TOC, cover page, find/replace, styles.
/// </summary>
public static class DocumentTools
{
    public static string InsertPageBreak(dynamic wordApp)
    {
        dynamic selection = wordApp.Selection;
        selection.InsertBreak(7); // wdPageBreak
        return "Inserted page break.";
    }

    public static string InsertSectionBreakNextPage(dynamic wordApp)
    {
        dynamic selection = wordApp.Selection;
        selection.InsertBreak(2); // wdSectionBreakNextPage
        return "Inserted next-page section break.";
    }

    public static string InsertSectionBreakContinuous(dynamic wordApp)
    {
        dynamic selection = wordApp.Selection;
        selection.InsertBreak(3); // wdSectionBreakContinuous
        return "Inserted continuous section break.";
    }

    public static string InsertTableOfContents(dynamic wordApp)
    {
        dynamic document = wordApp.ActiveDocument;
        dynamic range = document.Range(0, 0);
        dynamic toc = document.TablesOfContents.Add(range, true, 1, 3);
        toc.Update();
        return "Inserted table of contents.";
    }

    public static string InsertCoverPage(dynamic wordApp, string? title = null, string? subtitle = null)
    {
        title = string.IsNullOrWhiteSpace(title) ? "Document Title" : title;
        dynamic document = wordApp.ActiveDocument;
        dynamic range = document.Range(0, 0);
        range.Text = title + "\r\n";
        range.Font.Size = 28;
        range.Font.Bold = -1;
        range.ParagraphFormat.Alignment = 1;
        range.InsertParagraphAfter();

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            dynamic subRange = document.Range(range.End, range.End);
            subRange.Text = subtitle + "\r\n";
            subRange.Font.Size = 16;
            subRange.Font.ColorIndex = 15;
            subRange.ParagraphFormat.Alignment = 1;
            subRange.InsertParagraphAfter();
        }

        dynamic endRange = document.Range(document.Range().End - 1, document.Range().End - 1);
        endRange.InsertBreak(7);
        return $"Inserted cover page: {title}.";
    }

    public static string FindReplace(dynamic wordApp, string findText, string replaceText)
    {
        if (string.IsNullOrWhiteSpace(findText))
            return "Please provide text to find (findText parameter).";

        dynamic find = wordApp.Selection.Find;
        find.ClearFormatting();
        find.Text = findText;
        find.Replacement.ClearFormatting();
        find.Replacement.Text = replaceText ?? "";
        find.Forward = true;
        find.Wrap = 1;
        find.Execute(Replace: 2);

        return $"Replaced all occurrences of '{findText}' with '{replaceText}'.";
    }

    public static string ApplyHeading(dynamic wordApp, int level = 1)
    {
        level = Math.Clamp(level, 1, 3);
        dynamic selection = wordApp.Selection;
        selection.Style = level switch { 1 => "Heading 1", 2 => "Heading 2", _ => "Heading 3" };
        return $"Applied Heading {level} to current paragraph.";
    }

    public static string ApplyNormalStyle(dynamic wordApp)
    {
        wordApp.Selection.Style = "Normal";
        return "Applied Normal style.";
    }

    /// <summary>Set page margins (in points: 1 inch = 72 pt).</summary>
    public static string SetMargins(dynamic wordApp, float? top = null, float? bottom = null, float? left = null, float? right = null)
    {
        dynamic document = wordApp.ActiveDocument;
        dynamic ps = document.PageSetup;
        if (top.HasValue) ps.TopMargin = top.Value;
        if (bottom.HasValue) ps.BottomMargin = bottom.Value;
        if (left.HasValue) ps.LeftMargin = left.Value;
        if (right.HasValue) ps.RightMargin = right.Value;
        return "Page margins updated.";
    }

    /// <summary>Set page orientation (0=portrait, 1=landscape).</summary>
    public static string SetOrientation(dynamic wordApp, int orientation = 1)
    {
        dynamic document = wordApp.ActiveDocument;
        document.PageSetup.Orientation = orientation == 1 ? 1 : 0; // wdOrientLandscape / wdOrientPortrait
        return orientation == 1 ? "Set to landscape." : "Set to portrait.";
    }

    public static string GetStats(dynamic wordApp)
    {
        dynamic document = wordApp.ActiveDocument;
        dynamic builtInProps = document.BuiltInDocumentProperties;

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            paragraphs = document.Paragraphs.Count,
            words = document.Words.Count,
            characters = document.Characters.Count,
            pages = document.ComputeStatistics(2),
            lines = document.ComputeStatistics(1),
            sections = document.Sections.Count,
            tables = document.Tables.Count,
            title = (string?)builtInProps["Title"].Value,
            author = (string?)builtInProps["Author"].Value,
            created = builtInProps["Creation Date"].Value?.ToString(),
            lastSaved = builtInProps["Last Save Time"].Value?.ToString()
        });
    }
}
