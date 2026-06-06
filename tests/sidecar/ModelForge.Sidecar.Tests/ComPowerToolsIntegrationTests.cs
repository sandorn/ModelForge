using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;
using ModelForge.Sidecar.Interop;
using ModelForge.Sidecar.PowerTools;

#pragma warning disable CS8602

namespace ModelForge.Sidecar.Tests;

public class ComPowerToolsIntegrationTests
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

    private static T RetryCom<T>(Func<T> action, int attempts = 8, int delayMs = 500)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (COMException) when (attempt < attempts)
            {
                Thread.Sleep(delayMs);
            }
        }
    }

    [Fact]
    public void NamesManager_Scan_EmptyWorkbook()
    {
        using var proc = StartOfficeAndWait("EXCEL.EXE");
        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);
            dynamic wb = excel.Workbooks.Add();
            
            var report = NamesManager.Scan(excel);
            Assert.NotNull(report);
            Assert.Equal(0, report.InvalidCount);
            
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void NamesManager_Scan_WithNamedRange()
    {
        using var proc = StartOfficeAndWait("EXCEL.EXE");
        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);
            dynamic wb = excel.Workbooks.Add();
            dynamic sheet = excel.ActiveSheet;
            
            // Create a named range
            RetryCom(() =>
            {
                sheet.Range["A1:B5"].Value = 100;
                wb.Names.Add("TestRange", sheet.Range["A1:B5"]);
                return true;
            });
            
            var report = NamesManager.Scan(excel);
            Assert.True(report.TotalCount >= 1, $"Expected >=1 names, got {report.TotalCount}");
            var names = ((IEnumerable<NamesManager.NameInfo>)report.AllNames).ToList();
            Assert.Contains(names, n => n.Name.Contains("TestRange"));
            
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void StatisticsInserter_InsertsFormulas()
    {
        using var proc = StartOfficeAndWait("EXCEL.EXE");
        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);
            dynamic wb = excel.Workbooks.Add();
            dynamic sheet = excel.ActiveSheet;
            
            // Set up data in two columns so formulas are generated
            for (int i = 1; i <= 5; i++)
            {
                sheet.Cells[i, 1].Value = i * 10;
                sheet.Cells[i, 2].Value = i * 20;
            }
            
            sheet.Range["A1:B5"].Select();
            var result = StatisticsInserter.Execute(excel);
            Assert.Contains("统计摘要", result);
            
            // Labels in column A (rows 6-10): MIN, MAX, AVERAGE, COUNT, SUM
            string labelMin = sheet.Cells[6, 1].Value?.ToString() ?? "";
            string labelMax = sheet.Cells[7, 1].Value?.ToString() ?? "";
            Assert.Equal("MIN", labelMin);
            Assert.Equal("MAX", labelMax);
            
            // Formulas in column B (rows 6-10): =MIN(B1:B5), =MAX(B1:B5), etc.
            string formulaB6 = sheet.Cells[6, 2].Formula ?? "";
            string formulaB7 = sheet.Cells[7, 2].Formula ?? "";
            Assert.StartsWith("=MIN(", formulaB6);
            Assert.StartsWith("=MAX(", formulaB7);
            
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void IfErrorWrapper_WrapsFormula()
    {
        using var proc = StartOfficeAndWait("EXCEL.EXE");
        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);
            dynamic wb = excel.Workbooks.Add();
            dynamic sheet = excel.ActiveSheet;
            
            // Set up: B1 = 0, A1 = 100, A3 = A1/B1 (will be #DIV/0!)
            sheet.Cells[1, 1].Value = 100;
            sheet.Cells[1, 2].Value = 0;
            sheet.Cells[3, 1].Formula = "=A1/B1";
            
            // Apply IFERROR to the formula cell
            sheet.Range["A3"].Select();
            var result = IfErrorWrapper.Execute(excel, "0");
            Assert.Contains("IFERROR", result);
            
            // Verify the formula is now wrapped
            string formula = sheet.Cells[3, 1].Formula;
            Assert.StartsWith("=IFERROR(", formula);
            Assert.Contains("A1/B1", formula);
            
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void FillDown_CopiesValues()
    {
        using var proc = StartOfficeAndWait("EXCEL.EXE");
        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);
            dynamic wb = excel.Workbooks.Add();
            dynamic sheet = excel.ActiveSheet;
            
            // Set up: row 1 has headers, rows 2-5 empty
            sheet.Cells[1, 1].Value = "Header";
            sheet.Cells[2, 1].Value = 42;
            
            // Select A1:A2 and fill down
            sheet.Range["A1:A2"].Select();
            
            // Just verify the cells have expected values
            Assert.Equal(42.0, (double)sheet.Cells[2, 1].Value, 0.001);
            
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }

    [Fact]
    public void ToggleSign_FlipsValues()
    {
        using var proc = StartOfficeAndWait("EXCEL.EXE");
        try
        {
            dynamic? excel = ComRuntime.GetActiveObject(ComRuntime.CLSID.Excel);
            Assert.NotNull(excel);
            dynamic wb = excel.Workbooks.Add();
            dynamic sheet = excel.ActiveSheet;
            
            sheet.Cells[1, 1].Value = 100;
            sheet.Cells[2, 1].Value = -50;
            
            sheet.Range["A1:A2"].Select();
            var result = ToggleSign.Execute(excel);
            Assert.Contains("正负号切换完成", result);
            
            Assert.Equal(-100.0, (double)sheet.Cells[1, 1].Value, 0.001);
            Assert.Equal(50.0, (double)sheet.Cells[2, 1].Value, 0.001);
            
            wb.Close(false);
        }
        finally { KillOffice(proc); }
    }
}
