using ModelForge.Sidecar.Api;
using ModelForge.Sidecar.Commands;
using ModelForge.Sidecar.Configuration;
using ModelForge.Sidecar.Interop;
using ModelForge.Sidecar.Keyboard;
using ModelForge.Sidecar.Services;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "ModelForge.Sidecar";
});

// Bind configuration
var sidecarOptions = builder.Configuration.GetSection("Sidecar").Get<SidecarOptions>()
                     ?? new SidecarOptions();

builder.Services.AddSingleton(sidecarOptions);
builder.Services.AddSingleton(_ =>
{
    var registry = new ShortcutRegistry();
    registry.RegisterDefaults();
    return registry;
});

// 原生 COM Interop 层 (oleaut32 GetActiveObject + dynamic)
builder.Services.AddSingleton<OfficeApplicationFactory>();
builder.Services.AddSingleton<ExcelInteropService>();
builder.Services.AddSingleton<PowerPointInteropService>();
builder.Services.AddSingleton<WordInteropService>();

// Backend bridge HTTP client
builder.Services.AddHttpClient<BackendBridgeClient>(client =>
{
    client.BaseAddress = new Uri(sidecarOptions.BackendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(sidecarOptions.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-Client-Id", "ModelForge.Sidecar");
    if (!string.IsNullOrWhiteSpace(sidecarOptions.ServiceToken))
    {
        client.DefaultRequestHeaders.Add("X-Service-Token", sidecarOptions.ServiceToken);
    }
});

// Win32 global keyboard hook (conditional)
if (sidecarOptions.KeyboardHookEnabled)
{
    builder.Services.AddSingleton<ChordParser>();
    builder.Services.AddSingleton<KeyboardCommandRouter>();
    builder.Services.AddHostedService<GlobalKeyboardHook>();
}

// CORS for local Web Add-in dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("SidecarLocal", policy =>
    {
        policy
            .WithOrigins("https://localhost:5173", "http://localhost:5173",
                         "https://localhost:3000", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("SidecarLocal");
app.UseSidecarLocalApiToken();

// Map Sidecar localhost REST endpoints (port 5200)
app.MapSidecarEndpoints();

app.Urls.Add($"http://localhost:{sidecarOptions.SidecarPort}");

app.Run();
