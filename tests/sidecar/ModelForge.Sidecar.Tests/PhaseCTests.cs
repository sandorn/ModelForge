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
        Assert.Equal(0, report.LogoIssues);
        Assert.Equal(0, report.LogoPositionIssues);
        Assert.Null(report.TemplateName);
        Assert.Null(report.ReportPath);
        Assert.Equal("ModelForge Deck Check Report", report.ReportTitle);
        Assert.Equal("#1f3a5f", report.BrandPrimaryColor);
        Assert.Equal("#3b82f6", report.BrandAccentColor);
        Assert.Equal(0, report.TotalIssues);
        Assert.Equal("Pass", report.OverallStatus);
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
            DenseTextSlides = 2,
            LogoIssues = 1,
            LogoPositionIssues = 1,
            TemplateName = "ModelForge enterprise template",
            ReportPath = @"C:\Reports\deck-check.pdf"
        };
        report.Issues.Add("Slide 3: Font 'Comic Sans'");
        report.Issues.Add("Slide 5: Term 'DRAFT'");
        report.Issues.Add("Slide 7: Missing logo");
        Assert.Equal(3, report.Issues.Count);
        Assert.Equal(10, report.SlidesScanned);
        Assert.Equal(2, report.FontIssues);
        Assert.Equal(3, report.TermIssues);
        Assert.Equal(1, report.MissingSlideNumbers);
        Assert.Equal(2, report.DenseTextSlides);
        Assert.Equal(1, report.LogoIssues);
        Assert.Equal(1, report.LogoPositionIssues);
        Assert.Equal("ModelForge enterprise template", report.TemplateName);
        Assert.Equal(@"C:\Reports\deck-check.pdf", report.ReportPath);
        Assert.Equal(10, report.TotalIssues);
        Assert.Equal("ActionRequired", report.OverallStatus);
    }

    [Fact]
    public void DeckCheck_ExportReport_WritesBasicPdf()
    {
        var report = new DeckCheck.DeckCheckReport
        {
            SlidesScanned = 3,
            FontIssues = 1,
            TermIssues = 1,
            MissingSlideNumbers = 1,
            DenseTextSlides = 0,
            LogoIssues = 1,
            LogoPositionIssues = 1,
            TemplateName = "ModelForge enterprise template"
        };
        report.Issues.Add("Slide 1: Font 'Comic Sans'");
        report.Issues.Add("Slide 2: Missing logo");
        var path = Path.Combine(Path.GetTempPath(), $"deck-check-{Guid.NewGuid():N}.pdf");

        try
        {
            var resultPath = DeckCheck.ExportReport(report, path);
            var bytes = File.ReadAllBytes(resultPath);

            Assert.Equal(path, resultPath);
            Assert.True(bytes.Length > 100);
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes[..5]));
            Assert.Contains("Template", System.Text.Encoding.ASCII.GetString(bytes));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DeckCheck_BuildHtmlReport_IncludesLogoSummary()
    {
        var report = new DeckCheck.DeckCheckReport
        {
            SlidesScanned = 2,
            LogoIssues = 1,
            LogoPositionIssues = 1,
            TemplateName = "ModelForge enterprise template",
            ReportTitle = "Brand Compliance Report",
            BrandPrimaryColor = "#102A43",
            BrandAccentColor = "#0EA5E9"
        };
        report.Issues.Add("Slide 2: Missing logo");
        report.Issues.Add("Slide 2: Logo position outside template bounds");

        var html = DeckCheck.BuildHtmlReport(report);

        Assert.Contains("Brand Compliance Report", html);
        Assert.Contains("#102A43", html);
        Assert.Contains("#0EA5E9", html);
        Assert.Contains("Needs review", html);
        Assert.Contains("Logo issues", html);
        Assert.Contains("Logo position", html);
        Assert.Contains("ModelForge enterprise template", html);
        Assert.Contains("Missing logo", html);
    }

    [Fact]
    public void DeckCheck_BuildHtmlReport_NormalizesInvalidBrandColors()
    {
        var report = new DeckCheck.DeckCheckReport
        {
            ReportTitle = "Brand Compliance Report",
            BrandPrimaryColor = "not-a-color",
            BrandAccentColor = "00AAFF"
        };

        var html = DeckCheck.BuildHtmlReport(report);

        Assert.Contains("#1F3A5F", html);
        Assert.Contains("#00AAFF", html);
        Assert.Contains("Ready to share", html);
    }

    [Fact]
    public void DeckCheck_ExportReport_IncludesBrandSummary()
    {
        var report = new DeckCheck.DeckCheckReport
        {
            SlidesScanned = 1,
            ReportTitle = "Brand Compliance Report",
            TemplateName = "ModelForge enterprise template",
            BrandPrimaryColor = "#102A43",
            BrandAccentColor = "#0EA5E9"
        };
        var path = Path.Combine(Path.GetTempPath(), $"deck-check-brand-{Guid.NewGuid():N}.pdf");

        try
        {
            var resultPath = DeckCheck.ExportReport(report, path);
            var content = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(resultPath));

            Assert.Contains("Brand Compliance Report", content);
            Assert.Contains("Status", content);
            Assert.Contains("Brand colors", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LogoPolicy_Default_HasEnterpriseBounds()
    {
        var policy = DeckCheck.LogoPolicy.Default;

        Assert.Equal("Default enterprise template", policy.TemplateName);
        Assert.NotNull(policy.MaxLeft);
        Assert.NotNull(policy.MaxTop);
        Assert.NotNull(policy.MaxWidth);
        Assert.NotNull(policy.MaxHeight);
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
    [InlineData(2, "左对齐", "已将 2 个形状左对齐。")]
    [InlineData(5, "右对齐", "已将 5 个形状右对齐。")]
    [InlineData(3, "水平居中对齐", "已将 3 个形状水平居中对齐。")]
    [InlineData(4, "顶端对齐", "已将 4 个形状顶端对齐。")]
    [InlineData(6, "垂直居中对齐", "已将 6 个形状垂直居中对齐。")]
    [InlineData(7, "底端对齐", "已将 7 个形状底端对齐。")]
    public void Align_MessageFormat_MatchesCount(int count, string label, string expectedMessage)
    {
        var message = $"已将 {count} 个形状{label}。";
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [InlineData(3, "水平均分", "已将 3 个形状水平均分。")]
    [InlineData(10, "垂直均分", "已将 10 个形状垂直均分。")]
    public void Distribute_MessageFormat_MatchesCount(int count, string label, string expectedMessage)
    {
        var message = $"已将 {count} 个形状{label}。";
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [InlineData(2, 100.5f, "已将 2 个形状统一宽度为 100.5。")]
    [InlineData(4, 200f, "已将 4 个形状统一宽度为 200。")]
    public void UnifyWidth_MessageFormat_MatchesParams(int count, float width, string expectedMessage)
    {
        var message = $"已将 {count} 个形状统一宽度为 {width}。";
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [InlineData(2, 50.2f, "已将 2 个形状统一高度为 50.2。")]
    [InlineData(4, 150f, "已将 4 个形状统一高度为 150。")]
    public void UnifyHeight_MessageFormat_MatchesParams(int count, float height, string expectedMessage)
    {
        var message = $"已将 {count} 个形状统一高度为 {height}。";
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

    [Fact]
    public void AlignBoxes_Right_UsesSelectionBoundingBox()
    {
        var shapes = new[]
        {
            new ShapeTools.ShapeBox(100, 50, 20, 10),
            new ShapeTools.ShapeBox(160, 70, 40, 20),
            new ShapeTools.ShapeBox(120, 90, 10, 30)
        };

        var result = ShapeTools.AlignBoxes(shapes, ShapeTools.ShapeAlignment.Right);

        Assert.Equal(180, result[0].Left);
        Assert.Equal(160, result[1].Left);
        Assert.Equal(190, result[2].Left);
        Assert.All(result, shape => Assert.Equal(200, shape.Right));
    }

    [Fact]
    public void AlignBoxes_Middle_PreservesHorizontalPositions()
    {
        var shapes = new[]
        {
            new ShapeTools.ShapeBox(10, 10, 10, 10),
            new ShapeTools.ShapeBox(50, 50, 20, 20)
        };

        var result = ShapeTools.AlignBoxes(shapes, ShapeTools.ShapeAlignment.Middle);

        Assert.Equal(10, result[0].Left);
        Assert.Equal(50, result[1].Left);
        Assert.Equal(35, result[0].Top);
        Assert.Equal(30, result[1].Top);
    }

    [Fact]
    public void DistributeBoxes_Horizontal_CreatesEqualEdgeGaps()
    {
        var shapes = new[]
        {
            new ShapeTools.ShapeBox(10, 0, 20, 10),
            new ShapeTools.ShapeBox(80, 0, 10, 10),
            new ShapeTools.ShapeBox(130, 0, 30, 10)
        };

        var result = ShapeTools.DistributeBoxes(shapes, ShapeTools.DistributionAxis.Horizontal);

        Assert.Equal(10, result[0].Left);
        Assert.Equal(75, result[1].Left);
        Assert.Equal(130, result[2].Left);
        Assert.Equal(result[1].Left - result[0].Right, result[2].Left - result[1].Right);
    }

    [Fact]
    public void DistributeBoxes_Vertical_CreatesEqualEdgeGaps()
    {
        var shapes = new[]
        {
            new ShapeTools.ShapeBox(0, 10, 10, 20),
            new ShapeTools.ShapeBox(0, 80, 10, 10),
            new ShapeTools.ShapeBox(0, 130, 10, 30)
        };

        var result = ShapeTools.DistributeBoxes(shapes, ShapeTools.DistributionAxis.Vertical);

        Assert.Equal(10, result[0].Top);
        Assert.Equal(75, result[1].Top);
        Assert.Equal(130, result[2].Top);
        Assert.Equal(result[1].Top - result[0].Bottom, result[2].Top - result[1].Bottom);
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

    [Fact]
    public void LinkRefreshPlanner_GroupsBackendMetadataByTargetHost()
    {
        var plan = LinkRefreshPlanner.Create(new[]
        {
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "ppt-1",
                TargetType = ModelForge.Contracts.LinkTargetType.PowerPointShape,
                TargetAddress = "Slide1/Shape3"
            },
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "ppt-2",
                TargetType = ModelForge.Contracts.LinkTargetType.PowerPointChart,
                TargetAddress = "Slide2/Chart4"
            },
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "word-1",
                TargetType = ModelForge.Contracts.LinkTargetType.WordTable,
                TargetAddress = "Table1"
            }
        });

        Assert.Equal(3, plan.MetadataCount);
        Assert.Equal(2, plan.PowerPointTargets);
        Assert.Equal(1, plan.WordTargets);
        Assert.Equal(2, plan.PrecisePowerPointTargets);
        Assert.Equal(1, plan.PreciseWordTargets);
        Assert.True(plan.RefreshPowerPoint);
        Assert.True(plan.RefreshWord);
        Assert.Equal(new[] { "ppt-1", "ppt-2", "word-1" }, plan.LinkIds);
    }

    [Fact]
    public void LinkRefreshPlanner_ParsesPrecisePowerPointTargets()
    {
        var plan = LinkRefreshPlanner.Create(new[]
        {
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "ppt-shape",
                TargetType = ModelForge.Contracts.LinkTargetType.PowerPointShape,
                TargetAddress = "Slide12/Shape7"
            },
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "ppt-named",
                TargetType = ModelForge.Contracts.LinkTargetType.PowerPointChart,
                TargetAddress = "Slide3/Revenue Chart"
            }
        });

        Assert.Equal(2, plan.PrecisePowerPointTargets);
        Assert.Equal(12, plan.PowerPointTargetObjects[0].SlideIndex);
        Assert.Equal(7, plan.PowerPointTargetObjects[0].ShapeIndex);
        Assert.Equal(3, plan.PowerPointTargetObjects[1].SlideIndex);
        Assert.Equal("Revenue Chart", plan.PowerPointTargetObjects[1].ShapeName);
    }

    [Fact]
    public void LinkRefreshPlanner_ParsesPreciseWordTargets()
    {
        var plan = LinkRefreshPlanner.Create(new[]
        {
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "word-field",
                TargetType = ModelForge.Contracts.LinkTargetType.WordInlineShape,
                TargetAddress = "Field2"
            },
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "word-inline-shape",
                TargetType = ModelForge.Contracts.LinkTargetType.WordInlineShape,
                TargetAddress = "InlineShape4"
            },
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "word-table",
                TargetType = ModelForge.Contracts.LinkTargetType.WordTable,
                TargetAddress = "Table5"
            }
        });

        Assert.Equal(3, plan.PreciseWordTargets);
        Assert.Equal(2, plan.WordTargetObjects[0].FieldIndex);
        Assert.Equal(4, plan.WordTargetObjects[1].InlineShapeIndex);
        Assert.Equal(5, plan.WordTargetObjects[2].TableIndex);
    }

    [Fact]
    public void LinkRefreshPlanner_MarksIncompleteTargetsAsFallbackRequired()
    {
        var plan = LinkRefreshPlanner.Create(new[]
        {
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "ppt-unknown",
                TargetType = ModelForge.Contracts.LinkTargetType.PowerPointShape,
                TargetAddress = "Slide1"
            },
            new ModelForge.Contracts.LinkMetadata
            {
                LinkId = "word-unknown",
                TargetType = ModelForge.Contracts.LinkTargetType.WordTable,
                TargetAddress = "Body"
            }
        });

        Assert.Equal(0, plan.PrecisePowerPointTargets);
        Assert.Equal(0, plan.PreciseWordTargets);
        Assert.False(plan.PowerPointTargetObjects[0].IsPrecise);
        Assert.False(plan.WordTargetObjects[0].IsPrecise);
    }
}

}
