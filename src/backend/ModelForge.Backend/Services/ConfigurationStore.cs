using System.Collections.Concurrent;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

public interface IConfigurationStore
{
    Task<ConfigurationResponse> GetAsync(string scope, CancellationToken cancellationToken);

    Task<ConfigurationResponse> UpsertAsync(string scope, ConfigurationUpsertRequest request, CancellationToken cancellationToken);
}

public sealed class InMemoryConfigurationStore : IConfigurationStore
{
    private readonly ConcurrentDictionary<string, ConfigurationResponse> _entries = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryConfigurationStore()
    {
        _entries["default"] = new ConfigurationResponse
        {
            Scope = "default",
            Values = new Dictionary<string, string>
            {
                ["TelemetryEnabled"] = "false",
                ["DefaultLanguage"] = "zh-CN",
                ["BackendBridgeMode"] = "local-development"
            }
        };
    }

    public Task<ConfigurationResponse> GetAsync(string scope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedScope = NormalizeScope(scope);
        var entry = _entries.GetOrAdd(normalizedScope, key => new ConfigurationResponse { Scope = key });
        return Task.FromResult(Clone(entry));
    }

    public Task<ConfigurationResponse> UpsertAsync(string scope, ConfigurationUpsertRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedScope = NormalizeScope(scope);
        var entry = _entries.AddOrUpdate(
            normalizedScope,
            _ => new ConfigurationResponse
            {
                Scope = normalizedScope,
                Values = new Dictionary<string, string>(request.Values, StringComparer.OrdinalIgnoreCase),
                UpdatedAtUtc = DateTimeOffset.UtcNow
            },
            (_, existing) =>
            {
                foreach (var pair in request.Values)
                {
                    existing.Values[pair.Key] = pair.Value;
                }

                existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
                return existing;
            });

        return Task.FromResult(Clone(entry));
    }

    private static string NormalizeScope(string scope)
    {
        return string.IsNullOrWhiteSpace(scope) ? "default" : scope.Trim();
    }

    private static ConfigurationResponse Clone(ConfigurationResponse source)
    {
        return new ConfigurationResponse
        {
            Scope = source.Scope,
            Values = new Dictionary<string, string>(source.Values, StringComparer.OrdinalIgnoreCase),
            UpdatedAtUtc = source.UpdatedAtUtc
        };
    }
}
