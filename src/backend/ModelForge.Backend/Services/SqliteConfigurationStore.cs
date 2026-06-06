using Microsoft.EntityFrameworkCore;
using ModelForge.Backend.Data;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

/// <summary>
/// SQLite-persisted configuration store.
/// Replaces InMemoryConfigurationStore for production use.
/// </summary>
public sealed class SqliteConfigurationStore : IConfigurationStore
{
    private readonly ModelForgeDbContext _db;

    public SqliteConfigurationStore(ModelForgeDbContext db) => _db = db;

    public async Task<ConfigurationResponse> GetAsync(string scope, CancellationToken ct)
    {
        var entries = await _db.Configurations
            .Where(e => e.Scope == scope)
            .ToListAsync(ct);

        var values = entries.ToDictionary(e => e.Key, e => e.Value);
        var updatedAt = entries.MaxBy(e => e.UpdatedAtUtc)?.UpdatedAtUtc ?? DateTimeOffset.UtcNow;

        return new ConfigurationResponse
        {
            Scope = scope,
            Values = values,
            UpdatedAtUtc = updatedAt
        };
    }

    public async Task<ConfigurationResponse> UpsertAsync(string scope, ConfigurationUpsertRequest request, CancellationToken ct)
    {
        foreach (var (key, value) in request.Values)
        {
            var existing = await _db.Configurations
                .FirstOrDefaultAsync(e => e.Scope == scope && e.Key == key, ct);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedBy = request.UpdatedBy;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                _db.Configurations.Add(new ConfigurationEntry
                {
                    Scope = scope,
                    Key = key,
                    Value = value,
                    UpdatedBy = request.UpdatedBy,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return await GetAsync(scope, ct);
    }
}