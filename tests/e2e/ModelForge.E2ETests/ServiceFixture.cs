using System.Diagnostics;
using Xunit;

namespace ModelForge.E2ETests;

/// <summary>
/// E2E test fixture — manages Backend + Sidecar + Office process lifecycle.
/// Shared across all E2E tests to avoid repeated startup costs.
/// </summary>
public class ServiceFixture : IDisposable
{
    private Process? _backendProcess;
    private Process? _sidecarProcess;
    private Process? _excelProcess;

    public HttpClient BackendClient { get; }
    public HttpClient SidecarClient { get; }
    public bool BackendReady { get; private set; }
    public bool SidecarReady { get; private set; }
    public bool ExcelReady { get; private set; }

    private const string OfficeRoot = @"C:\Program Files (x86)\Microsoft Office\root\Office16";
    private const string SolutionRoot = @"D:\CODES\model-forge";
    private const int ServiceStartupMs = 5000;

    public ServiceFixture()
    {
        BackendClient = new HttpClient { BaseAddress = new Uri("http://localhost:5095"), Timeout = TimeSpan.FromSeconds(10) };
        SidecarClient = new HttpClient { BaseAddress = new Uri("http://localhost:5200"), Timeout = TimeSpan.FromSeconds(10) };

        StartBackend();
        StartSidecar();
        StartExcel();
    }

    private void StartBackend()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project src/backend/ModelForge.Backend/ModelForge.Backend.csproj --configuration Release --no-build",
                WorkingDirectory = SolutionRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            _backendProcess = Process.Start(psi)!;
            Thread.Sleep(ServiceStartupMs);

            // Health check
            var response = BackendClient.GetAsync("/health").Result;
            BackendReady = response.IsSuccessStatusCode;
        }
        catch { BackendReady = false; }
    }

    private void StartSidecar()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project src/sidecar/ModelForge.Sidecar/ModelForge.Sidecar.csproj --configuration Release --no-build",
                WorkingDirectory = SolutionRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            _sidecarProcess = Process.Start(psi)!;
            Thread.Sleep(ServiceStartupMs);

            var response = SidecarClient.GetAsync("/health").Result;
            SidecarReady = response.IsSuccessStatusCode;
        }
        catch { SidecarReady = false; }
    }

    private void StartExcel()
    {
        try
        {
            var excelPath = Path.Combine(OfficeRoot, "EXCEL.EXE");
            if (!File.Exists(excelPath)) return;

            var psi = new ProcessStartInfo
            {
                FileName = excelPath,
                WindowStyle = ProcessWindowStyle.Minimized,
                UseShellExecute = true,
            };
            _excelProcess = Process.Start(psi)!;
            Thread.Sleep(4000);
            ExcelReady = !_excelProcess.HasExited;
        }
        catch { ExcelReady = false; }
    }

    public void Dispose()
    {
        KillProcess(_excelProcess);
        KillProcess(_sidecarProcess);
        KillProcess(_backendProcess);
        BackendClient.Dispose();
        SidecarClient.Dispose();
    }

    private static void KillProcess(Process? proc)
    {
        try { proc?.Kill(); } catch { }
    }
}
