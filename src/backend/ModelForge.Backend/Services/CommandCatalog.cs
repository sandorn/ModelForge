using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

public interface ICommandCatalog
{
    IReadOnlyCollection<CommandDefinition> GetAll();

    CommandDefinition? FindById(string commandId);
}

public sealed class CommandCatalog : ICommandCatalog
{
    private readonly IReadOnlyCollection<CommandDefinition> _commands = new[]
    {
        WordCommand("word.build-due-diligence", "Generate DD Checklist", "Word", "Ctrl+Alt+Shift+D", "Generate due diligence checklist template."),
        WordCommand("word.build-cim", "Generate CIM Memo", "Word", "Ctrl+Alt+Shift+I", "Generate confidential information memorandum template."),
        WordCommand("word.build-management-presentation", "Generate Mgmt Presentation", "Word", "Ctrl+Alt+Shift+G", "Generate management presentation outline template."),
        WordCommand("word.embed-excel-range", "Embed Excel Range", "Word", "Ctrl+Alt+Shift+K", "Embed selected Excel range into active Word document."),
        WordCommand("word.refresh-links", "Refresh Word Links", "Word", "Ctrl+Alt+Shift+U", "Refresh embedded Excel links in Word document."),
        PptCommand("ppt.generate-agenda", "Generate Agenda", "PPT", "Ctrl+Alt+Shift+A", "Auto-generate agenda slide from PPT sections."),
        PptCommand("ppt.deck-check", "Deck Check", "PPT", "Ctrl+Alt+Shift+M", "Scan presentation for font, term and compliance issues."),
        PptCommand("ppt.align-left", "Align Shapes Left", "PPT", "Ctrl+Alt+Shift+L", "Align selected shapes to the leftmost shape."),
        PptCommand("ppt.align-center", "Align Shapes Center", "PPT", "Ctrl+Alt+Shift+C", "Align selected shapes to their horizontal center."),
        PptCommand("ppt.align-right", "Align Shapes Right", "PPT", "Ctrl+Alt+Shift+R", "Align selected shapes to the rightmost shape."),
        PptCommand("ppt.align-top", "Align Shapes Top", "PPT", "Ctrl+Alt+Shift+T", "Align selected shapes to the top edge."),
        PptCommand("ppt.align-middle", "Align Shapes Middle", "PPT", "Ctrl+Alt+Shift+E", "Align selected shapes to their vertical middle."),
        PptCommand("ppt.align-bottom", "Align Shapes Bottom", "PPT", "Ctrl+Alt+Shift+B", "Align selected shapes to the bottom edge."),
        PptCommand("ppt.distribute-horizontal", "Distribute Horizontal", "PPT", "Ctrl+Alt+Shift+H", "Distribute selected shapes evenly horizontally."),
        PptCommand("ppt.distribute-vertical", "Distribute Vertical", "PPT", "Ctrl+Alt+Shift+V", "Distribute selected shapes evenly vertically."),
        PptCommand("ppt.unify-width", "Unify Shape Width", "PPT", "Ctrl+Alt+Shift+W", "Resize selected shapes to match the first shape width."),
        PptCommand("ppt.unify-height", "Unify Shape Height", "PPT", "Ctrl+Alt+Shift+Y", "Resize selected shapes to match the first shape height."),
        PptCommand("ppt.unify-size", "Unify Shape Size", "PPT", "Ctrl+Alt+Shift+Z", "Resize all selected shapes to match the first shape dimensions."),

        ExcelCommand("excel.fill-right", "快速向右填充", "Power Tools", "Ctrl+Alt+R", "将当前公式或格式向右扩展到选定区域。"),
        ExcelCommand("excel.fill-down", "快速向下填充", "Power Tools", "Ctrl+Alt+D", "将当前公式或格式向下扩展到选定区域。"),
        ExcelCommand("excel.wrap-iferror", "IFERROR 封装", "Power Tools", "Ctrl+Alt+E", "为选中公式添加 IFERROR 包裹。"),
        ExcelCommand("excel.insert-statistics", "插入统计摘要", "Power Tools", "Ctrl+Alt+S", "为选定区域插入 MIN/MAX/AVERAGE/COUNT/SUM。"),
        ExcelCommand("excel.visualize-inputs", "标记硬编码输入", "Visualizations", "Ctrl+Alt+I", "扫描并标记硬编码单元格。"),
        ExcelCommand("excel.visualize-formulas", "标记公式", "Visualizations", "Ctrl+Alt+F", "扫描并标记公式单元格。"),
        ExcelCommand("excel.visualize-links", "标记外部链接", "Visualizations", "Ctrl+Alt+L", "扫描并标记外部链接。"),
        ExcelCommand("excel.clear-visualizations", "清除审计标色", "Visualizations", "Ctrl+Alt+C", "清除 ModelForge 视觉审计配色。"),
        ExcelCommand("excel.model-check", "运行 Model Check", "Model Check", "Ctrl+Alt+M", "扫描错误值、硬编码、外部链接和循环引用。"),
        ExcelCommand("excel.trace-precedents", "追踪前驱", "Formula Trace", "Ctrl+Alt+P", "高亮当前单元格的直接前驱。"),
        ExcelCommand("excel.trace-dependents", "追踪依赖", "Formula Trace", "Ctrl+Alt+T", "高亮当前单元格的直接依赖。"),
        ExcelCommand("excel.clear-trace", "清除追踪", "Formula Trace", "Ctrl+Alt+X", "清除公式追踪标记。"),
        ExcelCommand("excel.optimize-workbook", "优化工作簿", "Workbook", "Ctrl+Alt+O", "清理未使用样式、无效名称和过界残留。"),
        ExcelCommand("excel.prepare-to-share", "安全外发副本", "Workbook", "Ctrl+Alt+H", "生成移除公式和敏感元数据的外发副本。"),
        ExcelCommand("excel.apply-finance-format", "应用财务格式", "Formatting", "Ctrl+Alt+N", "应用千分位、会计格式或百分比格式。"),
        ExcelCommand("excel.toggle-sign", "切换正负号", "Formatting", "Ctrl+Alt+G", "对选中数字区域切换符号。"),
        ExcelCommand("excel.insert-dcf-template", "插入 DCF 模板", "Templates", "Ctrl+Alt+1", "插入基础 DCF 估值表模板。"),
        ExcelCommand("excel.names-manager", "Names Manager", "Workbook", "Ctrl+Alt+W", "Scan and clean named ranges in active workbook."),
        ExcelCommand("excel.link-to-powerpoint", "链接到 PowerPoint", "Linking", "Ctrl+Alt+K", "将选中 Range 或 Chart 链接到 PowerPoint。"),
        ExcelCommand("excel.refresh-links", "刷新 Office 链接", "Linking", "Ctrl+Alt+U", "刷新当前工作簿关联的 PPT/Word 链接。"),
        ExcelCommand("excel.open-task-pane", "打开任务窗格", "Bridge", "Ctrl+Alt+B", "打开 Web Add-in 管理与配置任务窗格。")
    };

