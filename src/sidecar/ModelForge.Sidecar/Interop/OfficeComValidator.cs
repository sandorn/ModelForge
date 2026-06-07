namespace ModelForge.Sidecar.Interop;

public static class OfficeComValidator
{
    public static OfficeComValidationResult Validate(string appName, string? name, string? version, string? path)
    {
        if (ContainsUnsupportedOfficePath(path))
        {
            return OfficeComValidationResult.Unsupported(
                $"{appName} COM 当前指向 WPS/Kingsoft Office，不是 Microsoft Office。请关闭 WPS，并启动 Microsoft Office 2024 的 {appName}。");
        }

        var majorVersion = ParseMajorVersion(version);
        if (majorVersion is > 0 and < 16)
        {
            return OfficeComValidationResult.Unsupported(
                $"{appName} COM 当前版本为 {version}，低于受支持的 Office 2016+/Office 2024。请启动 Microsoft Office 桌面版后重试。");
        }

        if (!string.IsNullOrWhiteSpace(path) && !LooksLikeMicrosoftOfficePath(path))
        {
            return OfficeComValidationResult.Unsupported(
                $"{appName} COM 当前路径不是 Microsoft Office 安装目录: {path}");
        }

        return OfficeComValidationResult.Supported(name, version, path);
    }

    private static int ParseMajorVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return 0;
        }

        var firstPart = version.Split('.')[0];
        return int.TryParse(firstPart, out var majorVersion) ? majorVersion : 0;
    }

    private static bool ContainsUnsupportedOfficePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.Contains("Kingsoft", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("WPS Office", StringComparison.OrdinalIgnoreCase) ||
               path.Contains(@"\office6", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeMicrosoftOfficePath(string path)
    {
        return path.Contains("Microsoft Office", StringComparison.OrdinalIgnoreCase) ||
               path.Contains(@"Office\root\Office16", StringComparison.OrdinalIgnoreCase) ||
               path.Contains(@"\Office16", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record OfficeComValidationResult(
    bool IsSupported,
    string? Error,
    string? Name,
    string? Version,
    string? Path)
{
    public static OfficeComValidationResult Supported(string? name, string? version, string? path) =>
        new(true, null, name, version, path);

    public static OfficeComValidationResult Unsupported(string error) =>
        new(false, error, null, null, null);
}
