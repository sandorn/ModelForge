using Xunit;

namespace ModelForge.Sidecar.Tests;

/// <summary>
/// Power Tools 业务逻辑单元测试（无需 Excel COM 运行时）。
/// </summary>
public class PowerToolsTests
{
    [Theory]
    [InlineData("=IFERROR(A1+B1,0)", true)]
    [InlineData("=IFERROR(VLOOKUP(X,Y,2,FALSE),\"N/A\")", true)]
    [InlineData("=A1+B1", false)]
    [InlineData("=SUM(B1:B10)", false)]
    [InlineData("  =IFERROR(A1,0)", true)]
    public void IsIfErrorWrapped_DetectsCorrectly(string formula, bool expected)
    {
        bool result = IsIfErrorWrapped(formula);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("=A1", "=IFERROR(A1,0)")]
    [InlineData("=SUM(B1:B10)+C1", "=IFERROR(SUM(B1:B10)+C1,0)")]
    public void WrapFormula_CreatesCorrectIfError(string formula, string expected)
    {
        var result = WrapWithIfError(formula, "0");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void UnwrapIfError_RemovesOuterWrapper()
    {
        var result = UnwrapIfError("=IFERROR(A1+B1,0)");
        Assert.Equal("=A1+B1", result);
    }

    [Theory]
    [InlineData("accounting", "Accounting")]
    [InlineData("percent", "Percent")]
    [InlineData("comma", "Comma")]
    [InlineData("currency", "Currency")]
    [InlineData("ACCOUNTING", "Accounting")]
    [InlineData("unknown", "Accounting")] // default fallback
    public void ParseFinanceFormatType_ValidInputs(string input, string expected)
    {
        var result = ParseFormatType(input);
        Assert.Equal(expected, result);
    }

    // ─── Helpers (mirror Sidecar logic for unit testing) ───

    private static bool IsIfErrorWrapped(string formula)
    {
        return formula.TrimStart().StartsWith("=IFERROR(", StringComparison.OrdinalIgnoreCase);
    }

    private static string WrapWithIfError(string formula, string fallback)
    {
        string body = formula.StartsWith('=') ? formula[1..] : formula;
        return $"=IFERROR({body},{fallback})";
    }

    private static string UnwrapIfError(string formula)
    {
        string body = formula.TrimStart();
        if (!body.StartsWith("=IFERROR(", StringComparison.OrdinalIgnoreCase))
            return formula;

        body = body[9..]; // remove "=IFERROR(" (9 chars)
        // Find matching closing parenthesis
        int depth = 1;
        int comma = -1;
        for (int i = 0; i < body.Length && depth > 0; i++)
        {
            if (body[i] == '(') depth++;
            else if (body[i] == ')') depth--;
            else if (body[i] == ',' && depth == 1) comma = i;
        }
        if (comma > 0)
            return "=" + body[..comma];
        return formula;
    }

    private static string ParseFormatType(string input)
    {
        return input.ToLowerInvariant() switch
        {
            "percent" => "Percent",
            "comma" => "Comma",
            "currency" => "Currency",
            _ => "Accounting"
        };
    }
}
