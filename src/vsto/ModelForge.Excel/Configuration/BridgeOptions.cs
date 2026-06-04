namespace ModelForge.Excel.Configuration
{
    public sealed class BridgeOptions
    {
        public string BackendBaseUrl { get; set; } = "http://localhost:5095";

        public int TimeoutSeconds { get; set; } = 10;
    }
}
