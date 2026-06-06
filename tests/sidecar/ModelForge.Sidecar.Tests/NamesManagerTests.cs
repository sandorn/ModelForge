using ModelForge.Sidecar.PowerTools;
using Xunit;

namespace ModelForge.Sidecar.Tests;

/// <summary>
/// Tests for NamesManager business logic (pure logic, no COM required).
/// </summary>
public class NamesManagerTests
{
    [Fact]
    public void NamesReport_InitialState()
    {
        var report = new NamesManager.NamesReport();
        Assert.Equal(0, report.TotalCount);
        Assert.Equal(0, report.InvalidCount);
        Assert.Equal(0, report.DeletedCount);
        Assert.Empty(report.AllNames);
        Assert.Empty(report.InvalidNames);
        Assert.Empty(report.DeleteErrors);
    }

    [Fact]
    public void NameInfo_Construction()
    {
        var info = new NamesManager.NameInfo
        {
            Name = "TestRange",
            RefersTo = "=Sheet1!$A$1:$B$10",
            IsVisible = true,
            IsValid = true
        };

        Assert.Equal("TestRange", info.Name);
        Assert.Equal("=Sheet1!$A$1:$B$10", info.RefersTo);
        Assert.True(info.IsVisible);
        Assert.True(info.IsValid);
        Assert.Null(info.Error);
    }

    [Fact]
    public void NameInfo_InvalidWithError()
    {
        var info = new NamesManager.NameInfo
        {
            Name = "#REF!Broken",
            RefersTo = "=#REF!$A$1",
            IsVisible = false,
            IsValid = false,
            Error = "Reference cannot be resolved."
        };

        Assert.False(info.IsValid);
        Assert.Equal("Reference cannot be resolved.", info.Error);
        Assert.False(info.IsVisible);
    }

    [Fact]
    public void NamesReport_AggregatesCorrectly()
    {
        var report = new NamesManager.NamesReport();
        report.AllNames.Add(new NamesManager.NameInfo { Name = "Valid1", IsValid = true });
        report.AllNames.Add(new NamesManager.NameInfo { Name = "Valid2", IsValid = true });
        report.AllNames.Add(new NamesManager.NameInfo { Name = "Broken1", IsValid = false, Error = "Bad ref" });
        report.AllNames.Add(new NamesManager.NameInfo { Name = "Broken2", IsValid = false, Error = "Bad ref" });

        report.InvalidNames.AddRange(report.AllNames.Where(n => !n.IsValid));

        Assert.Equal(4, report.TotalCount);
        Assert.Equal(2, report.InvalidCount);
        Assert.All(report.InvalidNames, n => Assert.False(n.IsValid));
    }
}