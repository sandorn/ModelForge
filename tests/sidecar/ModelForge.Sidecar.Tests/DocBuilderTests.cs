using Xunit;
using ModelForge.Sidecar.Word;

namespace ModelForge.Sidecar.Tests;

public class DocBuilderTests
{
    [Fact]
    public void CreateDueDiligenceTemplate_HasCorrectStructure()
    {
        var template = DocBuilder.CreateDueDiligenceTemplate("TestCo");
        Assert.Contains("TestCo", template.Title);
        Assert.Equal(5, template.Sections.Count);
        Assert.NotNull(template.Sections[4].Table);
    }

    [Fact]
    public void CreateDueDiligenceTemplate_DefaultName()
    {
        var template = DocBuilder.CreateDueDiligenceTemplate();
        Assert.Contains("目标公司", template.Title);
    }

    [Fact]
    public void CreateCimTemplate_HasCorrectStructure()
    {
        var template = DocBuilder.CreateCimTemplate("AcmeCorp");
        Assert.Contains("AcmeCorp", template.Title);
        Assert.Equal(7, template.Sections.Count);
    }

    [Fact]
    public void CreateCimTemplate_DefaultName()
    {
        var template = DocBuilder.CreateCimTemplate();
        Assert.Contains("目标公司", template.Title);
    }

    [Fact]
    public void CreateCimTemplate_EmptyStringFallback()
    {
        var template = DocBuilder.CreateCimTemplate("");
        Assert.Contains("目标公司", template.Title);
    }

    [Fact]
    public void CreateMgmtPresentation_HasCorrectStructure()
    {
        var template = DocBuilder.CreateManagementPresentationTemplate("FooInc");
        Assert.Contains("FooInc", template.Title);
        Assert.Equal(5, template.Sections.Count);
    }

    [Fact]
    public void CreateMgmtPresentation_DefaultName()
    {
        var template = DocBuilder.CreateManagementPresentationTemplate();
        Assert.Contains("公司名称", template.Title);
    }

    [Fact]
    public void DocSection_NullTableAllowed()
    {
        var section = new DocBuilder.DocSection { Heading = "Test", Body = "Content" };
        Assert.Null(section.Table);
    }

    [Fact]
    public void DocTemplate_EmptySectionsAllowed()
    {
        var template = new DocBuilder.DocTemplate { Title = "Empty" };
        Assert.Empty(template.Sections);
    }
}
