using System.Text;
using System.Text.Json;

namespace ModelForge.Backend.Services;

/// <summary>
/// AIWA LLM 调用封装 — 支持 Ollama / OpenAI-compatible (Agnes 等) / Mock。
/// </summary>
public sealed class AiwaService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _provider; // ollama | openai-compatible | mock
    private readonly Uri _apiBaseUrl;
    private readonly string? _apiKey;

    public AiwaService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _provider = configuration.GetValue<string>("AIWA:Provider") ?? "mock";
        _model = configuration.GetValue<string>("AIWA:Model") ?? "agnes-2.0-flash";
        var baseUrl = configuration.GetValue<string>("AIWA:ApiUrl") ?? "http://localhost:11434";
        _apiBaseUrl = new Uri(baseUrl);
        _apiKey = configuration.GetValue<string>("AIWA:ApiKey");
    }

    public string Provider => _provider;
    public string Model => _model;

    public async Task<string> ChatAsync(string message, string mode, CancellationToken cancellation = default)
    {
        return _provider switch
        {
            "openai-compatible" => await ChatOpenAiAsync(message, mode, cancellation),
            "ollama" => await ChatOllamaAsync(message, mode, cancellation),
            _ => await ChatMockAsync(message, mode, cancellation)
        };
    }

    private async Task<string> ChatOpenAiAsync(string message, string mode, CancellationToken cancellation)
    {
        var systemPrompt = GetSystemPrompt(mode);
        var body = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = message }
            },
            max_tokens = 4096,
            temperature = 0.3
        };

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_apiBaseUrl, "/v1/chat/completions"))
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_apiKey))
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _http.SendAsync(request, cancellation);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellation);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return content.Trim();
    }

    private async Task<string> ChatOllamaAsync(string message, string mode, CancellationToken cancellation)
    {
        var systemPrompt = GetSystemPrompt(mode);
        var prompt = $"{systemPrompt}\n\n用户输入:\n{message}";

        var body = new { model = _model, prompt, stream = false };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(new Uri(_apiBaseUrl, "/api/generate"), content, cancellation);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellation);
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
        return result.Trim();
    }

    private static Task<string> ChatMockAsync(string message, string mode, CancellationToken cancellation)
    {
        var result = mode switch
        {
            "summarize" => $"[Mock Summary] Analyzed input. Key points extracted. (AI provider not configured. Set AIWA:Provider to 'ollama' or 'openai-compatible'.)",
            "expand" => $"[Mock Expand] Elaborated content based on input. (Configure AI provider for real responses.)",
            "rewrite" => $"[Mock Rewrite] Text rewritten in professional style. (Configure AI provider for real responses.)",
            "proofread" => $"[Mock Proofread] No obvious errors found. (Configure AI provider for real responses.)",
            "translate" => $"[Mock Translation] Translation result here. (Configure AI provider for real responses.)",
            "explain" => $"[Mock Explain] Formula analysis: each part explained step by step. (Configure AI provider for real responses.)",
            _ => $"[Mock] AI provider not configured. Set environment variables AIWA:Provider, AIWA:ApiUrl, and AIWA:ApiKey for production use."
        };
        return Task.FromResult(result);
    }

    private static string GetSystemPrompt(string mode) => mode switch
    {
        "summarize" => "You are a professional financial document summarizer. Summarize concisely in Chinese, highlighting key figures and conclusions.",
        "expand" => "You are a professional financial analysis writer. Expand bullet points into detailed analysis with industry context and logical reasoning in Chinese.",
        "rewrite" => "You are a professional business document editor. Rewrite text in formal investment banking style while preserving meaning. Output in Chinese.",
        "proofread" => "You are a strict financial document proofreader. Check grammar, word choice, number consistency, and list issues with suggestions. Output in Chinese.",
        "translate" => "You are a professional Chinese-English translator. Translate accurately preserving professional terminology.",
        "explain" => "You are an Excel formula expert. Explain each part of the formula step by step in Chinese, with calculation logic and optimization suggestions.",
        _ => "You are a professional Office productivity AI assistant. Help the user with their content in Chinese."
    };
}
