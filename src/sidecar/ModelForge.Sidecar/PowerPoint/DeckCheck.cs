namespace ModelForge.Sidecar.PowerPoint;

using System.Globalization;
using System.Net;
using System.Text;

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
        public int MissingSlideNumbers { get; set; }
        public int DenseTextSlides { get; set; }
        public int LogoIssues { get; set; }
        public int LogoPositionIssues { get; set; }
        public int ColorIssues { get; set; }
        public string? TemplateName { get; set; }
        public string ReportTitle { get; set; } = "ModelForge Deck Check Report";
        public string BrandPrimaryColor { get; set; } = "#1f3a5f";
        public string BrandAccentColor { get; set; } = "#3b82f6";
        public string? ReportPath { get; set; }
        public List<string> Issues { get; } = new();
        public int TotalIssues =>
            FontIssues + TermIssues + MissingSlideNumbers + DenseTextSlides + LogoIssues + LogoPositionIssues + ColorIssues;
        public string OverallStatus => TotalIssues == 0 ? "Pass" : TotalIssues <= 5 ? "Review" : "ActionRequired";
    }

    /// <summary>
    /// 扫描当前演示文稿，检查字体、术语、颜色合规性。
    /// </summary>
    /// <summary>
    /// Run DeckCheck with enterprise dictionary terms via arguments dictionary.
    /// Enables Backend dictionary integration: pass forbiddenTerms as pipe-delimited string.
    /// </summary>
    public static DeckCheckReport RunWithDictionary(dynamic pptApp, Dictionary<string, string>? args = null)
    {
        string[]? forbiddenTerms = null;
        string[]? allowedFonts = null;
        string[]? brandColors = null;
        var checkLogos = false;
        var exportPdf = false;
        string? reportPath = null;
        string? templateName = null;
        string? reportTitle = null;
        string? brandPrimaryColor = null;
        string? brandAccentColor = null;
        float? logoMaxLeft = null;
        float? logoMaxTop = null;
        float? logoMaxWidth = null;
        float? logoMaxHeight = null;

        if (args != null)
        {
            if (args.TryGetValue("forbiddenTerms", out var forbiddenTermsValue))
                forbiddenTerms = SplitPipeDelimited(forbiddenTermsValue);
            if (args.TryGetValue("allowedFonts", out var allowedFontsValue))
                allowedFonts = SplitPipeDelimited(allowedFontsValue);
            if (args.TryGetValue("brandColors", out var brandColorsValue))
                brandColors = SplitPipeDelimited(brandColorsValue);
            if (args.TryGetValue("checkLogos", out var checkLogosValue))
                checkLogos = IsTruthy(checkLogosValue);
            if (args.TryGetValue("exportPdf", out var exportPdfValue))
                exportPdf = IsTruthy(exportPdfValue);
            if (args.TryGetValue("reportPath", out var reportPathValue) && !string.IsNullOrWhiteSpace(reportPathValue))
                reportPath = reportPathValue;
            if (args.TryGetValue("templateName", out var templateNameValue) && !string.IsNullOrWhiteSpace(templateNameValue))
                templateName = templateNameValue;
            if (args.TryGetValue("reportTitle", out var reportTitleValue) && !string.IsNullOrWhiteSpace(reportTitleValue))
                reportTitle = reportTitleValue;
            if (args.TryGetValue("brandPrimaryColor", out var brandPrimaryColorValue) && !string.IsNullOrWhiteSpace(brandPrimaryColorValue))
                brandPrimaryColor = brandPrimaryColorValue;
            if (args.TryGetValue("brandAccentColor", out var brandAccentColorValue) && !string.IsNullOrWhiteSpace(brandAccentColorValue))
                brandAccentColor = brandAccentColorValue;
            if (args.TryGetValue("logoMaxLeft", out var logoMaxLeftValue))
                logoMaxLeft = ParseFloat(logoMaxLeftValue);
            if (args.TryGetValue("logoMaxTop", out var logoMaxTopValue))
                logoMaxTop = ParseFloat(logoMaxTopValue);
            if (args.TryGetValue("logoMaxWidth", out var logoMaxWidthValue))
                logoMaxWidth = ParseFloat(logoMaxWidthValue);
            if (args.TryGetValue("logoMaxHeight", out var logoMaxHeightValue))
                logoMaxHeight = ParseFloat(logoMaxHeightValue);
        }

        // Fall back to defaults if no custom args
        return Run(
            pptApp,
            allowedFonts,
            forbiddenTerms,
            brandColors,
            checkLogos,
            exportPdf,
            reportPath,
            new LogoPolicy(templateName, logoMaxLeft, logoMaxTop, logoMaxWidth, logoMaxHeight)
            {
                ReportTitle = reportTitle ?? LogoPolicy.Default.ReportTitle,
                BrandPrimaryColor = NormalizeHexColor(brandPrimaryColor, LogoPolicy.Default.BrandPrimaryColor),
                BrandAccentColor = NormalizeHexColor(brandAccentColor, LogoPolicy.Default.BrandAccentColor)
            });
    }


    /// <param name="pptApp">PowerPoint Application</param>
    /// <param name="allowedFonts">允许的字体列表。默认 Arial 和 Calibri。</param>
    /// <param name="forbiddenTerms">禁止的术语列表。</param>
    public static DeckCheckReport Run(dynamic pptApp,
        string[]? allowedFonts = null,
        string[]? forbiddenTerms = null,
        string[]? brandColors = null,
        bool checkLogos = false,
        bool exportPdf = false,
        string? reportPath = null,
        LogoPolicy? logoPolicy = null)
    {
        allowedFonts ??= new[] { "Arial", "Calibri", "Calibri Light", "Microsoft YaHei" };
        forbiddenTerms ??= new[] { "机密", "草案", "DRAFT" };
        var checkColors = brandColors is { Length: > 0 };
        logoPolicy ??= LogoPolicy.Default;

        var report = new DeckCheckReport
        {
            TemplateName = logoPolicy.TemplateName,
            ReportTitle = logoPolicy.ReportTitle,
            BrandPrimaryColor = logoPolicy.BrandPrimaryColor,
            BrandAccentColor = logoPolicy.BrandAccentColor
        };
        dynamic presentation = pptApp.ActivePresentation;
        if (presentation == null) return report;

        report.SlidesScanned = presentation.Slides.Count;

        foreach (dynamic slide in presentation.Slides)
        {
            int slideNum = slide.SlideIndex;
            var hasSlideNumber = false;
            var charCount = 0;
            var hasLogo = false;

            foreach (dynamic shape in slide.Shapes)
            {
                if (checkLogos && LooksLikeLogo(shape))
                {
                    hasLogo = true;
                    if (!LogoIsWithinPolicy(shape, logoPolicy))
                    {
                        report.LogoPositionIssues++;
                        report.Issues.Add($"Slide {slideNum}: Logo position outside template bounds (shape: {shape.Name})");
                    }
                }

                // 检查文本框字体
                try
                {
                    if (shape.HasTextFrame != 0)
                    {
                        dynamic textRange = shape.TextFrame.TextRange;
                        string text = textRange.Text ?? "";
                        charCount += text.Length;
                        if (string.Equals(text.Trim(), slideNum.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            hasSlideNumber = true;
                        }

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

                        // 颜色合规检查
                        if (checkColors)
                        {
                            try
                            {
                                int fontColorRgb = textRange.Font.Color.RGB;
                                string fontColorHex = $"#{fontColorRgb & 0xFF:X2}{(fontColorRgb >> 8) & 0xFF:X2}{(fontColorRgb >> 16) & 0xFF:X2}";
                                if (!brandColors!.Any(c => string.Equals(c.Trim().TrimStart('#'), fontColorHex.TrimStart('#'), StringComparison.OrdinalIgnoreCase)))
                                {
                                    report.ColorIssues++;
                                    report.Issues.Add(
                                        $"Slide {slideNum}: 字体颜色 {fontColorHex} 不在品牌调色板内 (形状: {shape.Name})");
                                }
                            }
                            catch { /* 颜色读取失败，跳过 */ }
                        }

                        // 术语检查
                        foreach (var term in forbiddenTerms)
                        {
                            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                            {
                                report.TermIssues++;
                                report.Issues.Add(
                                    $"Slide {slideNum}: 包含禁止术语 '{term}' (形状: {shape.Name})");

                                // 高亮术语：在幻灯片上标记
                                HighlightTerm(textRange, term);
                            }
                        }
                    }
                }
                catch { }
            }

            if (!hasSlideNumber)
            {
                report.MissingSlideNumbers++;
                report.Issues.Add($"Slide {slideNum}: Missing slide number");
            }

            if (charCount > 2000)
            {
                report.DenseTextSlides++;
                report.Issues.Add($"Slide {slideNum}: Dense text ({charCount} characters)");
            }

            if (checkLogos && !hasLogo)
            {
                report.LogoIssues++;
                report.Issues.Add($"Slide {slideNum}: Missing logo");
            }
        }

        if (exportPdf)
        {
            report.ReportPath = ExportReport(report, reportPath);
        }

        return report;
    }

    public static string ExportReport(DeckCheckReport report, string? reportPath = null)
    {
        var outputPath = ResolveReportPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, BuildBasicPdf(report));
        return outputPath;
    }

    public static string BuildHtmlReport(DeckCheckReport report)
    {
        var primaryColor = NormalizeHexColor(report.BrandPrimaryColor, LogoPolicy.Default.BrandPrimaryColor);
        var accentColor = NormalizeHexColor(report.BrandAccentColor, LogoPolicy.Default.BrandAccentColor);
        var statusLabel = report.OverallStatus switch
        {
            "Pass" => "Ready to share",
            "Review" => "Needs review",
            _ => "Action required"
        };
        var statusClass = report.OverallStatus switch
        {
            "Pass" => "pass",
            "Review" => "review",
            _ => "action"
        };
        var issueRows = report.Issues.Count == 0
            ? "<tr><td colspan=\"2\">No issues found.</td></tr>"
            : string.Join(Environment.NewLine, report.Issues.Select((issue, index) =>
                $"<tr><td>{index + 1}</td><td>{WebUtility.HtmlEncode(issue)}</td></tr>"));

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>{{WebUtility.HtmlEncode(report.ReportTitle)}}</title>
  <style>
    :root { --brand-primary: {{primaryColor}}; --brand-accent: {{accentColor}}; --surface: #f6f8fb; --text: #172033; }
    body { font-family: Arial, sans-serif; color: var(--text); margin: 0; background: var(--surface); }
    .page { margin: 32px; background: #fff; border-radius: 18px; overflow: hidden; box-shadow: 0 12px 36px rgba(23, 32, 51, 0.12); }
    .hero { background: linear-gradient(135deg, var(--brand-primary), var(--brand-accent)); color: #fff; padding: 28px 32px; }
    .eyebrow { letter-spacing: .12em; text-transform: uppercase; opacity: .82; font-size: 11px; font-weight: 700; }
    h1 { margin: 8px 0 4px; font-size: 30px; }
    .meta { opacity: .86; }
    .content { padding: 28px 32px 32px; }
    .status { display: inline-block; border-radius: 999px; padding: 6px 12px; font-weight: 700; margin-top: 12px; }
    .status.pass { background: #dff6e7; color: #126b35; }
    .status.review { background: #fff0ce; color: #8a5700; }
    .status.action { background: #fde2e1; color: #a4262c; }
    .summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin: 22px 0; }
    .card { border: 1px solid #e5e9f2; border-radius: 12px; padding: 14px; background: #fff; }
    .value { font-size: 26px; font-weight: 800; color: var(--brand-primary); }
    .label { color: #667085; font-size: 12px; margin-top: 4px; }
    .template { border-left: 4px solid var(--brand-accent); margin-bottom: 24px; }
    table { border-collapse: collapse; width: 100%; }
    th, td { border-bottom: 1px solid #e5e9f2; padding: 10px; text-align: left; vertical-align: top; }
    th { background: #f8fafc; color: #344054; }
  </style>
</head>
<body>
  <main class="page">
    <section class="hero">
      <div class="eyebrow">ModelForge Presentation Proofing</div>
      <h1>{{WebUtility.HtmlEncode(report.ReportTitle)}}</h1>
      <div class="meta">Generated at {{DateTimeOffset.UtcNow:O}}</div>
      <div class="status {{statusClass}}">{{statusLabel}} · {{report.TotalIssues}} issues</div>
    </section>
    <section class="content">
      <div class="summary">
        <div class="card"><div class="value">{{report.SlidesScanned}}</div><div class="label">Slides scanned</div></div>
        <div class="card"><div class="value">{{report.FontIssues}}</div><div class="label">Font issues</div></div>
        <div class="card"><div class="value">{{report.TermIssues}}</div><div class="label">Term issues</div></div>
        <div class="card"><div class="value">{{report.MissingSlideNumbers}}</div><div class="label">Missing numbers</div></div>
        <div class="card"><div class="value">{{report.DenseTextSlides}}</div><div class="label">Dense slides</div></div>
        <div class="card"><div class="value">{{report.LogoIssues}}</div><div class="label">Logo issues</div></div>
        <div class="card"><div class="value">{{report.LogoPositionIssues}}</div><div class="label">Logo position</div></div>
        <div class="card"><div class="value">{{report.TotalIssues}}</div><div class="label">Total issues</div></div>
      </div>
      <div class="card template"><strong>Template</strong><br />{{WebUtility.HtmlEncode(report.TemplateName ?? "Default enterprise template")}}</div>
      <h2>Issues</h2>
      <table>
        <thead><tr><th>#</th><th>Issue</th></tr></thead>
        <tbody>
          {{issueRows}}
        </tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    private static byte[] BuildBasicPdf(DeckCheckReport report)
    {
        var lines = new List<string>
        {
            report.ReportTitle,
            $"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"Status: {report.OverallStatus}",
            $"Total issues: {report.TotalIssues}",
            $"Slides scanned: {report.SlidesScanned}",
            $"Font issues: {report.FontIssues}",
            $"Term issues: {report.TermIssues}",
            $"Missing slide numbers: {report.MissingSlideNumbers}",
            $"Dense text slides: {report.DenseTextSlides}",
            $"Logo issues: {report.LogoIssues}",
            $"Logo position issues: {report.LogoPositionIssues}",
            $"Template: {report.TemplateName ?? "Default enterprise template"}",
            $"Brand colors: {NormalizeHexColor(report.BrandPrimaryColor, LogoPolicy.Default.BrandPrimaryColor)} / {NormalizeHexColor(report.BrandAccentColor, LogoPolicy.Default.BrandAccentColor)}",
            "",
            "Issues:"
        };

        lines.AddRange(report.Issues.Count == 0
            ? new[] { "No issues found." }
            : report.Issues.Select((issue, index) => $"{index + 1}. {issue}"));

        var content = BuildPdfContent(lines);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        writer.Flush();
        var xrefOffset = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            writer.Write($"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
        }

        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();
        return stream.ToArray();
    }

    private static string BuildPdfContent(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        builder.Append("BT\n/F1 11 Tf\n50 750 Td\n14 TL\n");
        foreach (var line in lines.Take(45))
        {
            builder.Append('(').Append(EscapePdfText(NormalizePdfText(line))).Append(") Tj\nT*\n");
        }
        builder.Append("ET");
        return builder.ToString();
    }

    private static string NormalizePdfText(string value) =>
        string.Concat(value.Select(ch => ch <= 0x7f ? ch : '?'));

    private static string EscapePdfText(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string ResolveReportPath(string? reportPath)
    {
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            var fullPath = Path.GetFullPath(reportPath);
            return Path.GetExtension(fullPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : Path.ChangeExtension(fullPath, ".pdf");
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModelForge",
            "Reports");
        return Path.Combine(root, $"deck-check-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
    }

    private static string[] SplitPipeDelimited(string value) =>
        value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsTruthy(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("yes", StringComparison.OrdinalIgnoreCase);

    private static float? ParseFloat(string value) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;

    private static string NormalizeHexColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith('#'))
        {
            normalized = $"#{normalized}";
        }

        return normalized.Length == 7 &&
               normalized.Skip(1).All(Uri.IsHexDigit)
            ? normalized.ToUpperInvariant()
            : fallback;
    }

    public sealed record LogoPolicy(
        string? TemplateName,
        float? MaxLeft,
        float? MaxTop,
        float? MaxWidth,
        float? MaxHeight)
    {
        public string ReportTitle { get; init; } = "ModelForge Deck Check Report";
        public string BrandPrimaryColor { get; init; } = "#1F3A5F";
        public string BrandAccentColor { get; init; } = "#3B82F6";
        public static LogoPolicy Default { get; } = new("Default enterprise template", 160, 90, 220, 120);
    }

    private static void HighlightTerm(dynamic textRange, string term)
    {
        try
        {
            string text = textRange.Text ?? "";
            int idx = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                dynamic found = textRange.Characters(idx + 1, term.Length);
                found.Font.Color.RGB = 0x0000FF; // Red
                found.Font.Underline = true;
                found.Font.Bold = -1;
                idx = text.IndexOf(term, idx + 1, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { /* non-critical */ }
    }

    private static bool LooksLikeLogo(dynamic shape)
    {
        try
        {
            string name = shape.Name ?? string.Empty;
            if (name.Contains("logo", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch { }

        try
        {
            int type = shape.Type;
            if (type == 13)
                return true;
        }
        catch { }

        return false;
    }

    private static bool LogoIsWithinPolicy(dynamic shape, LogoPolicy policy)
    {
        try
        {
            float left = Convert.ToSingle(shape.Left, CultureInfo.InvariantCulture);
            float top = Convert.ToSingle(shape.Top, CultureInfo.InvariantCulture);
            float width = Convert.ToSingle(shape.Width, CultureInfo.InvariantCulture);
            float height = Convert.ToSingle(shape.Height, CultureInfo.InvariantCulture);

            return (!policy.MaxLeft.HasValue || left <= policy.MaxLeft.Value) &&
                   (!policy.MaxTop.HasValue || top <= policy.MaxTop.Value) &&
                   (!policy.MaxWidth.HasValue || width <= policy.MaxWidth.Value) &&
                   (!policy.MaxHeight.HasValue || height <= policy.MaxHeight.Value);
        }
        catch
        {
            return true;
        }
    }
}
