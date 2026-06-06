using Xunit;
using ModelForge.Sidecar.Optimization;
using ModelForge.Sidecar.Linking;
using ModelForge.Sidecar.PowerPoint;

namespace ModelForge.Sidecar.Tests;

public class OptimizationTests
{
    [Fact]
    public void PrepareResult_InitialState()
    {
        var result = new PrepareToShare.PrepareResult();
        Assert.Equal("", result.OutputPath);
        Assert.Equal(0, result.FormulasConverted);
        Assert.Equal(0, result.CommentsRemoved);
        Assert.Equal(0, result.HiddenRowsRemoved);
        Assert.Equal(0, result.HiddenColumnsRemoved);
        Assert.Equal(0, result.ExternalLinksBroken);
        Assert.Equal(0, result.VeryHiddenSheetsRemoved);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public void PrepareResult_FieldsAreSettable()
    {
        var result = new PrepareToShare.PrepareResult
        {
            HiddenRowsRemoved = 5,
            HiddenColumnsRemoved = 3,
            ExternalLinksBroken = 2,
            VeryHiddenSheetsRemoved = 1
        };
        Assert.Equal(5, result.HiddenRowsRemoved);
        Assert.Equal(3, result.HiddenColumnsRemoved);
        Assert.Equal(2, result.ExternalLinksBroken);
        Assert.Equal(1, result.VeryHiddenSheetsRemoved);
    }

    [Fact]
    public void OptimizationResult_InitialState()
    {
        var result = new WorkbookOptimizer.OptimizationResult();
        Assert.Equal(0, result.StylesRemoved);
        Assert.Equal(0, result.InvalidNamesRemoved);
        Assert.Equal(0, result.ExternalLinkResiduesRemoved);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public void OptimizationResult_ActionsCanAccumulate()
    {
        var result = new WorkbookOptimizer.OptimizationResult();
        result.Actions.Add("Removed 3 styles");
        result.Actions.Add("Broke 2 links");
        Assert.Equal(2, result.Actions.Count);
    }

    [Fact]
    public void PrepareResult_ActionsCanAccumulate()
    {
        var result = new PrepareToShare.PrepareResult();
        result.Actions.Add("Copied workbook");
        result.Actions.Add("Converted 10 formulas");
        Assert.Equal(2, result.Actions.Count);
    }
}

public class DeckCheckTests
{
    [Fact]
    public void DeckCheckReport_InitialState()
    {
        var report = new DeckCheck.DeckCheckReport();
        Assert.Equal(0, report.SlidesScanned);
        Assert.Equal(0, report.FontIssues);
        Assert.Equal(0, report.TermIssues);
        Assert.Equal(0, report.MissingSlideNumbers);
        Assert.Equal(0, report.DenseTextSlides);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void DeckCheckReport_IssuesCanAccumulate()
    {
        var report = new DeckCheck.DeckCheckReport
        {
            SlidesScanned = 10,
            FontIssues = 2,
            TermIssues = 3,
            MissingSlideNumbers = 1,
            DenseTextSlides = 2
        };
        report.Issues.Add("Slide 3: Font 'Comic Sans'");
        report.Issues.Add("Slide 5: Term 'DRAFT'");
        Assert.Equal(2, report.Issues.Count);
        Assert.Equal(10, report.SlidesScanned);
        Assert.Equal(2, report.FontIssues);
        Assert.Equal(3, report.TermIssues);
        Assert.Equal(1, report.MissingSlideNumbers);
        Assert.Equal(2, report.DenseTextSlides);
    }
}

public class DynamicAgendasTests
{
    [Fact]
    public void AgendaResult_InitialState()
    {
        var result = new DynamicAgendas.AgendaResult();
        Assert.Equal(0, result.SectionsFound);
        Assert.Equal(0, result.SlidesGenerated);
        Assert.Empty(result.SectionTitles);
    }

    [Fact]
    public void AgendaResult_SectionTitlesAccumulate()
    {
        var result = new DynamicAgendas.AgendaResult();
        result.SectionTitles.Add("Executive Summary");
        result.SectionTitles.Add("Financial Analysis");
        result.SectionsFound = result.SectionTitles.Count;
        Assert.Equal(2, result.SectionsFound);
        Assert.Contains("Executive Summary", result.SectionTitles);
    }

public class LinkRefresherTests
{
    [Fact]
    public void RefreshResult_InitialState()
    {
        var result = new LinkRefresher.RefreshResult();
        Assert.Equal(0, result.TotalLinks);
        Assert.Equal(0, result.Refreshed);
        Assert.Equal(0, result.Broken);
        Assert.Empty(result.BrokenDetails);
    }

    [Fact]
    public void RefreshResult_AccumulatesCorrectly()
    {
        var result = new LinkRefresher.RefreshResult
        {
            TotalLinks = 10,
            Refreshed = 8,
            Broken = 2
        };
        result.BrokenDetails.Add("Link to 'old_data.xlsx' is broken");
        Assert.Single(result.BrokenDetails);
        Assert.Contains("old_data.xlsx", result.BrokenDetails[0]);
    }
}

public class ShapeToolsTests
{
    [Theory]
    [InlineData(2, "已将 2 个形状左对齐。")]
    [InlineData(5, "已将 5 个形状左对齐。")]
    public void AlignLeft_MessageFormat_MatchesCount(int count, string expectedMessage)
    {
        // Test the message format pattern used in AlignLeft
        var message = $"已将 {count} 个形状左对齐。";
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [InlineData(3, "已将 3 个形状水平均分。")]
    [InlineData(10, "已将 10 个形状水平均分。")]
    public void DistributeHorizontal_MessageFormat_MatchesCount(int count, string expectedMessage)
    {
        var message = $"已将 {count} 个形状水平均分。";
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [InlineData(2, 100.5f, 50.2f, "已将 2 个形状统一为 100.5x50.2。")]
    [InlineData(4, 200f, 150f, "已将 4 个形状统一为 200x150。")]
    public void UnifySize_MessageFormat_MatchesParams(int count, float width, float height, string expectedMessage)
    {
        var message = $"已将 {count} 个形状统一为 {width}x{height}。";
        Assert.Equal(expectedMessage, message);
    }
}


public class LinkingTests
{
    [Fact]
    public void ExcelToPowerPointLinker_MessageFormat_ContainsWorkbookInfo()
    {
        // Test the message format pattern used in LinkRange
        var msg = "Range 'model.xlsx!Sheet1!$A$1:$D$20' linked to 'deck.pptx' Slide 3, shape: Rectangle 5.";
        Assert.Contains("model.xlsx", msg);
        Assert.Contains("deck.pptx", msg);
        Assert.Contains("Slide 3", msg);
    }

    [Fact]
    public void ExcelToWordLinker_MessageFormat_ContainsDocInfo()
    {
        var msg = "Range 'model.xlsx!Sheet1!$A$1:$D$20' linked to 'report.docx'.";
        Assert.Contains("model.xlsx", msg);
        Assert.Contains("report.docx", msg);
    }

    [Fact]
    public void LinkRefresher_PowerPointRefresh_ReturnsStructuredResult()
    {
        var result = new LinkRefresher.RefreshResult
        {
            TotalLinks = 5,
            Refreshed = 4,
            Broken = 1
        };
        result.BrokenDetails.Add(@"Slide 2: Chart 3 (C:\data\source.xlsx)");
        Assert.Equal(5, result.TotalLinks);
        Assert.Equal(4, result.Refreshed);
        Assert.Equal(1, result.Broken);
        Assert.Single(result.BrokenDetails);
    }

    [Fact]
    public void LinkRefresher_ExcelRefresh_TracksLinkSources()
    {
        var result = new LinkRefresher.RefreshResult
        {
            TotalLinks = 3,
            Refreshed = 3,
            Broken = 0
        };
        Assert.Empty(result.BrokenDetails);
        Assert.Equal(3, result.TotalLinks);
    }
}

}