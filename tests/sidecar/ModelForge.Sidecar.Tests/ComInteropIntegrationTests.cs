using System.Diagnostics;
using Xunit;
using ModelForge.Sidecar.Interop;

namespace ModelForge.Sidecar.Tests;

public class ComInteropIntegrationTests
{
    private const string OfficeRoot = @"C:\Program Files (x86)\Microsoft Office\root\Office16";

    private static bool TryStartOffice(string exeName, int waitMs, out Process proc)
    {
        proc = null!;
        var fullPath = Path.Combine(OfficeRoot, exeName);
        if (!File.Exists(fullPath)) return false;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fullPath,
                WindowStyle = ProcessWindowStyle.Minimized,
                UseShellExecute = true,
            };
            proc = Process.Start(psi);
            if (proc == null) return false;
            Thread.Sleep(waitMs);
            return !proc.HasExited;
        }
        catch { return false; }
    }

    private static void KillOffice(Process proc)
    {
        try { proc?.Kill(); } catch { }
    }

    [Fact]
    public void Diagnostic_CheckOfficePaths()
    {
        var root = OfficeRoot;
        Assert.True(Directory.Exists(root), $"Office root not found: {root}");
        Assert.True(File.Exists(Path.Combine(root, "EXCEL.EXE")), "EXCEL.EXE not found");
        Assert.True(File.Exists(Path.Combine(root, "POWERPNT.EXE")), "POWERPNT.EXE not found");
        Assert.True(File.Exists(Path.Combine(root, "WINWORD.EXE")), "WINWORD.EXE not found");
    }

    [Fact]
    public void Excel_ConnectAndReadWrite()
    {
        if (!TryStartOffice("EXCEL.EXE", 4000, out var proc))
            return;

        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);

            dynamic wb = excel.Workbooks.Add();
            dynamic sheet = excel.ActiveSheet;
            sheet.Cells[1, 1].Value = 42;
            sheet.Cells[1, 2].Formula = "=A1*2";

            Assert.Equal(42.0, (double)sheet.Cells[1, 1].Value, 0.001);
            Assert.Equal(84.0, (double)sheet.Cells[1, 2].Value, 0.001);
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void Excel_AutoFillIntegration()
    {
        if (!TryStartOffice("EXCEL.EXE", 4000, out var proc))
            return;

        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);

            dynamic wb = excel.Workbooks.Add();
            dynamic sheet = excel.ActiveSheet;
            sheet.Cells[1, 1].Value = 10;
            sheet.Range["A1"].AutoFill(sheet.Range["A1:A5"], 1); // xlFillCopy = 1 (copy values, don't series)

            Assert.Equal(10.0, (double)sheet.Cells[5, 1].Value, 0.001);
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void PowerPoint_ConnectAndCreate()
    {
        if (!TryStartOffice("POWERPNT.EXE", 4000, out var proc))
            return;

        try
        {
            dynamic? ppt = ComRuntime.GetActiveObject(ComRuntime.CLSID.PowerPoint);
            Assert.NotNull(ppt);

            dynamic pres = ppt.Presentations.Add();
            // Office 2024 may start with 0 or 1 slides
            int initialCount = pres.Slides.Count;
            dynamic newSlide = pres.Slides.Add(1, 1); // ppLayoutTitle
            Assert.NotNull(newSlide);
            Assert.True(pres.Slides.Count > initialCount, 
                $"Expected slides to increase from {initialCount}, got {pres.Slides.Count}");
            pres.Close();
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void Word_ConnectAndType()
    {
        if (!TryStartOffice("WINWORD.EXE", 4000, out var proc))
            return;

        try
        {
            dynamic? word = ComRuntime.GetActiveObject(ComRuntime.CLSID.Word);
            Assert.NotNull(word);

            dynamic doc = word.Documents.Add();
            dynamic range = doc.Content;
            range.Text = "ModelForge COM Test";
            Assert.Contains("ModelForge", (string)range.Text);
            doc.Close(false);
        }
        finally { KillOffice(proc); }
    }
}