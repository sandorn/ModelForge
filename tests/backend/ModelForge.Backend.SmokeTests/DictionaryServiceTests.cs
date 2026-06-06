using ModelForge.Backend.Services;
using Xunit;

namespace ModelForge.Backend.Tests.Services;

public class DictionaryServiceTests
{
    [Fact]
    public void GetAll_ReturnsSeededTerms()
    {
        var service = new InMemoryDictionaryService();
        var terms = service.GetAll();

        Assert.True(terms.Count >= 5, $"Expected at least 5 seed terms, got {terms.Count}");
    }

    [Fact]
    public void Check_MatchesKnownTerm()
    {
        var service = new InMemoryDictionaryService();
        var request = new DictionaryCheckRequest { Text = "This document is confidential" };

        var response = service.Check(request);

        Assert.NotEmpty(response.Matches);
        Assert.Contains(response.Matches, m => m.Term == "机密");
    }

    [Fact]
    public void Check_AutoReplacesReplacementTerm()
    {
        var service = new InMemoryDictionaryService();
        var request = new DictionaryCheckRequest { Text = "Value is TBD" };

        var response = service.Check(request);

        Assert.NotEmpty(response.Matches);
        Assert.Contains(response.Matches, m => m.Term == "待定" && m.Suggestion == "确定");
        Assert.NotNull(response.CleanedText);
        Assert.DoesNotContain("TBD", response.CleanedText);
    }

    [Fact]
    public void Check_NoMatches_ReturnsEmptyMatches()
    {
        var service = new InMemoryDictionaryService();
        var request = new DictionaryCheckRequest { Text = "clean text without any issues" };

        var response = service.Check(request);

        Assert.Empty(response.Matches);
        Assert.Null(response.CleanedText);
    }

    [Fact]
    public void AddOrUpdate_NewTerm_AppearsInGetAll()
    {
        var service = new InMemoryDictionaryService();
        var term = new DictionaryTerm
        {
            Id = "custom-1",
            Term = "自定义术语",
            Category = "Custom",
            Severity = "Warning"
        };

        service.AddOrUpdate(term);
        var terms = service.GetAll();

        Assert.Contains(terms, t => t.Id == "custom-1");
        Assert.Contains(terms, t => t.Term == "自定义术语");
    }

    [Fact]
    public void AddOrUpdate_UpdatesExistingTerm()
    {
        var service = new InMemoryDictionaryService();
        var term = new DictionaryTerm
        {
            Id = "custom-1",
            Term = "原始术语",
            Category = "Custom"
        };
        service.AddOrUpdate(term);

        var updated = new DictionaryTerm
        {
            Id = "custom-1",
            Term = "更新术语",
            Category = "Custom"
        };
        service.AddOrUpdate(updated);

        var terms = service.GetAll();
        var found = Assert.Single(terms, t => t.Id == "custom-1");
        Assert.Equal("更新术语", found.Term);
    }

    [Fact]
    public void Delete_ExistingTerm_ReturnsTrue()
    {
        var service = new InMemoryDictionaryService();
        var term = new DictionaryTerm { Id = "to-delete", Term = "待删除", Category = "Custom" };
        service.AddOrUpdate(term);

        var result = service.Delete("to-delete");

        Assert.True(result);
        Assert.DoesNotContain(service.GetAll(), t => t.Id == "to-delete");
    }

    [Fact]
    public void Delete_NonExistentTerm_ReturnsFalse()
    {
        var service = new InMemoryDictionaryService();

        var result = service.Delete("nonexistent");

        Assert.False(result);
    }
}
