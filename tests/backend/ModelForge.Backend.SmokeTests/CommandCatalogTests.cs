using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests.Services;

public class CommandCatalogTests
{
    [Fact]
    public void GetAll_ReturnsAtLeast20ExcelCommands()
    {
        var catalog = new CommandCatalog();
        var commands = catalog.GetAll();

        var excelCommands = commands.Where(c => c.Host == OfficeHost.Excel).ToList();
        Assert.True(excelCommands.Count >= 20,
            $"Expected at least 20 Excel commands, got {excelCommands.Count}");
    }

    [Fact]
    public void GetAll_ContainsModelCheckCommand()
    {
        var catalog = new CommandCatalog();
        var commands = catalog.GetAll();

        Assert.Contains(commands, c => c.Id == "excel.model-check");
    }

    [Fact]
    public void GetAll_ContainsLinkToPowerPointCommand()
    {
        var catalog = new CommandCatalog();
        var commands = catalog.GetAll();

        Assert.Contains(commands, c => c.Id == "excel.link-to-powerpoint");
    }

    [Fact]
    public void GetAll_AllCommandsHaveUniqueIds()
    {
        var catalog = new CommandCatalog();
        var commands = catalog.GetAll();

        var ids = commands.Select(c => c.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void GetAll_AllCommandsHaveRequiredFields()
    {
        var catalog = new CommandCatalog();
        var commands = catalog.GetAll();

        foreach (var cmd in commands)
        {
            Assert.False(string.IsNullOrWhiteSpace(cmd.Id), $"Command has empty Id");
            Assert.False(string.IsNullOrWhiteSpace(cmd.DisplayName), $"Command {cmd.Id} has empty DisplayName");
            Assert.False(string.IsNullOrWhiteSpace(cmd.Category), $"Command {cmd.Id} has empty Category");
            Assert.False(string.IsNullOrWhiteSpace(cmd.Description), $"Command {cmd.Id} has empty Description");
        }
    }

    [Fact]
    public void FindById_ExistingCommand_ReturnsCommand()
    {
        var catalog = new CommandCatalog();

        var cmd = catalog.FindById("excel.fill-down");

        Assert.NotNull(cmd);
        Assert.Equal("excel.fill-down", cmd.Id);
        Assert.Equal(OfficeHost.Excel, cmd.Host);
    }

    [Fact]
    public void FindById_NonExistentCommand_ReturnsNull()
    {
        var catalog = new CommandCatalog();

        var cmd = catalog.FindById("nonexistent.command");

        Assert.Null(cmd);
    }
}
