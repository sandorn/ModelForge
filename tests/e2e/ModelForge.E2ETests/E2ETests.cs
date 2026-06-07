using System.Diagnostics;
using System.Net.Http.Headers;
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
    public async Task Backend_Links_RefreshExisting_ReturnsEnvelope()
    {
        Assert.True(_svc.BackendReady);
        var createResp = await _svc.BackendClient.PostAsJsonAsync("/api/links", new
        {
            sourceType = 0,
            sourceDocumentId = "refresh-e2e-wb",
            sourceAddress = "Sheet1!A1:D20",
            targetType = 0,
            targetDocumentId = "refresh-e2e-ppt",
            targetAddress = "Slide1/Shape1",
            refreshPolicy = "manual"
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var linkId = created.GetProperty("data").GetProperty("linkId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(linkId));

        var response = await _svc.BackendClient.PostAsJsonAsync($"/api/links/{linkId}/refresh", new
        {
            requestedBy = "e2e-runner"
        });

        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(linkId, json.GetProperty("data").GetProperty("linkId").GetString());
        Assert.Equal(0, json.GetProperty("data").GetProperty("status").GetInt32());
        Assert.Null(json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Backend_Audit_PostEvent()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();
        await SendAsAdminAsync(
            HttpMethod.Put,
            "/api/config/default",
            token,
            new
            {
                values = new Dictionary<string, string> { ["TelemetryEnabled"] = "true" },
                updatedBy = "e2e"
            });

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
        Assert.True(json.GetProperty("data").GetProperty("recorded").GetBoolean());
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
    public async Task Backend_AuthLogin_RecordsAuditEvents()
    {
        Assert.True(_svc.BackendReady);
        var failedUsername = $"missing-{Guid.NewGuid():N}"[..20];

        var failedLogin = await _svc.BackendClient.PostAsJsonAsync(
            "/api/auth/login",
            new { username = failedUsername, password = "wrong-password" });
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, failedLogin.StatusCode);

        var token = await LoginAsAdminAsync();
        var auditResponse = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/audit-events?count=100", token);
        Assert.True(auditResponse.IsSuccessStatusCode);
        var auditJson = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        var auditItems = auditJson.GetProperty("data").GetProperty("items").EnumerateArray().ToArray();

        Assert.Contains(auditItems, item =>
            item.GetProperty("eventType").GetString() == "auth.login.failed" &&
            item.GetProperty("actorId").GetString() == failedUsername &&
            item.GetProperty("severity").GetInt32() == 2);
        Assert.Contains(auditItems, item =>
            item.GetProperty("eventType").GetString() == "auth.login.succeeded" &&
            item.GetProperty("resourceId").GetString() == item.GetProperty("actorId").GetString());
    }

    [Fact]
    public async Task Backend_AdminUsers_CreateToggle_ReturnsEnvelopes()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        var listResponse = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/users", token);
        Assert.True(listResponse.IsSuccessStatusCode);
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(listJson.TryGetProperty("traceId", out _));
        Assert.Contains(
            listJson.GetProperty("data").EnumerateArray(),
            user => user.GetProperty("username").GetString() == "admin");

        var username = $"e2e-{Guid.NewGuid():N}"[..16];
        var createBody = new { username, password = "ChangeMe123!", role = "Analyst" };
        var createResponse = await SendAsAdminAsync(HttpMethod.Post, "/api/admin/users", token, createBody);
        Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(createJson.TryGetProperty("traceId", out _));
        Assert.Null(createJson.GetProperty("error").GetString());
        var created = createJson.GetProperty("data");
        var userId = created.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(userId));
        Assert.Equal(username, created.GetProperty("username").GetString());
        Assert.Equal("Analyst", created.GetProperty("role").GetString());
        Assert.True(created.GetProperty("isActive").GetBoolean());

        var toggleResponse = await SendAsAdminAsync(HttpMethod.Put, $"/api/admin/users/{userId}/toggle", token);
        Assert.True(toggleResponse.IsSuccessStatusCode);
        var toggleJson = await toggleResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(userId, toggleJson.GetProperty("data").GetProperty("userId").GetString());
        Assert.False(toggleJson.GetProperty("data").GetProperty("active").GetBoolean());

        var auditResponse = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/audit-events?count=100", token);
        Assert.True(auditResponse.IsSuccessStatusCode);
        var auditJson = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        var auditItems = auditJson.GetProperty("data").GetProperty("items").EnumerateArray().ToArray();
        var createAudit = auditItems.FirstOrDefault(item =>
            item.GetProperty("eventType").GetString() == "admin.user.created" &&
            item.GetProperty("resourceId").GetString() == userId);
        var toggleAudit = auditItems.FirstOrDefault(item =>
            item.GetProperty("eventType").GetString() == "admin.user.toggled" &&
            item.GetProperty("resourceId").GetString() == userId);
        Assert.True(createAudit.ValueKind == JsonValueKind.Object);
        Assert.True(toggleAudit.ValueKind == JsonValueKind.Object);
        Assert.False(string.IsNullOrWhiteSpace(createAudit.GetProperty("actorId").GetString()));
        Assert.Equal(createAudit.GetProperty("actorId").GetString(), toggleAudit.GetProperty("actorId").GetString());

        var duplicateResponse = await SendAsAdminAsync(HttpMethod.Post, "/api/admin/users", token, createBody);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        var duplicateJson = await AssertEnvelopeErrorAsync(duplicateResponse);
        Assert.Contains("already exists", duplicateJson.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_AdminUsers_RejectsInvalidCreateRequests()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        var missingUsername = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/admin/users",
            token,
            new { username = "", password = "ChangeMe123!", role = "Analyst" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingUsername.StatusCode);
        var missingUsernameJson = await AssertEnvelopeErrorAsync(missingUsername);
        Assert.Contains("username", missingUsernameJson.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);

        var invalidRole = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/admin/users",
            token,
            new { username = $"role-{Guid.NewGuid():N}"[..16], password = "ChangeMe123!", role = "Owner" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidRole.StatusCode);
        var invalidRoleJson = await AssertEnvelopeErrorAsync(invalidRole);
        Assert.Contains("role", invalidRoleJson.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_AdminRoles_ReturnsBuiltInPermissions()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        var response = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/roles", token);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var roles = json.GetProperty("data").GetProperty("roles").EnumerateArray().ToArray();
        Assert.Contains(roles, role =>
            role.GetProperty("role").GetString() == "Admin" &&
            role.GetProperty("permissions").EnumerateArray().Any(permission => permission.GetString() == "users.manage"));
        Assert.Contains(roles, role =>
            role.GetProperty("role").GetString() == "Auditor" &&
            role.GetProperty("permissions").EnumerateArray().Any(permission => permission.GetString() == "audit.view"));
    }

    [Fact]
    public async Task Backend_Dispatch_RequiresCommandId()
    {
        Assert.True(_svc.BackendReady);
        var body = new { commandId = "", host = 1 };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/commands/dispatch", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var json = await AssertEnvelopeErrorAsync(response);
        Assert.Contains("commandId", json.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_Links_RequiresFields()
    {
        Assert.True(_svc.BackendReady);
        var body = new { sourceType = 0, sourceDocumentId = "", sourceAddress = "", targetType = 0, targetDocumentId = "", targetAddress = "" };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/links", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var json = await AssertEnvelopeErrorAsync(response);
        Assert.Contains("sourceDocumentId", json.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_Audit_RequiresEventType()
    {
        Assert.True(_svc.BackendReady);
        var body = new { eventType = "", actorId = "test", host = 1, severity = 1 };
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var json = await AssertEnvelopeErrorAsync(response);
        Assert.Contains("eventType", json.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_Audit_RespectsTelemetryDisabledForInformationalEvents()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();
        await SendAsAdminAsync(
            HttpMethod.Put,
            "/api/config/default",
            token,
            new
            {
                values = new Dictionary<string, string> { ["TelemetryEnabled"] = "false" },
                updatedBy = "e2e"
            });

        var skipped = await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", new
        {
            eventType = "e2e.telemetry.skipped",
            actorId = "telemetry-user",
            host = 1,
            severity = 1,
            commandId = "excel.fill-down"
        });
        var skippedJson = await skipped.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(System.Net.HttpStatusCode.Accepted, skipped.StatusCode);
        Assert.False(skippedJson.GetProperty("data").GetProperty("recorded").GetBoolean());
        Assert.Equal("skipped", skippedJson.GetProperty("data").GetProperty("eventId").GetString());

        var warning = await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", new
        {
            eventType = "e2e.telemetry.warning",
            actorId = "telemetry-user",
            host = 1,
            severity = 2,
            commandId = "excel.fill-down"
        });
        var warningJson = await warning.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(System.Net.HttpStatusCode.Accepted, warning.StatusCode);
        Assert.True(warningJson.GetProperty("data").GetProperty("recorded").GetBoolean());
    }

    [Fact]
    public async Task Backend_AdminAuditExport_ReturnsCsv()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();
        await SendAsAdminAsync(
            HttpMethod.Put,
            "/api/config/default",
            token,
            new
            {
                values = new Dictionary<string, string> { ["TelemetryEnabled"] = "true" },
                updatedBy = "e2e"
            });
        await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", new
        {
            eventType = "e2e.export",
            actorId = "csv-user",
            host = 1,
            severity = 1,
            commandId = "excel.fill-down"
        });
        var response = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/audit-events/export?count=20", token);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("text/csv", response.Content.Headers.ContentType?.MediaType ?? "");
        Assert.Contains("eventId,recordedAtUtc,eventType,actorId,host,severity,commandId,resourceId", body);
        Assert.Contains("e2e.export", body);
        Assert.Contains("csv-user", body);
    }

    [Fact]
    public async Task Backend_AdminAuditSummary_ReturnsBuckets()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();
        await SendAsAdminAsync(
            HttpMethod.Put,
            "/api/config/default",
            token,
            new
            {
                values = new Dictionary<string, string> { ["TelemetryEnabled"] = "true" },
                updatedBy = "e2e"
            });
        await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", new
        {
            eventType = "e2e.summary",
            actorId = "summary-user",
            host = 1,
            severity = 1,
            commandId = "excel.model-check"
        });
        var response = await SendAsAdminAsync(
            HttpMethod.Get,
            "/api/admin/audit-events/summary?hours=24&eventType=e2e.summary&actorId=summary-user",
            token);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var data = json.GetProperty("data");
        Assert.True(data.GetProperty("totalEvents").GetInt32() >= 1);
        Assert.True(data.GetProperty("bucketHours").GetInt32() >= 1);
        Assert.Contains(
            data.GetProperty("timeline").EnumerateArray(),
            bucket => bucket.GetProperty("count").GetInt32() >= 1);
        Assert.Contains(
            data.GetProperty("byEventType").EnumerateArray(),
            bucket => bucket.GetProperty("key").GetString() == "e2e.summary");
        Assert.Contains(
            data.GetProperty("byActor").EnumerateArray(),
            bucket => bucket.GetProperty("key").GetString() == "summary-user");
    }

    [Fact]
    public async Task Backend_AdminAuditEvents_SupportsServerFilters()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();
        await SendAsAdminAsync(
            HttpMethod.Put,
            "/api/config/default",
            token,
            new
            {
                values = new Dictionary<string, string> { ["TelemetryEnabled"] = "true" },
                updatedBy = "e2e"
            });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var matchingActor = $"filter-user-{suffix}";
        var otherActor = $"filter-other-{suffix}";
        await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", new
        {
            eventType = "e2e.filter.match",
            actorId = matchingActor,
            host = 1,
            severity = 2,
            commandId = "excel.filter-match"
        });
        await _svc.BackendClient.PostAsJsonAsync("/api/audit-events", new
        {
            eventType = "e2e.filter.other",
            actorId = otherActor,
            host = 3,
            severity = 1,
            commandId = "word.filter-other"
        });

        var listResponse = await SendAsAdminAsync(
            HttpMethod.Get,
            $"/api/admin/audit-events?count=100&eventType=e2e.filter.match&actorId={matchingActor}&host=1&severity=2&search=filter-match",
            token);
        var listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.True(listResponse.IsSuccessStatusCode, listBody);
        var listJson = JsonSerializer.Deserialize<JsonElement>(listBody);
        var data = listJson.GetProperty("data");
        var items = data.GetProperty("items").EnumerateArray().ToArray();
        Assert.Single(items);
        Assert.Equal("e2e.filter.match", items[0].GetProperty("eventType").GetString());
        Assert.Equal(matchingActor, items[0].GetProperty("actorId").GetString());
        Assert.Equal(1, items[0].GetProperty("host").GetInt32());
        Assert.Equal(2, items[0].GetProperty("severity").GetInt32());
        Assert.Equal(1, data.GetProperty("pagination").GetProperty("total").GetInt32());
        Assert.Equal(matchingActor, data.GetProperty("query").GetProperty("actorId").GetString());

        var summaryResponse = await SendAsAdminAsync(
            HttpMethod.Get,
            $"/api/admin/audit-events/summary?hours=24&actorId={matchingActor}",
            token);
        var summaryBody = await summaryResponse.Content.ReadAsStringAsync();
        Assert.True(summaryResponse.IsSuccessStatusCode, summaryBody);
        var summaryJson = JsonSerializer.Deserialize<JsonElement>(summaryBody).GetProperty("data");
        Assert.True(summaryJson.GetProperty("totalEvents").GetInt32() >= 1);
        Assert.Contains(
            summaryJson.GetProperty("byEventType").EnumerateArray(),
            bucket => bucket.GetProperty("key").GetString() == "e2e.filter.match");
        Assert.Contains(
            summaryJson.GetProperty("heatmap").EnumerateArray(),
            cell =>
                cell.GetProperty("rowKey").GetString() == "e2e.filter.match" &&
                cell.GetProperty("columnKey").GetString() == "Excel" &&
                cell.GetProperty("count").GetInt32() >= 1);

        var exportResponse = await SendAsAdminAsync(
            HttpMethod.Get,
            $"/api/admin/audit-events/export?count=100&actorId={matchingActor}",
            token);
        var exportBody = await exportResponse.Content.ReadAsStringAsync();
        Assert.True(exportResponse.IsSuccessStatusCode, exportBody);
        Assert.Contains("e2e.filter.match", exportBody);
        Assert.Contains(matchingActor, exportBody);
        Assert.DoesNotContain(otherActor, exportBody);
    }

    [Fact]
    public async Task Backend_AdminAuditRetention_ReturnsEnvelopeAndValidatesDays()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        var invalidResponse = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/admin/audit-events/retention",
            token,
            new { retentionDays = 0, dryRun = true });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        var invalidJson = await AssertEnvelopeErrorAsync(invalidResponse);
        Assert.Contains("retentionDays", invalidJson.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);

        var dryRunResponse = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/admin/audit-events/retention",
            token,
            new { retentionDays = 90, dryRun = true });
        var dryRunBody = await dryRunResponse.Content.ReadAsStringAsync();
        Assert.True(dryRunResponse.IsSuccessStatusCode, dryRunBody);
        var data = JsonSerializer.Deserialize<JsonElement>(dryRunBody).GetProperty("data");

        Assert.Equal(90, data.GetProperty("retentionDays").GetInt32());
        Assert.True(data.GetProperty("dryRun").GetBoolean());
        Assert.Equal(0, data.GetProperty("deletedEvents").GetInt32());
        Assert.True(data.TryGetProperty("cutoffUtc", out _));
    }

    [Fact]
    public async Task Backend_AdminDiagnostics_ReturnsSafeSummary()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        await SendAsAdminAsync(
            HttpMethod.Put,
            "/api/config/default",
            token,
            new
            {
                values = new Dictionary<string, string>
                {
                    ["TelemetryEnabled"] = "false",
                    ["ServiceToken"] = "super-secret-value",
                    ["OpenAiApiKey"] = "sk-test-secret"
                },
                updatedBy = "e2e"
            });
        var response = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/diagnostics", token);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var data = json.GetProperty("data");
        Assert.Equal("ModelForge", data.GetProperty("version").GetProperty("product").GetString());
        Assert.True(data.GetProperty("commandCount").GetInt32() >= 20);
        Assert.True(data.GetProperty("dictionaryTermCount").GetInt32() >= 1);
        Assert.True(data.GetProperty("auditRetentionDays").GetInt32() >= 1);
        Assert.True(data.GetProperty("auditEventsEligibleForRetentionPrune").GetInt32() >= 0);
        Assert.True(data.GetProperty("configuration").TryGetProperty("TelemetryEnabled", out _));
        Assert.Equal("[REDACTED]", data.GetProperty("configuration").GetProperty("ServiceToken").GetString());
        Assert.Equal("[REDACTED]", data.GetProperty("configuration").GetProperty("OpenAiApiKey").GetString());
        Assert.DoesNotContain("admin123", body);
        Assert.DoesNotContain("super-secret-value", body);
        Assert.DoesNotContain("sk-test-secret", body);
        Assert.DoesNotContain("Jwt", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_AdminDiagnosticsBundle_ReturnsSafeJsonFile()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        var response = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/diagnostics/bundle", token);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType ?? "");
        Assert.NotNull(response.Content.Headers.ContentDisposition?.FileName);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("ModelForge", json.GetProperty("summary").GetProperty("version").GetProperty("product").GetString());
        Assert.True(json.GetProperty("runtime").TryGetProperty("frameworkDescription", out _));
        Assert.True(json.GetProperty("recentAuditEvents").ValueKind == JsonValueKind.Array);
        Assert.DoesNotContain("admin123", body);
        Assert.DoesNotContain("Jwt", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sk-", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_Dictionary_Errors_ReturnEnvelope()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        var invalidCreate = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/dictionary/",
            token,
            new { id = "", term = "" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidCreate.StatusCode);
        var invalidJson = await AssertEnvelopeErrorAsync(invalidCreate);
        Assert.Contains("term", invalidJson.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);

        var missingDelete = await SendAsAdminAsync(
            HttpMethod.Delete,
            $"/api/dictionary/missing-{Guid.NewGuid():N}",
            token);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, missingDelete.StatusCode);
        var missingJson = await AssertEnvelopeErrorAsync(missingDelete);
        Assert.Contains("not found", missingJson.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_Dictionary_ImportExport_ReturnsEnvelope()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();
        var id = $"bulk-{Guid.NewGuid():N}"[..16];

        var importResponse = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/dictionary/import",
            token,
            new
            {
                overwrite = true,
                terms = new[]
                {
                    new { id, term = "批量导入术语", category = "Custom", severity = "Warning" }
                }
            });
        Assert.True(importResponse.IsSuccessStatusCode);
        var importJson = await importResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, importJson.GetProperty("data").GetProperty("imported").GetInt32());
        Assert.Contains(
            importJson.GetProperty("data").GetProperty("terms").EnumerateArray(),
            term => term.GetProperty("id").GetString() == id);

        var exportResponse = await SendAsAdminAsync(HttpMethod.Get, "/api/dictionary/export", token);
        Assert.True(exportResponse.IsSuccessStatusCode);
        var exportJson = await exportResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(exportJson.GetProperty("data").GetProperty("count").GetInt32() >= 1);
        Assert.Contains(
            exportJson.GetProperty("data").GetProperty("terms").EnumerateArray(),
            term => term.GetProperty("id").GetString() == id);
    }

    [Fact]
    public async Task Backend_AdminWriteOperations_RecordAuditEvents()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();
        var termId = $"audit-{Guid.NewGuid():N}"[..16];

        var configResponse = await SendAsAdminAsync(
            HttpMethod.Put,
            "/api/config/default",
            token,
            new
            {
                values = new Dictionary<string, string> { ["TelemetryEnabled"] = "false", ["AuditTestKey"] = termId },
                updatedBy = "e2e-audit"
            });
        Assert.True(configResponse.IsSuccessStatusCode);

        var upsertResponse = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/dictionary/",
            token,
            new { id = termId, term = "审计术语", category = "Audit", severity = "Warning" });
        Assert.Equal(System.Net.HttpStatusCode.Created, upsertResponse.StatusCode);

        var importResponse = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/dictionary/import",
            token,
            new
            {
                overwrite = true,
                terms = new[] { new { id = $"{termId}-i", term = "导入审计术语", category = "Audit", severity = "Warning" } }
            });
        Assert.True(importResponse.IsSuccessStatusCode);

        var deleteResponse = await SendAsAdminAsync(HttpMethod.Delete, $"/api/dictionary/{termId}", token);
        Assert.True(deleteResponse.IsSuccessStatusCode);

        var auditResponse = await SendAsAdminAsync(HttpMethod.Get, "/api/admin/audit-events?count=100", token);
        Assert.True(auditResponse.IsSuccessStatusCode);
        var auditJson = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        var auditItems = auditJson.GetProperty("data").GetProperty("items").EnumerateArray().ToArray();

        Assert.Contains(auditItems, item =>
            item.GetProperty("eventType").GetString() == "admin.config.updated" &&
            item.GetProperty("resourceId").GetString() == "default");
        Assert.Contains(auditItems, item =>
            item.GetProperty("eventType").GetString() == "admin.dictionary.term.upserted" &&
            item.GetProperty("resourceId").GetString() == termId);
        Assert.Contains(auditItems, item =>
            item.GetProperty("eventType").GetString() == "admin.dictionary.imported");
        Assert.Contains(auditItems, item =>
            item.GetProperty("eventType").GetString() == "admin.dictionary.term.deleted" &&
            item.GetProperty("resourceId").GetString() == termId);
    }

    [Fact]
    public async Task Backend_Dictionary_ImportRejectsEmptyTerms()
    {
        Assert.True(_svc.BackendReady);
        var token = await LoginAsAdminAsync();

        var response = await SendAsAdminAsync(
            HttpMethod.Post,
            "/api/dictionary/import",
            token,
            new { terms = Array.Empty<object>() });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var json = await AssertEnvelopeErrorAsync(response);
        Assert.Contains("terms", json.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Backend_Dictionary_ServiceExportRequiresServiceToken()
    {
        Assert.True(_svc.BackendReady);

        var rejected = await _svc.BackendClient.GetAsync("/api/dictionary/service-export");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, rejected.StatusCode);
        await AssertEnvelopeErrorAsync(rejected);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/dictionary/service-export");
        request.Headers.Add("X-Service-Token", ServiceFixture.TestServiceToken);
        var accepted = await _svc.BackendClient.SendAsync(request);
        var body = await accepted.Content.ReadAsStringAsync();

        Assert.True(accepted.IsSuccessStatusCode, body);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.True(json.GetProperty("data").GetProperty("count").GetInt32() >= 1);
        Assert.Contains(
            json.GetProperty("data").GetProperty("terms").EnumerateArray(),
            term => !string.IsNullOrWhiteSpace(term.GetProperty("term").GetString()));
    }

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin123" });
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Admin login failed: {response.StatusCode}: {bodyText}");
        var json = JsonSerializer.Deserialize<JsonElement>(bodyText);
        var token = json.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private async Task<HttpResponseMessage> SendAsAdminAsync(HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        return await _svc.BackendClient.SendAsync(request);
    }

    private static async Task<JsonElement> AssertEnvelopeErrorAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("traceId", out _));
        Assert.True(json.TryGetProperty("error", out var error));
        Assert.False(string.IsNullOrWhiteSpace(error.GetString()));
        return json;
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
    public async Task Sidecar_Shortcuts_ImportExport_ReturnsEnvelopes()
    {
        Assert.True(_svc.SidecarReady);

        var exportResponse = await _svc.SidecarClient.GetAsync("/api/shortcuts/export");
        Assert.True(exportResponse.IsSuccessStatusCode);
        var exportJson = await exportResponse.Content.ReadFromJsonAsync<JsonElement>();
        var exported = exportJson.GetProperty("data").GetProperty("shortcuts").EnumerateArray().ToList();
        Assert.True(exported.Count >= 21, $"Expected >= 21 exported shortcuts, got {exported.Count}");

        var originalShortcuts = exported.Select(item => new
        {
            commandId = item.GetProperty("commandId").GetString(),
            displayName = item.GetProperty("displayName").GetString(),
            shortcut = item.GetProperty("shortcut").GetString()
        }).ToArray();

        var importResponse = await _svc.SidecarClient.PostAsJsonAsync("/api/shortcuts/import", new
        {
            shortcuts = new[]
            {
                new { commandId = "excel.fill-right", displayName = "快速向右填充", shortcut = "Ctrl+Alt+Shift+R" }
            }
        });
        Assert.True(importResponse.IsSuccessStatusCode);
        var importJson = await importResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, importJson.GetProperty("data").GetProperty("imported").GetInt32());
        Assert.Equal("Ctrl+Alt+Shift+R",
            importJson.GetProperty("data").GetProperty("shortcuts")[0].GetProperty("shortcut").GetString());

        var restoreResponse = await _svc.SidecarClient.PostAsJsonAsync("/api/shortcuts/import", new { shortcuts = originalShortcuts });
        Assert.True(restoreResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Sidecar_ProtectedApi_RequiresLocalToken()
    {
        Assert.True(_svc.SidecarReady);
        using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5200"), Timeout = TimeSpan.FromSeconds(10) };

        var response = await client.GetAsync("/api/shortcuts/export");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Sidecar local API token", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sidecar_Shortcuts_ImportRejectsDuplicates()
    {
        Assert.True(_svc.SidecarReady);
        var beforeResponse = await _svc.SidecarClient.GetAsync("/api/shortcuts");
        var beforeJson = await beforeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var beforeCount = beforeJson.EnumerateArray().Count();

        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/shortcuts/import", new
        {
            shortcuts = new[]
            {
                new { commandId = "cmd.a", displayName = "A", shortcut = "Ctrl+Alt+1" },
                new { commandId = "cmd.b", displayName = "B", shortcut = "Ctrl+Alt+1" }
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var errorJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("冲突", errorJson.GetProperty("error").GetString());

        var afterResponse = await _svc.SidecarClient.GetAsync("/api/shortcuts");
        var afterJson = await afterResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(beforeCount, afterJson.EnumerateArray().Count());
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
        var json = JsonSerializer.Deserialize<JsonElement>(bodyText);
        Assert.True(json.TryGetProperty("traceId", out _));
        Assert.True(json.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task Sidecar_Execute_RequiresCommandId()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "", host = "excel" };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("commandId", json.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(json.GetProperty("data").GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Sidecar_Execute_ValidationFailure_ReportsBackendAudit()
    {
        Assert.True(_svc.BackendReady);
        Assert.True(_svc.SidecarReady);
        var token = await LoginAsAdminAsync();
        await SetTelemetryAsync(token, true);

        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", new { commandId = "", host = "excel" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        var found = await WaitForAuditEventAsync(
            token,
            item =>
                item.GetProperty("eventType").GetString() == "command.validation_failed" &&
                item.GetProperty("actorId").GetString() == "local-sidecar" &&
                item.GetProperty("severity").GetInt32() == 2);

        Assert.True(found, "Expected Sidecar validation failure to be reported to Backend audit events.");
    }

    [Fact]
    public async Task Sidecar_Execute_InvalidHost()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "excel.fill-down", host = "invalidhost" };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Invalid host", json.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sidecar_Execute_RejectsUnknownCommand()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "excel.unknown-command", host = "excel" };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("不支持", json.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(json.GetProperty("data").GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Sidecar_Status_ReturnsEnvelope()
    {
        Assert.True(_svc.SidecarReady);
        var response = await _svc.SidecarClient.GetAsync("/api/status");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("traceId", out _));
        Assert.True(json.GetProperty("data").TryGetProperty("connected", out _));
    }

    [Fact]
    public async Task Sidecar_ExcelInfo_LegacyAlias_ReturnsEnvelope()
    {
        Assert.True(_svc.SidecarReady);
        var response = await _svc.SidecarClient.GetAsync("/api/excel/info");
        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("traceId", out _));
        Assert.True(json.GetProperty("data").TryGetProperty("connected", out _));
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

    [Fact]
    public async Task Sidecar_Execute_AcceptsNamesManagerCommand()
    {
        Assert.True(_svc.SidecarReady);
        var body = new { commandId = "excel.names-manager", host = "excel", arguments = new Dictionary<string, string> { ["action"] = "scan" } };
        var response = await _svc.SidecarClient.PostAsJsonAsync("/api/execute", body);
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(bodyText);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
            $"Expected 200 or 503, got {response.StatusCode}: {bodyText[..Math.Min(200, bodyText.Length)]}");
        Assert.NotEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _svc.BackendClient.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin123" });
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Admin login failed: {response.StatusCode}: {bodyText}");
        var json = JsonSerializer.Deserialize<JsonElement>(bodyText);
        var token = json.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private async Task SetTelemetryAsync(string token, bool enabled)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/config/default");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            values = new Dictionary<string, string> { ["TelemetryEnabled"] = enabled ? "true" : "false" },
            updatedBy = "e2e-sidecar"
        });

        var response = await _svc.BackendClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
    }

    private async Task<bool> WaitForAuditEventAsync(string token, Func<JsonElement, bool> predicate)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/audit-events?count=100");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _svc.BackendClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, body);

            var json = JsonSerializer.Deserialize<JsonElement>(body);
            if (json.GetProperty("data").GetProperty("items").EnumerateArray().Any(predicate))
            {
                return true;
            }

            await Task.Delay(250);
        }

        return false;
    }
}
