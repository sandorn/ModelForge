using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

public sealed class TelemetryPolicy
{
    public async Task<bool> ShouldRecordAuditEventAsync(
        AuditEventRequest request,
        IConfigurationStore configurationStore,
        CancellationToken cancellationToken)
    {
        if (request.Severity >= AuditSeverity.Warning)
        {
            return true;
        }

        if (request.EventType.StartsWith("admin.", StringComparison.OrdinalIgnoreCase) ||
            request.EventType.StartsWith("security.", StringComparison.OrdinalIgnoreCase) ||
            request.EventType.StartsWith("auth.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var config = await configurationStore.GetAsync("default", cancellationToken);
        if (!config.Values.TryGetValue("TelemetryEnabled", out var value))
        {
            return false;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
