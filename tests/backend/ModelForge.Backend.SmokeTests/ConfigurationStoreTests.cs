using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests.Services;

public class ConfigurationStoreTests
{
    [Fact]
    public async Task UpsertAsync_CreatesNewConfiguration()
    {
        var store = new InMemoryConfigurationStore();
        var request = new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["key1"] = "value1" },
            UpdatedBy = "admin"
        };

        var result = await store.UpsertAsync("test-scope", request, CancellationToken.None);

        Assert.Equal("test-scope", result.Scope);
        Assert.Equal("value1", result.Values["key1"]);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsExistingConfiguration()
    {
        var store = new InMemoryConfigurationStore();
        var request = new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["theme"] = "dark" },
            UpdatedBy = "admin"
        };
        await store.UpsertAsync("ui", request, CancellationToken.None);

        var result = await store.GetAsync("ui", CancellationToken.None);

        Assert.Equal("ui", result.Scope);
        Assert.Equal("dark", result.Values["theme"]);
    }

    [Fact]
    public async Task GetAsync_NonExistentScope_ReturnsEmptyValues()
    {
        var store = new InMemoryConfigurationStore();

        var result = await store.GetAsync("nonexistent", CancellationToken.None);

        Assert.Equal("nonexistent", result.Scope);
        Assert.Empty(result.Values);
    }

    [Fact]
    public async Task UpsertAsync_OverwritesExistingValues()
    {
        var store = new InMemoryConfigurationStore();
        var request1 = new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["key"] = "old" }
        };
        await store.UpsertAsync("scope", request1, CancellationToken.None);

        var request2 = new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["key"] = "new" }
        };
        var result = await store.UpsertAsync("scope", request2, CancellationToken.None);

        Assert.Equal("new", result.Values["key"]);
    }
}