    public IReadOnlyCollection<CommandDefinition> GetAll()
    {
        return _commands;
    }

    public CommandDefinition? FindById(string commandId)
    {
        return _commands.FirstOrDefault(command => string.Equals(command.Id, commandId, StringComparison.OrdinalIgnoreCase));
    }

    private static CommandDefinition PptCommand(string id, string displayName, string category, string shortcut, string description)
    {
        return new CommandDefinition
        {
            Id = id,
            DisplayName = displayName,
            Host = OfficeHost.PowerPoint,
            Target = CommandExecutionTarget.Sidecar,
            Category = category,
            DefaultShortcut = shortcut,
            Description = description
        };
    }

    private static CommandDefinition ExcelCommand(string id, string displayName, string category, string shortcut, string description)
    {
        return new CommandDefinition
        {
            Id = id,
            DisplayName = displayName,
            Host = OfficeHost.Excel,
            Target = CommandExecutionTarget.Sidecar,
            Category = category,
            DefaultShortcut = shortcut,
            Description = description
        };
    }

    private static CommandDefinition WordCommand(string id, string displayName, string category, string shortcut, string description)
    {
        return new CommandDefinition
        {
            Id = id,
            DisplayName = displayName,
            Host = OfficeHost.Word,
            Target = CommandExecutionTarget.Sidecar,
            Category = category,
            DefaultShortcut = shortcut,
            Description = description
        };
    }

}
