using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests.Services;

public class TelemetryPolicyTests
{
    [Fact]
    public async Task ShouldRecordAuditEventAsync_SkipsInformationalWhenTelemetryDisabled()
    {
        var store = new InMemoryConfigurationStore();
        var policy = new TelemetryPolicy();

        var result = await policy.ShouldRecordAuditEventAsync(
            new AuditEventRequest { EventType = "command.executed", Severity = AuditSeverity.Information },
            store,
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ShouldRecordAuditEventAsync_RecordsInformationalWhenTelemetryEnabled()
    {
        var store = new InMemoryConfigurationStore();
        await store.UpsertAsync("default", new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["TelemetryEnabled"] = "true" }
        }, CancellationToken.None);
        var policy = new TelemetryPolicy();

        var result = await policy.ShouldRecordAuditEventAsync(
            new AuditEventRequest { EventType = "command.executed", Severity = AuditSeverity.Information },
            store,
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ShouldRecordAuditEventAsync_AlwaysRecordsSecurityAndWarnings()
    {
        var store = new InMemoryConfigurationStore();
        var policy = new TelemetryPolicy();

        var security = await policy.ShouldRecordAuditEventAsync(
            new AuditEventRequest { EventType = "security.token.invalid", Severity = AuditSeverity.Information },
            store,
            CancellationToken.None);
        var warning = await policy.ShouldRecordAuditEventAsync(
            new AuditEventRequest { EventType = "command.failed", Severity = AuditSeverity.Warning },
            store,
            CancellationToken.None);

        Assert.True(security);
        Assert.True(warning);
    }
}
