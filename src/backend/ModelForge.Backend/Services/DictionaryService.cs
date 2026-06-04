using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ModelForge.Backend.Services;

/// <summary>
/// 企业术语字典条目。
/// </summary>
public sealed class DictionaryTerm
{
    public string Id { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string? Replacement { get; set; }
    public string? RegexPattern { get; set; }
    public string Category { get; set; } = "General";
    public string Severity { get; set; } = "Warning"; // Warning | Error | Info
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 术语命中结果。
/// </summary>
public sealed class TermMatch
{
    public string TermId { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string MatchedText { get; set; } = string.Empty;
    public int Position { get; set; }
    public string? Suggestion { get; set; }
}

/// <summary>
/// 术语检查请求。
/// </summary>
public sealed class DictionaryCheckRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Language { get; set; } = "zh-CN";
}

/// <summary>
/// 术语检查响应。
/// </summary>
public sealed class DictionaryCheckResponse
{
    public string OriginalText { get; set; } = string.Empty;
    public List<TermMatch> Matches { get; set; } = new();
    public int MatchCount => Matches.Count;
    public string? CleanedText { get; set; }
}

public interface IDictionaryService
{
    IReadOnlyList<DictionaryTerm> GetAll();
    DictionaryTerm AddOrUpdate(DictionaryTerm term);
    bool Delete(string id);
    DictionaryCheckResponse Check(DictionaryCheckRequest request);
}

/// <summary>
/// 内存术语字典服务。生产环境替换为数据库持久化。
/// </summary>
public sealed class InMemoryDictionaryService : IDictionaryService
{
    private readonly ConcurrentDictionary<string, DictionaryTerm> _terms = new();

    public InMemoryDictionaryService()
    {
        // 预置金融行业常用术语
        Seed("confidential", "机密", null, "Confidential", "Error", "Compliance");
        Seed("draft", "草案", null, "DRAFT", "Warning", "Compliance");
        Seed("internal_only", "内部使用", null, "内部", "Error", "Compliance");
        Seed("tbd", "待定", "确定", @"\bTBD\b", "Info", "Editorial");
        Seed("ebitda", "EBITDA", null, @"\bebitda\b", "Info", "Financial");
        Seed("revenue", "收入/Revenue", null, @"\brevenue\b", "Info", "Financial");
        Seed("npv", "NPV/净现值", null, @"\bnpv\b", "Info", "Financial");
        Seed("irr", "IRR/内部收益率", null, @"\birr\b", "Info", "Financial");
        Seed("pe_ratio", "P/E Ratio", null, @"\bP/E\b", "Info", "Financial");
    }

    private void Seed(string id, string term, string? replacement, string? regex, string severity, string category)
    {
        _terms[id] = new DictionaryTerm
        {
            Id = id,
            Term = term,
            Replacement = replacement,
            RegexPattern = regex ?? Regex.Escape(term),
            Category = category,
            Severity = severity,
        };
    }

    public IReadOnlyList<DictionaryTerm> GetAll() => _terms.Values.ToArray();

    public DictionaryTerm AddOrUpdate(DictionaryTerm term)
    {
        if (string.IsNullOrWhiteSpace(term.Id))
            term.Id = Guid.NewGuid().ToString("N")[..8];
        term.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(term.RegexPattern))
            term.RegexPattern = Regex.Escape(term.Term);

        _terms[term.Id] = term;
        return term;
    }

    public bool Delete(string id) => _terms.TryRemove(id, out _);

    public DictionaryCheckResponse Check(DictionaryCheckRequest request)
    {
        var response = new DictionaryCheckResponse { OriginalText = request.Text };
        var cleaned = request.Text;

        foreach (var term in _terms.Values)
        {
            try
            {
                var pattern = term.RegexPattern ?? Regex.Escape(term.Term);
                var matches = Regex.Matches(request.Text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    response.Matches.Add(new TermMatch
                    {
                        TermId = term.Id,
                        Term = term.Term,
                        MatchedText = match.Value,
                        Position = match.Index,
                        Suggestion = term.Replacement,
                    });

                    // 自动替换
                    if (term.Replacement != null)
                    {
                        cleaned = Regex.Replace(cleaned, pattern, term.Replacement, RegexOptions.IgnoreCase);
                    }
                }
            }
            catch { /* 跳过无效正则 */ }
        }

        response.CleanedText = cleaned != request.Text ? cleaned : null;
        return response;
    }
}
