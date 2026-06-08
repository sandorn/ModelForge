using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests;

/// <summary>
/// Smoke tests that validate the backend command catalog meets Phase A requirements.
/// These were originally a console Program.cs; now converted to xUnit.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void CommandCatalog_HasAtLeast20ExcelCommands()
    {
        var catalog = new CommandCatalog();
        var commands = catalog.GetAll();

        var excelCommands = commands.Where(c => c.Host == OfficeHost.Excel).ToList();
        Assert.True(excelCommands.Count >= 45,
            $"Expected at least 45 Excel commands. Got {excelCommands.Count}.");
    }

    [Fact]
    public void CommandCatalog_PrioritizesExcel()
    {
        var catalog = new CommandCatalog();
        var commands = catalog.GetAll();

        Assert.NotEmpty(commands);
        Assert.True(commands.Any(c => c.Host == OfficeHost.Excel), "Should contain Excel commands");
        Assert.True(commands.Any(c => c.Host == OfficeHost.Word), "Should contain Word commands");
        Assert.True(commands.Any(c => c.Host == OfficeHost.PowerPoint), "Should contain PPT commands");
    }

    [Fact]
    public void CommandCatalog_HasModelCheck()
    {
        var catalog = new CommandCatalog();
        Assert.NotNull(catalog.FindById("excel.model-check"));
    }

    [Fact]
    public void CommandCatalog_HasLinkToPowerPoint()
    {
        var catalog = new CommandCatalog();
        Assert.NotNull(catalog.FindById("excel.link-to-powerpoint"));
    }
}
