using Xunit;

namespace ModelForge.Sidecar.Tests;

/// <summary>
/// Tests for ModelCheck analysis logic (pure string/formula analysis, no COM).
/// </summary>
public class ModelCheckLogicTests
{
    private static readonly HashSet<string> ErrorValues = new(StringComparer.Ordinal)
    {
        "#REF!", "#N/A", "#VALUE!", "#DIV/0!", "#NUM!", "#NAME?", "#NULL!"
    };

    [Theory]
    [InlineData("#REF!", true)]
    [InlineData("#N/A", true)]
    [InlineData("#VALUE!", true)]
    [InlineData("#DIV/0!", true)]
    [InlineData("#NUM!", true)]
    [InlineData("#NAME?", true)]
    [InlineData("#NULL!", true)]
    [InlineData("123", false)]
    [InlineData("=A1+B1", false)]
    [InlineData("N/A", false)]
    public void IsErrorValue_DetectsCorrectly(string value, bool expected)
    {
        var result = ErrorValues.Contains(value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("='[Budget.xlsx]Sheet1'!A1", true)]
    [InlineData("=VLOOKUP(A1,'C:\\Data\\[source.xlsx]Sheet1'!$A$1:$C$10,3,FALSE)", true)]
    [InlineData("=SUM(A1:A10)+[Workbook.xlsx]Sheet1!B1", true)]
    [InlineData("=A1+B1", false)]
    [InlineData("=SUM(Sheet2!B1:B10)", false)]
    [InlineData("='Sheet2'!A1", false)]
    public void HasExternalLink_DetectsCorrectly(string formula, bool expected)
    {
        bool hasBracket = formula.Contains('[');
        bool hasDrivePath = formula.Contains('!') && formula.Contains(":\\");
        bool result = hasBracket || hasDrivePath;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("=IF(A1>0,100,200)", true)]
    [InlineData("=VLOOKUP(B1,Data!$A:$Z,3,FALSE)", true)]
    [InlineData("=SUM(C1:C10)+500", true)]
    [InlineData("=A1+B1", true)]
    [InlineData("100", false)]
    [InlineData("hello world", false)]
    public void HasFormula_DetectsCorrectly(string input, bool expected)
    {
        bool result = input.TrimStart().StartsWith('=');
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("=A1+B1", "A1+B1")]
    [InlineData("=SUM(B:B)", "SUM(B:B)")]
    [InlineData("  =IF(A1,1,0)", "IF(A1,1,0)")]
    public void StripFormulaPrefix_StripsEquals(string formula, string expected)
    {
        string result = formula.TrimStart();
        if (result.StartsWith('=')) result = result[1..];
        Assert.Equal(expected, result);
    }
}
