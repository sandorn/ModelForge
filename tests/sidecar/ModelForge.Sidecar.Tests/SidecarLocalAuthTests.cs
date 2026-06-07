using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelForge.Sidecar.Api;
using ModelForge.Sidecar.Configuration;
using Xunit;

namespace ModelForge.Sidecar.Tests;

public class SidecarLocalAuthTests
{
    [Fact]
    public async Task LocalApiToken_AllowsPublicHealth()
    {
        using var app = CreateApp("expected-token");

        var response = await SendAsync(app, "/health");

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
    }

    [Fact]
    public async Task LocalApiToken_RejectsProtectedApiWithoutToken()
    {
        using var app = CreateApp("expected-token");

        var response = await SendAsync(app, "/api/shortcuts/export");

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
        Assert.Contains("Sidecar local API token", response.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LocalApiToken_AllowsProtectedApiWithToken()
    {
        using var app = CreateApp("expected-token");

        var response = await SendAsync(app, "/api/shortcuts/export", "expected-token");

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
    }

    [Fact]
    public async Task LocalApiToken_DisabledWhenEmpty()
    {
        using var app = CreateApp("");

        var response = await SendAsync(app, "/api/shortcuts/export");

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
    }

    private static TestApp CreateApp(string localApiToken)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SidecarOptions { LocalApiToken = localApiToken });
        var provider = services.BuildServiceProvider();
        var builder = new ApplicationBuilder(provider);
        builder.UseSidecarLocalApiToken();
        builder.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("ok");
        });

        return new TestApp(builder.Build(), provider);
    }

    private static async Task<TestResponse> SendAsync(TestApp app, string path, string? localApiToken = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (!string.IsNullOrWhiteSpace(localApiToken))
        {
            context.Request.Headers[SidecarLocalAuth.HeaderName] = localApiToken;
        }

        await app.Delegate(context);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return new TestResponse(context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private sealed record TestResponse(int StatusCode, string Body);

    private sealed class TestApp(RequestDelegate requestDelegate, ServiceProvider services) : IDisposable
    {
        public RequestDelegate Delegate { get; } = requestDelegate;
        public ServiceProvider Services { get; } = services;

        public void Dispose() => Services.Dispose();
    }
}
