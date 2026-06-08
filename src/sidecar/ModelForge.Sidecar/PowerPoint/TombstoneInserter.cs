namespace ModelForge.Sidecar.PowerPoint;

/// <summary>
/// Tombstone 插入器 — 交易公告墓碑模板（投行/PE 常用）。
/// </summary>
public static class TombstoneInserter
{
    public static string InsertTombstone(dynamic pptApp, string? companyName = null)
    {
        companyName = string.IsNullOrWhiteSpace(companyName) ? "Company Name" : companyName;
        dynamic slide = pptApp.ActiveWindow.View.Slide;
        float slideWidth = pptApp.ActivePresentation.PageSetup.SlideWidth;
        float centerX = slideWidth / 2f;
        float top = 60f;

        // ── 标题 ──
        dynamic title = slide.Shapes.AddTextbox(1, centerX - 200, top, 400, 36);
        title.TextFrame.TextRange.Text = companyName;
        title.TextFrame.TextRange.Font.Size = 22;
        title.TextFrame.TextRange.Font.Bold = true;
        title.TextFrame.TextRange.Font.Color.RGB = 0x1F3A5F;
        title.TextFrame.TextRange.ParagraphFormat.Alignment = 2; // ppAlignCenter
        top += 50;

        // ── 分割线 ──
        dynamic line = slide.Shapes.AddLine(centerX - 180, top, centerX + 180, top);
        line.Line.ForeColor.RGB = 0xC0C0C0;
        line.Line.Weight = 1.5f;
        top += 20;

        // ── 交易描述 ──
        dynamic desc = slide.Shapes.AddTextbox(1, centerX - 200, top, 400, 24);
        desc.TextFrame.TextRange.Text = "has been acquired by";
        desc.TextFrame.TextRange.Font.Size = 14;
        desc.TextFrame.TextRange.Font.Italic = true;
        desc.TextFrame.TextRange.Font.Color.RGB = 0x666666;
        desc.TextFrame.TextRange.ParagraphFormat.Alignment = 2;
        top += 40;

        // ── 买方 ──
        dynamic buyer = slide.Shapes.AddTextbox(1, centerX - 200, top, 400, 28);
        buyer.TextFrame.TextRange.Text = "[Acquirer Name]";
        buyer.TextFrame.TextRange.Font.Size = 18;
        buyer.TextFrame.TextRange.Font.Bold = true;
        buyer.TextFrame.TextRange.Font.Color.RGB = 0x333333;
        buyer.TextFrame.TextRange.ParagraphFormat.Alignment = 2;
        top += 50;

        // ── 分割线 ──
        dynamic line2 = slide.Shapes.AddLine(centerX - 120, top, centerX + 120, top);
        line2.Line.ForeColor.RGB = 0xC0C0C0;
        line2.Line.Weight = 1f;
        top += 20;

        // ── 财务顾问 ──
        dynamic advisorLabel = slide.Shapes.AddTextbox(1, centerX - 200, top, 400, 20);
        advisorLabel.TextFrame.TextRange.Text = "Financial Advisor to " + companyName;
        advisorLabel.TextFrame.TextRange.Font.Size = 10;
        advisorLabel.TextFrame.TextRange.Font.Color.RGB = 0x888888;
        advisorLabel.TextFrame.TextRange.ParagraphFormat.Alignment = 2;
        top += 25;

        dynamic advisor = slide.Shapes.AddTextbox(1, centerX - 200, top, 400, 24);
        advisor.TextFrame.TextRange.Text = "[Advisor Name]";
        advisor.TextFrame.TextRange.Font.Size = 13;
        advisor.TextFrame.TextRange.Font.Bold = true;
        advisor.TextFrame.TextRange.Font.Color.RGB = 0x1F3A5F;
        advisor.TextFrame.TextRange.ParagraphFormat.Alignment = 2;
        top += 50;

        // ── 日期 ──
        dynamic date = slide.Shapes.AddTextbox(1, centerX - 200, top, 400, 18);
        date.TextFrame.TextRange.Text = DateTime.Now.ToString("yyyy 年 M 月");
        date.TextFrame.TextRange.Font.Size = 10;
        date.TextFrame.TextRange.Font.Color.RGB = 0x999999;
        date.TextFrame.TextRange.ParagraphFormat.Alignment = 2;

        return $"已插入 Tombstone 模板（{companyName}）。请替换占位文本为实际交易信息。";
    }
}
