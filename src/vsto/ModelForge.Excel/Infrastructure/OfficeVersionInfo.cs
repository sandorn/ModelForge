namespace ModelForge.Excel.Infrastructure
{
    public sealed class OfficeVersionInfo
    {
        public OfficeVersionInfo(string applicationName, string version, bool is64BitProcess)
        {
            ApplicationName = applicationName;
            Version = version;
            Is64BitProcess = is64BitProcess;
        }

        public string ApplicationName { get; }

        public string Version { get; }

        public bool Is64BitProcess { get; }

        public static OfficeVersionInfo FromApplication(dynamic application)
        {
            string name = application?.Name ?? "Excel";
            string version = application?.Version ?? "Unknown";
            return new OfficeVersionInfo(name, version, System.Environment.Is64BitProcess);
        }
    }
}
