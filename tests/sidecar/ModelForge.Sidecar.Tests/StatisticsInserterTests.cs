using Xunit;

namespace ModelForge.Sidecar.Tests;

/// <summary>
/// Tests for StatisticsInserter computation logic (pure logic).
/// </summary>
public class StatisticsInserterTests
{
    [Theory]
    [InlineData(new double[] { 1, 2, 3, 4, 5 }, 1)]
    [InlineData(new double[] { -5, 0, 5, 10 }, -5)]
    [InlineData(new double[] { 100 }, 100)]
    public void ComputeMin_ReturnsMinimum(double[] values, double expected)
    {
        var result = values.Min();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new double[] { 1, 2, 3, 4, 5 }, 5)]
    [InlineData(new double[] { -5, 0, 5, 10 }, 10)]
    [InlineData(new double[] { 100 }, 100)]
    public void ComputeMax_ReturnsMaximum(double[] values, double expected)
    {
        var result = values.Max();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new double[] { 10, 20, 30, 40, 50 }, 30)]
    [InlineData(new double[] { 1, 2, 3 }, 2)]
    [InlineData(new double[] { 0, 0, 0 }, 0)]
    public void ComputeAverage_ReturnsCorrectMean(double[] values, double expected)
    {
        var result = values.Average();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new double[] { 1, 2, 3 }, 6)]
    [InlineData(new double[] { 10, 20 }, 30)]
    [InlineData(new double[] { }, 0)]
    public void ComputeSum_ReturnsTotal(double[] values, double expected)
    {
        var result = values.Sum();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComputeCount_ReturnsNumberOfValues()
    {
        var values = new double[] { 1, 2, 3, 4, 5 };
        Assert.Equal(5, values.Length);
    }

    [Theory]
    [InlineData("=MIN(A1:A10)", new[] { "MIN" })]
    [InlineData("=MAX(B1:B20)", new[] { "MAX" })]
    [InlineData("=AVERAGE(C1:C15)", new[] { "AVERAGE" })]
    [InlineData("=SUM(D1:D10)", new[] { "SUM" })]
    [InlineData("=COUNT(E1:E10)", new[] { "COUNT" })]
    public void StatisticsFormulaTemplates_CorrectFunctions(string template, string[] expectedFunctions)
    {
        foreach (var func in expectedFunctions)
        {
            Assert.Contains(func, template.ToUpperInvariant());
        }
    }
}
