using Xunit;

namespace ModelForge.Sidecar.Tests;

/// <summary>
/// Tests for cell classification and external link detection logic
/// (pure logic, no COM required).
/// </summary>
public class CellClassifierTests
{
    [Theory]
    [InlineData("=A1+B1", false)]
    [InlineData("=SUM(B1:B10)", false)]
    [InlineData("=IF(A1>0,A1,0)", false)]
    [InlineData("='[Budget.xlsx]Sheet1'!A1", true)]
    [InlineData("=SUM('[Model.xlsx]Sheet1'!B1:B10)", true)]
    [InlineData("=VLOOKUP(A1,'C:\\Data\\[source.xlsx]Sheet1'!$A$1:$C$10,3,FALSE)", true)]
    public void IsExternalLink_DetectsCorrectly(string formula, bool expected)
    {
        bool result = IsExternalLink(formula);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("0", false)]
    [InlineData("hello", false)]
    [InlineData("123", false)]
    public void IsEmptyText_DetectsCorrectly(string value, bool expected)
    {
        bool result = string.IsNullOrWhiteSpace(value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("=A1+B1", CellType.Formula)]
    [InlineData("=SUM(C1:C5)", CellType.Formula)]
    [InlineData("='[Workbook.xlsx]Sheet1'!A1", CellType.ExternalLink)]
    [InlineData("=VLOOKUP(X,'C:\\path\\[file.xlsx]Data'!$A:$Z,2,FALSE)", CellType.ExternalLink)]
    public void ClassifyFormula_CorrectType(string formula, CellType expected)
    {
        var result = ClassifyFormula(formula);
        Assert.Equal(expected, result);
    }

    public enum CellType { Empty, Hardcoded, Formula, ExternalLink }

    private static bool IsExternalLink(string formula)
    {
        return formula.Contains('[') || formula.Contains("'[")
            || (formula.Contains('!') && formula.Contains(":\\"));
    }

    private static CellType ClassifyFormula(string formula)
    {
        if (IsExternalLink(formula))
            return CellType.ExternalLink;
        if (formula.StartsWith('='))
            return CellType.Formula;
        return CellType.Hardcoded;
    }
}
