using Microsoft.Extensions.Primitives;
using ModelForge.Contracts;
using ModelForge.Sidecar.Configuration;

namespace ModelForge.Sidecar.Api;

public static class SidecarLocalAuth
{
    public const string HeaderName = "X-ModelForge-Sidecar-Token";

    public static IApplicationBuilder UseSidecarLocalApiToken(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var options = context.RequestServices.GetRequiredService<SidecarOptions>();
            if (!RequiresToken(context, options))
            {
                await next(context);
                return;
            }

            if (HasValidToken(context.Request.Headers[HeaderName], options.LocalApiToken))
            {
                await next(context);
                return;
            }

            var traceId = GetTraceId(context);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Trace-Id"] = traceId;
            await context.Response.WriteAsJsonAsync(
                ApiEnvelope<object>.Failure("Valid Sidecar local API token is required.", traceId));
        });
    }

    private static bool RequiresToken(HttpContext context, SidecarOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LocalApiToken))
        {
            return false;
        }

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return false;
        }

        return context.Request.Path.StartsWithSegments("/api");
    }

    private static bool HasValidToken(StringValues providedToken, string configuredToken)
    {
        if (StringValues.IsNullOrEmpty(providedToken))
        {
            return false;
        }

        return string.Equals(providedToken.FirstOrDefault(), configuredToken, StringComparison.Ordinal);
    }

    private static string GetTraceId(HttpContext context)
    {
        var headerTraceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(headerTraceId)
            ? context.TraceIdentifier
            : headerTraceId;
    }
}
