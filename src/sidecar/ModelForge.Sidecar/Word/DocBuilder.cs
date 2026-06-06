namespace ModelForge.Sidecar.Word;

/// <summary>
/// Doc Builder — 基于结构化数据生成 Word 文档初稿。
/// 支持插入标题、正文段落、表格和目录。
/// </summary>
public static class DocBuilder
{
    public sealed class DocTemplate
    {
        public string Title { get; set; } = "Untitled Document";
        public List<DocSection> Sections { get; set; } = new();
    }

    public sealed class DocSection
    {
        public string Heading { get; set; } = "";
        public string? Body { get; set; }
        public List<string[]>? Table { get; set; }
    }

    /// <summary>
    /// 根据模板在活动 Word 文档中插入结构化内容。
    /// </summary>
    public static string Build(dynamic wordApp, DocTemplate template)
    {
        dynamic document = wordApp.ActiveDocument;
        if (document == null) return "Word 中没有打开的文档。";

        dynamic selection = wordApp.Selection;

        // 标题
        selection.Font.Size = 18;
        selection.Font.Bold = 1;
        selection.TypeText(template.Title);
        selection.TypeParagraph();
        selection.TypeParagraph();

        // 各节
        foreach (var section in template.Sections)
        {
            // 小节标题
            selection.Font.Size = 14;
            selection.Font.Bold = 1;
            selection.TypeText(section.Heading);
            selection.TypeParagraph();

            // 正文
            if (!string.IsNullOrWhiteSpace(section.Body))
            {
                selection.Font.Size = 11;
                selection.Font.Bold = 0;
                selection.TypeText(section.Body);
                selection.TypeParagraph();
            }

            // 表格
            if (section.Table?.Count > 0)
            {
                int rows = section.Table.Count;
                int cols = section.Table[0].Length;
                dynamic table = document.Tables.Add(selection.Range, rows, cols);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols && c < section.Table[r].Length; c++)
                    {
                        table.Cell(r + 1, c + 1).Range.Text = section.Table[r][c];
                    }
                }
                selection.TypeParagraph();
            }

            selection.TypeParagraph();
        }

        return $"文档 '{template.Title}' 已生成：{template.Sections.Count} 节。";
    }

    /// <summary>
    /// 生成标准尽调文档模板。
    /// </summary>
    public static DocTemplate CreateDueDiligenceTemplate(string companyName = "目标公司")
    {
        return new DocTemplate
        {
            Title = $"{companyName} — 尽职调查清单",
            Sections = new List<DocSection>
            {
                new() { Heading = "1. 公司基本信息", Body = "本节包含公司注册信息、股权结构、组织架构等基本资料。" },
                new() { Heading = "2. 财务报表审查", Body = "审查最近三年审计报告、管理报表及关键财务指标。" },
                new() { Heading = "3. 法律合规", Body = "核查公司涉诉情况、合规记录及重大合同。" },
                new() { Heading = "4. 风险评估", Body = "识别主要经营风险、财务风险及合规风险。" },
                new()
                {
                    Heading = "5. 关键数据摘要",
                    Table = new List<string[]>
                    {
                        new[] { "指标", "2024", "2025", "2026(E)" },
                        new[] { "收入 (百万)", "-", "-", "-" },
                        new[] { "EBITDA (百万)", "-", "-", "-" },
                        new[] { "净利润 (百万)", "-", "-", "-" },
                    }
                },
            }
        };
    }

    public static DocTemplate CreateCimTemplate(string companyName = "")
    {
        if (string.IsNullOrWhiteSpace(companyName)) companyName = "目标公司";
        return new DocTemplate
        {
            Title = companyName + " - CIM",
            Sections = new List<DocSection>
            {
                new() { Heading = "1. Exec Summary", Body = "Investment highlights and growth outlook." },
                new() { Heading = "2. Company Overview", Body = "Basic information and business model." },
                new() { Heading = "3. Market Analysis", Body = "TAM, growth rate, competitive landscape." },
                new() { Heading = "4. Competitive Advantages", Body = "Tech moat, brand, customer stickiness." },
                new() { Heading = "5. Financials", Body = "Historical financial data table placeholder." },
                new() { Heading = "6. Management & Ownership", Body = "Founding team and equity structure." },
                new() { Heading = "7. Risk Factors", Body = "Key operational, financial and compliance risks." },
            }
        };
    }

    public static DocTemplate CreateManagementPresentationTemplate(string companyName = "")
    {
        if (string.IsNullOrWhiteSpace(companyName)) companyName = "公司名称";
        return new DocTemplate
        {
            Title = companyName + " - Management Presentation Outline",
            Sections = new List<DocSection>
            {
                new() { Heading = "A. Company Overview", Body = "Business model, team, milestones." },
                new() { Heading = "B. Market & Competition", Body = "Market landscape, positioning, growth drivers." },
                new() { Heading = "C. Financial Performance", Body = "Revenue drivers, cost structure, profit path." },
                new() { Heading = "D. Growth Strategy", Body = "Organic growth, M&A, new markets." },
                new() { Heading = "E. Funding Use", Body = "Capital allocation plan." },
            }
        };
    }

}
