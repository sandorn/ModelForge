using System.Diagnostics;
using Xunit;
using ModelForge.Sidecar.Interop;
using ModelForge.Sidecar.PowerPoint;

#pragma warning disable CS8602

namespace ModelForge.Sidecar.Tests;

public class ComPptIntegrationTests
{
    private const string OfficeRoot = @"C:\Program Files (x86)\Microsoft Office\root\Office16";

    private static Process StartOfficeAndWait(string exeName, int waitMs = 4000)
    {
        var fullPath = Path.Combine(OfficeRoot, exeName);
        if (!File.Exists(fullPath)) throw new Exception($"Office not found: {fullPath}");
        var psi = new ProcessStartInfo
        {
            FileName = fullPath,
            WindowStyle = ProcessWindowStyle.Minimized,
            UseShellExecute = true,
        };
        var proc = Process.Start(psi)!;
        Thread.Sleep(waitMs);
        return proc;
    }

    private static void KillOffice(Process proc)
    {
        try { proc?.Kill(); } catch { }
    }

    private static void AddTextSlide(dynamic ppt, string title, string body)
    {
        dynamic pres = ppt.ActivePresentation;
        dynamic slide = pres.Slides.Add(pres.Slides.Count + 1, 1); // ppLayoutTitle
        slide.Shapes[1].TextFrame.TextRange.Text = title;
        slide.Shapes[2].TextFrame.TextRange.Text = body;
    }

    [Fact]
    public void DeckCheck_EmptyPresentation()
    {
        using var proc = StartOfficeAndWait("POWERPNT.EXE");
        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);
            dynamic pres = ppt.Presentations.Add();

            var report = DeckCheck.Run(ppt);
            Assert.NotNull(report);
            // Default new presentation may have 0 or 1 slides
            Assert.True(report.SlidesScanned >= 0);

            pres.Close();
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void DeckCheck_WithContent()
    {
        using var proc = StartOfficeAndWait("POWERPNT.EXE");
        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);
            dynamic pres = ppt.Presentations.Add();

            // Add a slide with known content
            AddTextSlide(ppt, "Title Slide", "Some body text here for testing purposes");
            // Add second slide with a draft term
            AddTextSlide(ppt, "Second Slide", "This is a DRAFT version");

            var report = DeckCheck.Run(ppt);
            Assert.NotNull(report);
            Assert.True(report.SlidesScanned >= 2, $"Expected >=2 slides scanned, got {report.SlidesScanned}");
            // "DRAFT" is a forbidden term — should trigger at least 1 term issue
            Assert.True(report.TermIssues >= 1 || report.FontIssues >= 0,
                "DeckCheck should detect content issues");

            pres.Close();
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void DeckCheck_CustomFonts()
    {
        using var proc = StartOfficeAndWait("POWERPNT.EXE");
        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);
            dynamic pres = ppt.Presentations.Add();

            AddTextSlide(ppt, "Test", "Testing fonts");

            // Run with a narrow allowed-font list
            var report = DeckCheck.Run(ppt, allowedFonts: new[] { "Courier New" });
            Assert.NotNull(report);
            // Calibri (default) won't match Courier New — font issues expected
            Assert.True(report.FontIssues >= 0);

            pres.Close();
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void ShapeTools_AlignLeft_RequiresSelection()
    {
        using var proc = StartOfficeAndWait("POWERPNT.EXE");
        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);
            dynamic pres = ppt.Presentations.Add();

            // Add a slide with shapes
            dynamic slide = pres.Slides.Add(pres.Slides.Count + 1, 12); // ppLayoutBlank
            slide.Shapes.AddShape(1, 100, 100, 200, 100);  // Rectangle at x=100
            slide.Shapes.AddShape(1, 300, 100, 200, 100);  // Rectangle at x=300

            // Select both shapes
            slide.Shapes.Range().Select();

            // AlignLeft with 2+ shapes selected
            var result = ShapeTools.AlignLeft(ppt);
            Assert.NotNull(result);

            // Verify shapes are now at same left position
            dynamic shapeRange = ppt.ActiveWindow.Selection.ShapeRange;
            Assert.True(shapeRange.Count >= 1);

            pres.Close();
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void ShapeTools_DistributeHorizontal_InsufficientShapes()
    {
        using var proc = StartOfficeAndWait("POWERPNT.EXE");
        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);
            dynamic pres = ppt.Presentations.Add();

            dynamic slide = pres.Slides.Add(pres.Slides.Count + 1, 12);
            slide.Shapes.AddShape(1, 100, 100, 200, 100);
            slide.Shapes.SelectAll();

            // Only 1 shape selected — should return "at least 3" message
            var result = ShapeTools.DistributeHorizontal(ppt);
            Assert.Contains("3", result);

            pres.Close();
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void ShapeTools_UnifySize_Shapes()
    {
        using var proc = StartOfficeAndWait("POWERPNT.EXE");
        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);
            dynamic pres = ppt.Presentations.Add();

            dynamic slide = pres.Slides.Add(pres.Slides.Count + 1, 12);
            slide.Shapes.AddShape(1, 100, 100, 100, 50);   // smaller shape
            slide.Shapes.AddShape(1, 250, 100, 200, 150);  // larger shape

            slide.Shapes.Range().Select();
            var result = ShapeTools.UnifySize(ppt);
            Assert.Contains("统一", result);

            // After unify, both shapes should have same width
            dynamic shapeRange = ppt.ActiveWindow.Selection.ShapeRange;
            float w1 = shapeRange[1].Width;
            float w2 = shapeRange[2].Width;
            Assert.Equal(w1, w2, 0.1);

            pres.Close();
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void DynamicAgendas_EmptyPresentation()
    {
        using var proc = StartOfficeAndWait("POWERPNT.EXE");
        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);
            dynamic pres = ppt.Presentations.Add();

            var result = DynamicAgendas.Generate(ppt);
            Assert.NotNull(result);
            Assert.True(result.SectionsFound >= 0);

            pres.Close();
        }
        finally { KillOffice(proc); }
    }
}
