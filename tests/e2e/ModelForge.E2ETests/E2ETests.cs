using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ModelForge.E2ETests;

[Collection("E2E")]
public class BackendE2ETests : IClassFixture<ServiceFixture>
{
    private readonly ServiceFixture _svc;
    public BackendE2ETests(ServiceFixture svc) => _svc = svc;

    [Fact]
    public async Task Backend_Health_ReturnsHealthy()
    {
        Assert.True(_svc.BackendReady, "Backend service not ready");
        var response = await _svc.BackendClient.GetAsync("/health");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Backend_Version_ReturnsVersion()
    {
        Assert.True(_svc.BackendReady);
        var response = await _svc.BackendClient.GetAsync("/api/version");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ModelForge", json.GetProperty("data").GetProperty("product").GetString());
    }

    [Fact]
    public async Task Backend_Commands_ReturnsAllThreeHosts()
    {
        Assert.True(_svc.BackendReady);
        var response = await _svc.BackendClient.GetAsync("/api/commands");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var commands = json.GetProperty("data").EnumerateArray().ToList();
        Assert.True(commands.Count >= 25, $"Expected >= 25 commands, got {commands.Count}");
        Assert.Contains(commands, c => c.GetProperty("host").GetInt32() == 1);
        Assert.Contains(commands, c => c.GetProperty("host").GetInt32() == 2);
        Assert.Contains(commands, c => c.GetProperty("host").GetInt32() == 3);
    }

    [Fact]
    public async Task Backend_Config_ReadOnly()
    {
        Assert.True(_svc.BackendReady);
        var scope = "default";
        var getResp = await _svc.BackendClient.GetAsync($"/api/config/{scope}");
        Assert.True(getResp.IsSuccessStatusCode);
        var json = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        var data = json.GetProperty("data");
        Assert.Equal("default", data.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task Backend_Commands_AllWordCommands()
    {
        Assert.True(_svc.BackendReady);
        var response = await _svc.BackendClient.GetAsync("/api/commands");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var commands = json.GetProperty("data").EnumerateArray()
            .Where(c => c.GetProperty("host").GetInt32() == 3)
            .Select(c => c.GetProperty("id").GetString()).ToList();
        Assert.Contains("word.build-due-diligence", commands);
        Assert.Contains("word.build-cim", commands);
        Assert.Contains("word.build-management-presentation", commands);
        Assert.Contains("word.embed-excel-range", commands);
        Assert.Contains("word.refresh-links", commands);
    }

    [Fact]
    public async Task Backend_Commands_AllPptCommands()
    {
        Assert.True(_svc.BackendReady);
        var response = await _svc.BackendClient.GetAsync("/api/commands");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var commands = json.GetProperty("data").EnumerateArray()
            .Where(c => c.GetProperty("host").GetInt32() == 2)
            .Select(c => c.GetProperty("id").GetString()).ToList();
        Assert.Contains("ppt.generate-agenda", commands);
        Assert.Contains("ppt.deck-check", commands);
        Assert.Contains("ppt.align-left", commands);
        Assert.Contains("ppt.distribute-horizontal", commands);
        Assert.Contains("ppt.unify-size", commands);
    }

    [Fact]
    public async Task Backend_Commands_ExcelNamesManager()
    {
        Assert.True(_svc.BackendReady);
        var response = await _svc.BackendClient.GetAsync("/api/commands");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var commands = json.GetProperty("data").EnumerateArray()
            .Where(c => c.GetProperty("host").GetInt32() == 1)
            .Select(c => c.GetProperty("id").GetString()).ToList();
        Assert.Contains("excel.names-manager", commands);
    }

    [Fact]
    public async Task Backend_Links_CreateAndGet()
    {
        Assert.True(_svc.BackendReady);
        var createBody = new
        {
            sourceType = 0,
            sourceDocumentId = "e2e-test-wb",
            sourceAddress = "Sheet1!A1:D20",
            targetType = 0,
            targetDocumentId = "e2e-test-ppt",
            targetAddress = "Slide1/Shape1",
            refreshPolicy = "manual"
        };
        var createResp = await _svc.BackendClient.PostAsJsonAsync("/api/links", createBody);
        Assert.Equal(System.Net.HttpStatusCode.Created, createResp.StatusCode);

        var getResp = await _svc.BackendClient.GetAsync("/api/links");
        Assert.True(getResp.IsSuccessStatusCode);
        var json = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        var links = json.GetProperty("data").EnumerateArray().ToList();
        Assert.Contains(links, l => l.GetProperty("sourceDocumentId").GetString() == "e2e-test-wb");
    }

    [Fact]
    public async Task Backend_Audit_PostEvent()
    {
        Assert.True(_svc.BackendReady);
        var body = new
        {
            eventType = "e2e.test",
            actorId = "e2e-runner",
            host = 1,
            severity = 1,
            commandId = "excel.fill-down"
        };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", body);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(json.GetProperty("data").GetProperty("eventId").GetString());
    }

    [Fact]
    public async Task Backend_Dictionary_GetAll()
    {
        Assert.True(_svc.BackendReady);
        var response = await _svc.BackendClient.GetAsync("/api/dictionary/");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var terms = json.GetProperty("data").EnumerateArray().ToList();
        Assert.True(terms.Count >= 8, $"Expected >= 8 seed terms, got {terms.Count}");
    }

    [Fact]
    public async Task Backend_Dictionary_Check()
    {
        Assert.True(_svc.BackendReady);
        var body = new { text = "This is a DRAFT confidential document with TBD items.", language = "en" };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/dictionary/check", body);
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var matches = json.GetProperty("data").GetProperty("matches").EnumerateArray().ToList();
        Assert.True(matches.Count >= 2, $"Expected >= 2 term matches, got {matches.Count}");

    }



    [Fact]
    public async Task Backend_Audit_GetRequiresAuth()
    {
        Assert.True(_svc.BackendReady);
        // Admin audit endpoint requires authentication
        var response = await _svc.BackendClient.GetAsync("/api/admin/audit-events?count=2");
        // Should return 401 Unauthorized (or 403 Forbidden) without auth
        Assert.True((int)response.StatusCode == 401 || (int)response.StatusCode == 403,
            $"Expected 401 or 403, got {response.StatusCode}");
    }

    [Fact]
    public async Task Backend_Dispatch_RequiresCommandId()
    {
        Assert.True(_svc.BackendReady);
        var body = new { commandId = "", host = 1 };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/commands/dispatch", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Backend_Links_RequiresFields()
    {
        Assert.True(_svc.BackendReady);
        var body = new { sourceType = 0, sourceDocumentId = "", sourceAddress = "", targetType = 0, targetDocumentId = "", targetAddress = "" };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/links", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Backend_Audit_RequiresEventType()
    {
        Assert.True(_svc.BackendReady);
        var body = new { eventType = "", actorId = "test", host = 1, severity = 1 };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }


}

[Collection("E2E")]
public class SidecarE2ETests : IClassFixture<ServiceFixture>
{
    private readonly ServiceFixture _svc;
    public SidecarE2ETests(ServiceFixture svc) => _svc = svc;

    [Fact]
    public async Task Sidecar_Health_ReturnsHealthy()
    {
        Assert.True(_svc.SidecarReady, "Sidecar service not ready");
        var response = await _svc.SidecarClient.GetAsync("/health");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Sidecar_Shortcuts_ReturnsList()
    {
        Assert.True(_svc.SidecarReady);
        var response = await _svc.SidecarClient.GetAsync("/api/shortcuts");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var shortcuts = json.EnumerateArray().ToList();
        Assert.True(shortcuts.Count >= 21, $"Expected >= 21 shortcuts, got {shortcuts.Count}");
    }

    [Fact]
    public async Task Sidecar_Execute_AcceptsValidCommand()
    {
        Assert.True(_svc.SidecarReady, "Sidecar service not ready");
        var body = new { commandId = "excel.model-check", host = "excel", arguments = new Dictionary<string, string>() };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(bodyText);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
            $"Expected 200 or 503, got {response.StatusCode}: {bodyText[..Math.Min(200, bodyText.Length)]}");
    }

    [Fact]
    public async Task Sidecar_Execute_RequiresCommandId()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "", host = "excel" };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.Contains("commandId", bodyText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sidecar_Execute_InvalidHost()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "excel.fill-down", host = "invalidhost" };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        Assert.True((int)response.StatusCode >= 400, $"Expected error status for invalid host, got {response.StatusCode}");
    }

    [Fact]
    public async Task Sidecar_ExcelInfo_ReturnsStatus()
    {
        Assert.True(_svc.SidecarReady);
        var response = await _svc.SidecarClient.GetAsync("/api/excel/info");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("connected", out _));
    }

    [Fact]
    public async Task Sidecar_Execute_AcceptsPowerPointCommand()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "ppt.deck-check", host = "powerpoint", arguments = new Dictionary<string, string>() };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(bodyText);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
            $"Expected 200 or 503, got {response.StatusCode}: {bodyText[..Math.Min(200, bodyText.Length)]}");
    }

    [Fact]
    public async Task Sidecar_Execute_AcceptsWordCommand()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "word.build-cim", host = "word", arguments = new Dictionary<string, string>() };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(bodyText);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
            $"Expected 200 or 503, got {response.StatusCode}: {bodyText[..Math.Min(200, bodyText.Length)]}");
    }

    [Fact]
    public async Task Sidecar_Execute_HostCaseInsensitive()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "excel.fill-down", host = "EXCEL" };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(bodyText);
        Assert.NotEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
