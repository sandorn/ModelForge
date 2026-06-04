using ModelForge.Backend.Services;
using ModelForge.Contracts;

var catalog = new CommandCatalog();
var commands = catalog.GetAll();

Ensure(commands.Count >= 20, "阶段一至少应定义 20 个 Excel 高频命令。 ");
Ensure(commands.All(command => command.Host == OfficeHost.Excel), "阶段一命令目录应优先聚焦 Excel。 ");
Ensure(commands.Any(command => command.Id == "excel.model-check"), "命令目录缺少 Model Check 入口。 ");
Ensure(commands.Any(command => command.Id == "excel.link-to-powerpoint"), "命令目录缺少 Excel → PowerPoint 链接入口。 ");

Console.WriteLine("ModelForge.Backend.SmokeTests: passed");

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine(message);
        Environment.ExitCode = 1;
        throw new InvalidOperationException(message);
    }
}
