using ModelForge.Sidecar.Interop;
using Xunit;

namespace ModelForge.Sidecar.Tests;

public class OfficeRuntimeValidatorTests
{
    [Fact]
    public void Validate_RejectsWpsKingsoftPath()
    {
        var result = OfficeComValidator.Validate(
            "Excel",
            "Microsoft Excel",
            "12.0",
            @"C:\Users\Administrator\AppData\Local\Kingsoft\WPS Office\12.1.0.26895\office6");

        Assert.False(result.IsSupported);
        Assert.Contains("WPS", result.Error);
    }

    [Fact]
    public void Validate_RejectsLegacyOfficeMajorVersion()
    {
        var result = OfficeComValidator.Validate(
            "Excel",
            "Microsoft Excel",
            "12.0",
            @"C:\Program Files\Microsoft Office\Office12");

        Assert.False(result.IsSupported);
        Assert.Contains("低于受支持", result.Error);
    }

    [Fact]
    public void Validate_AcceptsOffice16Path()
    {
        var result = OfficeComValidator.Validate(
            "Excel",
            "Microsoft Excel",
            "16.0",
            @"C:\Program Files (x86)\Microsoft Office\Root\Office16");

        Assert.True(result.IsSupported);
        Assert.Equal("Microsoft Excel", result.Name);
        Assert.Equal("16.0", result.Version);
    }
}
