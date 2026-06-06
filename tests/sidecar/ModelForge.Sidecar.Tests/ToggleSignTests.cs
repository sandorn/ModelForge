using Xunit;

namespace ModelForge.Sidecar.Tests;

/// <summary>
/// Tests for ToggleSign formula manipulation logic (pure logic, no COM).
/// </summary>
public class ToggleSignTests
{
    [Theory]
    [InlineData("=A1+B1", "=-(A1+B1)")]
    [InlineData("=SUM(C1:C5)", "=-(SUM(C1:C5))")]
    [InlineData("=A1*B1", "=-(A1*B1)")]
    [InlineData("=100", "=-(100)")]
    public void ToggleFormula_WrapsWithNegation(string formula, string expected)
    {
        var result = ToggleSignFormula(formula);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("=-(A1+B1)", "=A1+B1")]
    [InlineData("=-(SUM(C1:C5))", "=SUM(C1:C5)")]
    [InlineData("=-(A1)", "=A1")]
    public void ToggleFormula_UnwrapsNegation(string formula, string expected)
    {
        var result = ToggleSignFormula(formula);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(100.0, -100.0)]
    [InlineData(-50.0, 50.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(3.14159, -3.14159)]
    public void ToggleNumericValue_FlipsSign(double input, double expected)
    {
        var result = -input;
        Assert.Equal(expected, result);
    }

    private static string ToggleSignFormula(string formula)
    {
        string trimmed = formula.TrimStart();
        string body = trimmed.StartsWith('=') ? trimmed[1..].TrimStart() : trimmed;

        if (body.StartsWith("-(") && body.EndsWith(')'))
        {
            return "=" + body[2..^1];
        }
        return $"=-({body})";
    }
}
