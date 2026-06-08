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
        WordCommand("word.insert-row-above", "Insert Row Above", "Word", "Ctrl+Alt+Shift+J", "Insert a table row above current cell."),
        WordCommand("word.insert-row-below", "Insert Row Below", "Word", "Ctrl+Alt+Shift+N", "Insert a table row below current cell."),
        WordCommand("word.insert-column-left", "Insert Column Left", "Word", "Ctrl+Alt+Shift+9", "Insert a table column left of current cell."),
        WordCommand("word.insert-column-right", "Insert Column Right", "Word", "Ctrl+Alt+Shift+0", "Insert a table column right of current cell."),
        WordCommand("word.insert-sum-formula", "Insert SUM Formula", "Word", "Ctrl+Alt+Shift+-", "Insert =SUM(ABOVE) formula in current cell."),
        WordCommand("word.document-outline", "Document Outline", "Word", "Ctrl+Alt+Shift+O", "Show document heading outline."),
        WordCommand("word.goto-heading", "Go to Heading", "Word", "", "Navigate to a specific heading."),
        WordCommand("word.insert-page-break", "Insert Page Break", "Word", "", "Insert a page break at cursor."),
        WordCommand("word.insert-section-break-next", "Insert Section Break (Next Page)", "Word", "", "Insert a next-page section break."),
        WordCommand("word.insert-toc", "Insert Table of Contents", "Word", "", "Insert automatic TOC at document start."),
        WordCommand("word.insert-cover-page", "Insert Cover Page", "Word", "", "Insert a simple cover page."),
        PptCommand("ppt.apply-appear", "Apply Appear Animation", "Animation", "", "Apply appear animation to selected shape."),
        PptCommand("ppt.apply-fade", "Apply Fade Animation", "Animation", "", "Apply fade animation to selected shape."),
        PptCommand("ppt.apply-fly-in", "Apply Fly-In Animation", "Animation", "", "Apply fly-in animation from bottom."),
        PptCommand("ppt.clear-animations", "Clear Animations", "Animation", "", "Remove all animations from selected shape."),
        PptCommand("ppt.export-to-images", "Export to PNG", "Export", "", "Export all slides as PNG images."),
        PptCommand("ppt.apply-layout", "Apply Layout", "Layout", "", "Apply a slide layout by name or index."),
        PptCommand("ppt.duplicate-slide", "Duplicate Slide", "Slides", "", "Duplicate the current slide."),
        PptCommand("ppt.move-slide", "Move Slide", "Slides", "", "Move current slide to a new position."),
        PptCommand("ppt.set-background-color", "Set Background Color", "Design", "", "Set slide background to a solid color."),
        PptCommand("ppt.reset-background", "Reset Background", "Design", "", "Reset slide background to follow master."),
        PptCommand("ppt.apply-transition-fade", "Apply Fade Transition", "Animation", "", "Apply fade transition to all slides."),
        PptCommand("ppt.apply-transition-push", "Apply Push Transition", "Animation", "", "Apply push transition to all slides."),
        PptCommand("ppt.send-to-front", "Send to Front", "Arrange", "", "Bring selected shape to front."),
        PptCommand("ppt.send-to-back", "Send to Back", "Arrange", "", "Send selected shape to back."),
        ExcelCommand("excel.auto-sum", "AutoSum", "Editing", "Ctrl+Alt+=", "Insert SUM formula below selection."),
        WordCommand("word.set-margins", "Set Page Margins", "Word", "", "Set page margins (in points)."),
        WordCommand("word.set-orientation", "Set Orientation", "Word", "", "Set page orientation (0=portrait, 1=landscape)."),
        WordCommand("word.apply-heading", "Apply Heading Style", "Word", "", "Apply Heading 1/2/3 to current paragraph."),
        WordCommand("word.apply-normal-style", "Apply Normal Style", "Word", "", "Apply Normal style to current paragraph."),
        ExcelCommand("excel.paste-values", "Paste Values Only", "Editing", "Ctrl+Alt+V", "Paste copied data as values only."),
        ExcelCommand("excel.remove-duplicates", "Remove Duplicates", "Data", "", "Remove duplicate rows from selection."),
        ExcelCommand("excel.freeze-panes", "Freeze Panes", "View", "Ctrl+Alt+A", "Freeze panes at selected cell."),
        ExcelCommand("excel.unfreeze-panes", "Unfreeze Panes", "View", "", "Unfreeze all panes."),
        ExcelCommand("excel.set-print-area", "Set Print Area", "Print", "", "Set print area to selection."),
        ExcelCommand("excel.clear-print-area", "Clear Print Area", "Print", "", "Clear print area."),
        WordCommand("word.find-replace", "Find and Replace", "Word", "", "Find and replace text in document."),
        WordCommand("word.doc-stats", "Document Stats", "Word", "", "Show document statistics (words, pages, etc.)."),
        ExcelCommand("excel.apply-heat-map", "Apply Heat Map", "Conditional", "", "Apply 3-color heat map scale."),
        ExcelCommand("excel.apply-data-bars", "Apply Data Bars", "Conditional", "", "Apply data bars to selection."),
        ExcelCommand("excel.apply-icon-set", "Apply Icon Set", "Conditional", "", "Apply traffic light icon set."),
        ExcelCommand("excel.apply-top-10", "Apply Top 10", "Conditional", "", "Highlight top 10 values."),
        ExcelCommand("excel.clear-conditional-formats", "Clear Conditional Formats", "Conditional", "", "Clear all conditional formatting."),
        ExcelCommand("excel.insert-xirr-template", "Insert XIRR Template", "Finance", "", "Insert XIRR/NPV calculation template."),
        ExcelCommand("excel.insert-lbo-template", "Insert LBO Template", "Finance", "", "Insert simplified LBO model template."),
        ExcelCommand("excel.add-dropdown", "Add Dropdown List", "Data", "", "Add dropdown data validation to selection."),
        ExcelCommand("excel.add-numeric-range", "Add Numeric Range", "Data", "", "Add numeric range validation."),
        ExcelCommand("excel.clear-validation", "Clear Validation", "Data", "", "Clear data validation from selection."),
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
        PptCommand("ppt.harvey-ball", "Insert Harvey Ball", "TurboShapes", "Ctrl+Alt+Shift+1", "Insert a Harvey Ball quadrant circle (0-4)."),
        PptCommand("ppt.progress-bar", "Insert Progress Bar", "TurboShapes", "Ctrl+Alt+Shift+2", "Insert a percentage progress bar."),
        PptCommand("ppt.rating-stars", "Insert Rating Stars", "TurboShapes", "Ctrl+Alt+Shift+3", "Insert a 1-5 star rating display."),
        PptCommand("ppt.rotate-clockwise", "Rotate Clockwise", "Arrange", "Ctrl+Alt+Shift+4", "Rotate selected shapes 90° clockwise."),
        PptCommand("ppt.rotate-counterclockwise", "Rotate Counter-Clockwise", "Arrange", "Ctrl+Alt+Shift+5", "Rotate selected shapes 90° counter-clockwise."),
        PptCommand("ppt.swap-positions", "Swap Positions", "Arrange", "Ctrl+Alt+Shift+6", "Swap positions of exactly two selected shapes."),
        PptCommand("ppt.add-slide-numbers", "Add Slide Numbers", "Pagination", "Ctrl+Alt+Shift+7", "Add page numbers to all slides."),
        PptCommand("ppt.remove-slide-numbers", "Remove Slide Numbers", "Pagination", "Ctrl+Alt+Shift+8", "Remove page numbers from all slides."),
        PptCommand("ppt.insert-tombstone", "Insert Tombstone", "Templates", "Ctrl+Alt+Shift+X", "Insert a deal tombstone announcement template."),
        PptCommand("ppt.search-by-font", "Search by Font", "Reformat", "Ctrl+Alt+Shift+F", "Search all shapes by font name."),
        PptCommand("ppt.replace-font", "Replace Font", "Reformat", "Ctrl+Alt+Shift+P", "Batch replace font across all slides."),
        PptCommand("ppt.search-by-font-size", "Search by Font Size", "Reformat", "Ctrl+Alt+Shift+S", "Search shapes by font size."),
        PptCommand("ppt.replace-font-size", "Replace Font Size", "Reformat", "Ctrl+Alt+Shift+.", "Batch replace font size across all slides."),
        PptCommand("ppt.new-from-template", "New from Template", "File", "Ctrl+Alt+Shift+;", "Create new presentation from .potx template."),
        PptCommand("ppt.list-templates", "List PPT Templates", "File", "Ctrl+Alt+Shift+Q", "List available .potx template files."),
        PptCommand("ppt.insert-logo", "Insert Logo", "Brand", "", "Insert a logo image from file."),
        PptCommand("ppt.add-logo-all-slides", "Add Logo to All Slides", "Brand", "", "Add logo to every slide."),
        PptCommand("ppt.save-master-shape", "Save Master Shape", "MasterShapes", "", "Save selected shape to MasterShapes library."),
        PptCommand("ppt.list-master-shapes", "List Master Shapes", "MasterShapes", "", "List saved master shapes."),
        PptCommand("ppt.insert-master-shape", "Insert Master Shape", "MasterShapes", "", "Insert a saved master shape."),
        PptCommand("ppt.set-shape-meta", "Set Shape Meta", "Meta", "", "Attach key-value metadata to selected shape."),
        PptCommand("ppt.get-shape-meta", "Get Shape Meta", "Meta", "", "Read metadata from selected shape."),
        PptCommand("ppt.search-by-meta", "Search by Meta", "Meta", "", "Search shapes by metadata key/value."),
        PptCommand("ppt.remove-shape-meta", "Remove Shape Meta", "Meta", "", "Remove metadata key from shape."),
        PptCommand("ppt.list-sections", "List Sections", "Sections", "", "List all presentation sections."),
        PptCommand("ppt.add-section", "Add Section", "Sections", "", "Add a new section before current slide."),
        PptCommand("ppt.rename-section", "Rename Section", "Sections", "", "Rename a section."),
        PptCommand("ppt.delete-section", "Delete Section", "Sections", "", "Delete a section (keeps slides)."),

        ExcelCommand("excel.hide-empty-sheets", "Hide Empty Sheets", "Workbook", "", "Hide all empty worksheets."),
        ExcelCommand("excel.correlation", "Correlation", "Statistics", "Ctrl+Alt+Q", "Calculate Pearson correlation between two columns."),
        ExcelCommand("excel.descriptive-stats", "Descriptive Statistics", "Statistics", "Ctrl+Alt+Y", "Generate descriptive statistics summary."),
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
        ExcelCommand("excel.analyze-formula", "公式简化分析", "Power Tools", "Ctrl+Alt+Z", "分析选中公式并给出简化/优化建议。"),
        ExcelCommand("excel.save-template", "Save as Template", "Templates", "Ctrl+Alt+8", "Save selected range as reusable template."),
        ExcelCommand("excel.list-templates", "List Templates", "Templates", "Ctrl+Alt+9", "List all saved user templates."),
        ExcelCommand("excel.insert-template", "Insert Template", "Templates", "Ctrl+Alt+J", "Insert a saved template at cursor position."),
        ExcelCommand("excel.insert-dcf-template", "插入 DCF 模板", "Templates", "Ctrl+Alt+1", "插入基础 DCF 估值表模板。"),
        ExcelCommand("excel.insert-bs-template", "插入 BS 期权模板", "Templates", "Ctrl+Alt+2", "插入 Black-Scholes 期权定价模板。"),
        ExcelCommand("excel.insert-column-chart", "Quick Column Chart", "Charts", "Ctrl+Alt+3", "Insert a clustered column chart from selected data."),
        ExcelCommand("excel.insert-line-chart", "Quick Line Chart", "Charts", "Ctrl+Alt+4", "Insert a line chart from selected data."),
        ExcelCommand("excel.unify-chart-sizes", "Unify Chart Sizes", "Charts", "", "Unify all chart dimensions in workbook."),
        ExcelCommand("excel.unify-chart-styles", "Unify Chart Styles", "Charts", "", "Apply consistent style to all charts."),
        ExcelCommand("excel.add-series-to-chart", "Add Series to Chart", "Charts", "", "Add selected data as new series to chart."),
        ExcelCommand("excel.link-to-word", "Link to Word", "Linking", "", "Copy selected range to Word document."),
        ExcelCommand("excel.unhide-all-sheets", "Unhide All Sheets", "Workbook", "Ctrl+Alt+5", "Make all hidden worksheets visible."),
        ExcelCommand("excel.list-sheets", "List Sheets", "Workbook", "Ctrl+Alt+6", "List all worksheets with visibility status."),
        ExcelCommand("excel.names-manager", "Names Manager", "Workbook", "Ctrl+Alt+W", "Scan and clean named ranges in active workbook."),
        ExcelCommand("excel.link-to-powerpoint", "链接到 PowerPoint", "Linking", "Ctrl+Alt+K", "将选中 Range 链接到 PowerPoint。"),
        ExcelCommand("excel.link-chart-to-powerpoint", "链接图表到 PPT", "Linking", "", "将选中 Chart 链接到 PowerPoint。"),
        ExcelCommand("excel.refresh-links", "刷新 Office 链接", "Linking", "Ctrl+Alt+U", "刷新当前工作簿关联的 PPT/Word 链接。"),
        ExcelCommand("excel.repair-links", "修复失效链接", "Linking", "Ctrl+Alt+7", "诊断并尝试修复失效的 PowerPoint/Excel 链接。"),
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
