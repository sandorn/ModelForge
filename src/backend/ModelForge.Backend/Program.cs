using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using ModelForge.Backend.Auth;
using ModelForge.Backend.Data;
using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog 日志配置 ──
var logPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "ModelForge", "logs", "backend-.log");
Serilog.Log.Logger = new Serilog.LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(logPath, rollingInterval: Serilog.RollingInterval.Day, retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Logging.AddSerilog();

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "ModelForge.Backend";
});

var loggerFactory = LoggerFactory.Create(b => b.AddSerilog());
var logger = loggerFactory.CreateLogger("ModelForge.Backend");

// ── JWT Auth Configuration ──
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<InMemoryUserStore>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new JwtService(jwtOptions, NullLogger<JwtService>.Instance).GetValidationParameters();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("AuditorOrAdmin", p => p.RequireRole("Admin", "Auditor"));
});

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentAddIn", policy =>
    {
        policy
            .WithOrigins("https://localhost:5173", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ── Persistence ──
var provider = builder.Configuration.GetValue<string>("DatabaseProvider")?.ToLowerInvariant() ?? "inmemory";
var connectionString = builder.Configuration.GetConnectionString("ModelForge") ?? "";
var serviceToken = builder.Configuration.GetValue<string>("ModelForge:ServiceToken") ?? "";

if (provider == "inmemory")
{
    logger.LogInformation("Using in-memory stores (set DatabaseProvider=sqlite or postgres for persistence)");
    builder.Services.AddSingleton<IConfigurationStore, InMemoryConfigurationStore>();
    builder.Services.AddSingleton<IAuditSink, InMemoryAuditSink>();
    builder.Services.AddSingleton<ILinkMetadataStore, InMemoryLinkMetadataStore>();
}
else
{
    if (string.IsNullOrWhiteSpace(connectionString) && provider != "sqlite")
    {
        logger.LogWarning("No connection string configured for {Provider}, falling back to in-memory", provider);
        builder.Services.AddSingleton<IConfigurationStore, InMemoryConfigurationStore>();
        builder.Services.AddSingleton<IAuditSink, InMemoryAuditSink>();
        builder.Services.AddSingleton<ILinkMetadataStore, InMemoryLinkMetadataStore>();
    }
    else
    {
        var finalCs = provider == "sqlite" && string.IsNullOrWhiteSpace(connectionString)
            ? "Data Source=modelforge.db"
            : connectionString;

        builder.Services.AddDbContext<ModelForgeDbContext>(options =>
        {
            switch (provider)
            {
                case "postgres":
                    options.UseNpgsql(finalCs);
                    break;
                default:
                    options.UseSqlite(finalCs);
                    break;
            }
        });

        logger.LogInformation("Using {Provider} persistence", provider);
        builder.Services.AddScoped<IConfigurationStore, SqliteConfigurationStore>();
        builder.Services.AddScoped<IAuditSink, SqliteAuditSink>();
        builder.Services.AddScoped<ILinkMetadataStore, SqliteLinkMetadataStore>();
    }
}

builder.Services.AddSingleton<ICommandCatalog, CommandCatalog>();
builder.Services.AddSingleton<ICommandDispatcher, InMemoryCommandDispatcher>();
builder.Services.AddSingleton<IDictionaryService, InMemoryDictionaryService>();
builder.Services.AddSingleton<TelemetryPolicy>();
builder.Services.AddHttpClient<AiwaService>();

var app = builder.Build();

// ── Auto-migrate on startup ──
if (provider != "inmemory")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ModelForgeDbContext>();
    db.Database.EnsureCreated();
    logger.LogInformation("Database ensured created.");
}

app.UseCors("DevelopmentAddIn");
app.UseAuthentication();
app.UseAuthorization();

// ── TraceId middleware ──
app.Use(async (context, next) =>
{
    var traceId = context.Request.Headers.TryGetValue("X-Trace-Id", out var incoming)
        ? incoming.ToString()
        : Guid.NewGuid().ToString("N");
    context.Items["TraceId"] = traceId;
    context.Response.Headers["X-Trace-Id"] = traceId;
    await next();
});

// ── Global exception handler ──
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var envelope = ApiEnvelope<object>.Failure("Internal server error.", GetTraceId(context));
        await context.Response.WriteAsJsonAsync(envelope);
    }
});

// ── Auth Endpoints ──
var auth = app.MapGroup("/api/auth");

auth.MapPost("/login", async (HttpContext ctx, LoginRequest request, JwtService jwt, InMemoryUserStore store, IAuditSink auditSink) =>
{
    var user = store.ValidateCredentials(request.Username, request.Password);
    if (user == null)
    {
        await RecordBackendAuditAsync(
            ctx,
            auditSink,
            logger,
            "auth.login.failed",
            AuditSeverity.Warning,
            actorId: string.IsNullOrWhiteSpace(request.Username) ? "anonymous" : request.Username.Trim(),
            metadata: new Dictionary<string, string>
            {
                ["username"] = request.Username
            });
        return Results.Json(
            ApiEnvelope<object>.Failure("Invalid username or password.", GetTraceId(ctx)),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var token = jwt.IssueToken(user.Id, user.Username, user.Role);
    var loginResponse = new LoginResponse
    {
        Token = token,
        UserId = user.Id,
        Username = user.Username,
        Role = user.Role,
        ExpiresAt = DateTime.UtcNow.AddHours(jwtOptions.TokenLifetimeHours)
    };
    await RecordBackendAuditAsync(
        ctx,
        auditSink,
        logger,
        "auth.login.succeeded",
        AuditSeverity.Information,
        actorId: user.Id,
        resourceId: user.Id,
        metadata: new Dictionary<string, string>
        {
            ["username"] = user.Username,
            ["role"] = user.Role
        });

    return Results.Ok(ApiEnvelope<LoginResponse>.Success(loginResponse, GetTraceId(ctx)));
});

auth.MapGet("/me", (HttpContext ctx, ClaimsPrincipal user, InMemoryUserStore store) =>
{
    var userId = user.FindFirstValue("sub");
    if (userId == null)
        return Results.Json(
            ApiEnvelope<object>.Failure("Missing user identity.", GetTraceId(ctx)),
            statusCode: StatusCodes.Status401Unauthorized);
    var identity = store.GetById(userId);
    if (identity == null)
        return Results.NotFound(ApiEnvelope<object>.Failure($"User '{userId}' not found.", GetTraceId(ctx)));
    return Results.Ok(ApiEnvelope<object>.Success(new { identity.Id, identity.Username, identity.Role, identity.IsActive }, GetTraceId(ctx)));
}).RequireAuthorization();

// ── Admin Endpoints ──
var admin = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly");
admin.MapGet("/users", (HttpContext ctx, InMemoryUserStore store) =>
    Results.Ok(ApiEnvelope<IReadOnlyCollection<AdminUserResponse>>.Success(
        store.GetAll().Select(ToAdminUserResponse).ToArray(),
        GetTraceId(ctx))));
admin.MapPost("/users", async (HttpContext ctx, AdminUserCreateRequest req, InMemoryUserStore store, IAuditSink auditSink) =>
{
    if (string.IsNullOrWhiteSpace(req.Username))
        return Results.BadRequest(ApiEnvelope<object>.Failure("username is required.", GetTraceId(ctx)));
    if (string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(ApiEnvelope<object>.Failure("password is required.", GetTraceId(ctx)));

    var role = string.IsNullOrWhiteSpace(req.Role) ? RoleDefinitions.Analyst : req.Role.Trim();
    var canonicalRole = RoleDefinitions.AllRoles.FirstOrDefault(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    if (canonicalRole is null)
        return Results.BadRequest(ApiEnvelope<object>.Failure(
            $"role must be one of: {string.Join(", ", RoleDefinitions.AllRoles)}.",
            GetTraceId(ctx)));

    UserIdentity user;
    try
    {
        user = store.AddUser(req.Username, req.Password, canonicalRole);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(ApiEnvelope<object>.Failure(ex.Message, GetTraceId(ctx)));
    }

    await RecordBackendAuditAsync(
        ctx,
        auditSink,
        logger,
        "admin.user.created",
        AuditSeverity.Information,
        resourceId: user.Id,
        metadata: new Dictionary<string, string>
        {
            ["username"] = user.Username,
            ["role"] = user.Role
        });

    return Results.Created(
        $"/api/admin/users/{user.Id}",
        ApiEnvelope<AdminUserResponse>.Success(ToAdminUserResponse(user), GetTraceId(ctx)));
});
admin.MapPut("/users/{userId}/toggle", async (HttpContext ctx, string userId, InMemoryUserStore store, IAuditSink auditSink) =>
{
    var user = store.GetById(userId);
    if (user is null)
        return Results.NotFound(ApiEnvelope<object>.Failure($"User '{userId}' not found.", GetTraceId(ctx)));
    var nextActive = !user.IsActive;
    store.SetActive(userId, nextActive);
    await RecordBackendAuditAsync(
        ctx,
        auditSink,
        logger,
        "admin.user.toggled",
        AuditSeverity.Information,
        resourceId: userId,
        metadata: new Dictionary<string, string>
        {
            ["username"] = user.Username,
            ["active"] = nextActive.ToString()
        });

    return Results.Ok(ApiEnvelope<AdminUserToggleResponse>.Success(
        new AdminUserToggleResponse { UserId = userId, Active = nextActive },
        GetTraceId(ctx)));
});
admin.MapPut("/users/{userId}", async (HttpContext ctx, string userId, AdminUserUpdateRequest req, InMemoryUserStore store, IAuditSink auditSink) =>
{
    try
    {
        var user = store.UpdateUser(userId, req.Password, req.Role);
        if (user is null)
            return Results.NotFound(ApiEnvelope<object>.Failure($"User '{userId}' not found.", GetTraceId(ctx)));

        await RecordBackendAuditAsync(
            ctx, auditSink, logger, "admin.user.updated",
            AuditSeverity.Information, resourceId: userId,
            metadata: new Dictionary<string, string>
            {
                ["username"] = user.Username,
                ["role"] = user.Role
            });

        return Results.Ok(ApiEnvelope<AdminUserResponse>.Success(ToAdminUserResponse(user), GetTraceId(ctx)));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ApiEnvelope<object>.Failure(ex.Message, GetTraceId(ctx)));
    }
});
admin.MapDelete("/users/{userId}", async (HttpContext ctx, string userId, InMemoryUserStore store, IAuditSink auditSink) =>
{
    var user = store.GetById(userId);
    if (user is null)
        return Results.NotFound(ApiEnvelope<object>.Failure($"User '{userId}' not found.", GetTraceId(ctx)));

    store.DeleteUser(userId);
    await RecordBackendAuditAsync(
        ctx, auditSink, logger, "admin.user.deleted",
        AuditSeverity.Warning, resourceId: userId,
        metadata: new Dictionary<string, string>
        {
            ["username"] = user.Username
        });

    return Results.Ok(ApiEnvelope<object>.Success(new { deleted = userId }, GetTraceId(ctx)));
});
admin.MapGet("/roles", (HttpContext ctx) =>
    Results.Ok(ApiEnvelope<AdminRolesResponse>.Success(
        new AdminRolesResponse
        {
            Roles = RoleDefinitions.AllRoles
                .Select(role => new AdminRolePermissionResponse
                {
                    Role = role,
                    Permissions = RoleDefinitions.GetPermissions(role),
                    BuiltIn = RoleDefinitions.BuiltInRoles.Contains(role)
                })
                .ToArray()
        },
        GetTraceId(ctx))));
admin.MapPost("/roles", async (HttpContext ctx, AdminRoleCreateRequest req, IAuditSink auditSink) =>
{
    if (string.IsNullOrWhiteSpace(req.Role) || req.Permissions is null || req.Permissions.Length == 0)
        return Results.BadRequest(ApiEnvelope<object>.Failure("role and permissions are required.", GetTraceId(ctx)));

    if (!RoleDefinitions.AddCustomRole(req.Role, req.Permissions))
        return Results.Conflict(ApiEnvelope<object>.Failure($"Role '{req.Role}' already exists or is built-in.", GetTraceId(ctx)));

    await RecordBackendAuditAsync(ctx, auditSink, logger, "admin.role.created",
        AuditSeverity.Warning, resourceId: req.Role);
    return Results.Created($"/api/admin/roles/{req.Role}",
        ApiEnvelope<object>.Success(new { role = req.Role, permissions = req.Permissions }, GetTraceId(ctx)));
});
admin.MapDelete("/roles/{roleName}", async (HttpContext ctx, string roleName, IAuditSink auditSink) =>
{
    if (!RoleDefinitions.RemoveCustomRole(roleName))
        return Results.NotFound(ApiEnvelope<object>.Failure($"Role '{roleName}' not found or is built-in.", GetTraceId(ctx)));

    await RecordBackendAuditAsync(ctx, auditSink, logger, "admin.role.deleted",
        AuditSeverity.Warning, resourceId: roleName);
    return Results.Ok(ApiEnvelope<object>.Success(new { deleted = roleName }, GetTraceId(ctx)));
});
admin.MapGet("/audit-events", async (
    HttpContext ctx,
    IAuditSink auditSink,
    int? count,
    int? page,
    int? pageSize,
    string? eventType,
    string? actorId,
    OfficeHost? host,
    AuditSeverity? severity,
    string? commandId,
    string? resourceId,
    string? search,
    DateTimeOffset? sinceUtc,
    DateTimeOffset? untilUtc) =>
{
    var effectivePage = Math.Max(1, page ?? 1);
    var effectivePageSize = Math.Clamp(pageSize ?? count ?? 50, 1, 500);
    var effectiveCount = Math.Clamp(count ?? (effectivePage * effectivePageSize), 1, 500);
    var query = new AdminAuditEventsQuery
    {
        Count = effectiveCount,
        Page = effectivePage,
        PageSize = effectivePageSize,
        EventType = NormalizeFilter(eventType),
        ActorId = NormalizeFilter(actorId),
        Host = host,
        Severity = severity,
        CommandId = NormalizeFilter(commandId),
        ResourceId = NormalizeFilter(resourceId),
        Search = NormalizeFilter(search),
        SinceUtc = sinceUtc,
        UntilUtc = untilUtc
    };
    if (query.SinceUtc > query.UntilUtc)
        return Results.BadRequest(ApiEnvelope<object>.Failure("sinceUtc must be earlier than untilUtc.", GetTraceId(ctx)));

    var events = await auditSink.GetRecentAsync(GetAuditFetchCount(query, 500), ctx.RequestAborted);
    var filteredEvents = ApplyAuditEventFilters(events, query).ToArray();
    var result = filteredEvents
        .Skip((effectivePage - 1) * effectivePageSize)
        .Take(effectivePageSize)
        .Select(ToAdminAuditEventItem)
        .ToArray();
    var response = new AdminAuditEventsResponse
    {
        Items = result,
        Pagination = new PaginationResponse { Page = effectivePage, PageSize = effectivePageSize, Total = filteredEvents.Length },
        Query = query
    };
    return Results.Ok(ApiEnvelope<AdminAuditEventsResponse>.Success(response, GetTraceId(ctx)));
});
admin.MapGet("/audit-events/export", async (
    HttpContext ctx,
    IAuditSink auditSink,
    int? count,
    string? eventType,
    string? actorId,
    OfficeHost? host,
    AuditSeverity? severity,
    string? commandId,
    string? resourceId,
    string? search,
    DateTimeOffset? sinceUtc,
    DateTimeOffset? untilUtc) =>
{
    var effectiveCount = Math.Clamp(count ?? 500, 1, 5000);
    var query = new AdminAuditEventsQuery
    {
        Count = effectiveCount,
        EventType = NormalizeFilter(eventType),
        ActorId = NormalizeFilter(actorId),
        Host = host,
        Severity = severity,
        CommandId = NormalizeFilter(commandId),
        ResourceId = NormalizeFilter(resourceId),
        Search = NormalizeFilter(search),
        SinceUtc = sinceUtc,
        UntilUtc = untilUtc
    };
    if (query.SinceUtc > query.UntilUtc)
        return Results.BadRequest(ApiEnvelope<object>.Failure("sinceUtc must be earlier than untilUtc.", GetTraceId(ctx)));

    var events = await auditSink.GetRecentAsync(GetAuditFetchCount(query, 5000), ctx.RequestAborted);
    var csv = BuildAuditEventsCsv(ApplyAuditEventFilters(events, query).Take(effectiveCount).ToArray());
    var fileName = $"modelforge-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
    return Results.File(
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
        "text/csv; charset=utf-8",
        fileName);
});
admin.MapGet("/audit-events/summary", async (
    HttpContext ctx,
    IAuditSink auditSink,
    int? hours,
    int? count,
    string? eventType,
    string? actorId,
    OfficeHost? host,
    AuditSeverity? severity,
    string? commandId,
    string? resourceId,
    string? search,
    DateTimeOffset? sinceUtc,
    DateTimeOffset? untilUtc) =>
{
    var windowHours = Math.Clamp(hours ?? 168, 1, 24 * 30);
    var effectiveCount = Math.Clamp(count ?? 1000, 1, 5000);
    var since = DateTimeOffset.UtcNow.AddHours(-windowHours);
    var bucketHours = GetAuditTimelineBucketHours(windowHours);
    var query = new AdminAuditEventsQuery
    {
        Count = effectiveCount,
        EventType = NormalizeFilter(eventType),
        ActorId = NormalizeFilter(actorId),
        Host = host,
        Severity = severity,
        CommandId = NormalizeFilter(commandId),
        ResourceId = NormalizeFilter(resourceId),
        Search = NormalizeFilter(search),
        SinceUtc = sinceUtc,
        UntilUtc = untilUtc
    };
    if (query.SinceUtc > query.UntilUtc)
        return Results.BadRequest(ApiEnvelope<object>.Failure("sinceUtc must be earlier than untilUtc.", GetTraceId(ctx)));

    var events = await auditSink.GetRecentAsync(GetAuditFetchCount(query, 5000), ctx.RequestAborted);
    var windowEvents = events
        .Where(e => AuditEventMatches(e, query))
        .Where(e => e.Response.RecordedAtUtc >= since)
        .ToArray();

    var response = new AdminAuditSummaryResponse
    {
        GeneratedAtUtc = DateTimeOffset.UtcNow,
        WindowHours = windowHours,
        BucketHours = bucketHours,
        TotalEvents = windowEvents.Length,
        ByEventType = BuildAuditSummaryBuckets(windowEvents, e => e.Request.EventType),
        ByHost = BuildAuditSummaryBuckets(windowEvents, e => e.Request.Host.ToString()),
        ByActor = BuildAuditSummaryBuckets(windowEvents, e => string.IsNullOrWhiteSpace(e.Request.ActorId) ? "anonymous" : e.Request.ActorId),
        Timeline = BuildAuditTimelineBuckets(windowEvents, since, DateTimeOffset.UtcNow, bucketHours),
        Heatmap = BuildAuditHeatmap(windowEvents),
        Query = query
    };

    return Results.Ok(ApiEnvelope<AdminAuditSummaryResponse>.Success(response, GetTraceId(ctx)));
});
admin.MapPost("/audit-events/retention", async (
    HttpContext ctx,
    IAuditSink auditSink,
    IConfigurationStore configurationStore,
    IConfiguration appConfiguration,
    AdminAuditRetentionRequest request) =>
{
    var retentionDays = request.RetentionDays ?? await GetAuditRetentionDaysAsync(
        configurationStore,
        appConfiguration,
        ctx.RequestAborted);
    if (retentionDays < 1 || retentionDays > 3650)
    {
        return Results.BadRequest(ApiEnvelope<object>.Failure(
            "retentionDays must be between 1 and 3650.",
            GetTraceId(ctx)));
    }

    var cutoffUtc = DateTimeOffset.UtcNow.AddDays(-retentionDays);
    var matchedEvents = await auditSink.CountBeforeAsync(cutoffUtc, ctx.RequestAborted);
    var deletedEvents = request.DryRun
        ? 0
        : await auditSink.DeleteBeforeAsync(cutoffUtc, ctx.RequestAborted);
    if (!request.DryRun)
    {
        await RecordBackendAuditAsync(
            ctx,
            auditSink,
            logger,
            "admin.audit.retention.pruned",
            AuditSeverity.Warning,
            resourceId: "audit-events",
            metadata: new Dictionary<string, string>
            {
                ["retentionDays"] = retentionDays.ToString(),
                ["cutoffUtc"] = cutoffUtc.ToString("O"),
                ["matchedEvents"] = matchedEvents.ToString(),
                ["deletedEvents"] = deletedEvents.ToString()
            });
    }

    var response = new AdminAuditRetentionResponse
    {
        RetentionDays = retentionDays,
        CutoffUtc = cutoffUtc,
        MatchedEvents = matchedEvents,
        DeletedEvents = deletedEvents,
        DryRun = request.DryRun,
        ExecutedAtUtc = DateTimeOffset.UtcNow
    };

    return Results.Ok(ApiEnvelope<AdminAuditRetentionResponse>.Success(response, GetTraceId(ctx)));
});
admin.MapGet("/diagnostics", async (
    HttpContext ctx,
    ICommandCatalog commandCatalog,
    ILinkMetadataStore linkStore,
    IDictionaryService dictionaryService,
    IConfigurationStore configurationStore,
    IConfiguration appConfiguration,
    IAuditSink auditSink) =>
{
    var response = await BuildAdminDiagnosticsAsync(
        ctx,
        provider,
        commandCatalog,
        linkStore,
        dictionaryService,
        configurationStore,
        appConfiguration,
        auditSink);

    return Results.Ok(ApiEnvelope<AdminDiagnosticsResponse>.Success(response, GetTraceId(ctx)));
});
admin.MapGet("/diagnostics/bundle", async (
    HttpContext ctx,
    ICommandCatalog commandCatalog,
    ILinkMetadataStore linkStore,
    IDictionaryService dictionaryService,
    IConfigurationStore configurationStore,
    IConfiguration appConfiguration,
    IAuditSink auditSink) =>
{
    var summary = await BuildAdminDiagnosticsAsync(
        ctx,
        provider,
        commandCatalog,
        linkStore,
        dictionaryService,
        configurationStore,
        appConfiguration,
        auditSink);
    var auditEvents = await auditSink.GetRecentAsync(100, ctx.RequestAborted);
    var bundle = new AdminDiagnosticsBundleResponse
    {
        Summary = summary,
        Runtime = BuildRuntimeDiagnostics(),
        RecentAuditEvents = auditEvents.Select(ToAdminAuditEventItem).ToArray(),
        Notes =
        [
            "This diagnostics bundle is JSON-only and excludes log files, secrets, authentication tokens, and workbook contents.",
            "Use MSI install/uninstall logs separately when debugging installer failures."
        ]
    };
    var payload = JsonSerializer.SerializeToUtf8Bytes(bundle, new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    });
    var fileName = $"modelforge-diagnostics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
    return Results.File(payload, "application/json; charset=utf-8", fileName);
});

// ── Public Endpoints ──
app.MapGet("/health", async (HttpContext ctx) =>
{
    var health = new HealthResponse();
    var dbStatus = await GetDatabaseStatusAsync(ctx, provider);
    return Results.Ok(ApiEnvelope<object>.Success(new { health.Status, health.Service, health.TimestampUtc, database = dbStatus }, GetTraceId(ctx)));
});
app.MapGet("/api/version", (HttpContext ctx) =>
    Results.Ok(ApiEnvelope<VersionInfoResponse>.Success(new VersionInfoResponse(), GetTraceId(ctx))));

var api = app.MapGroup("/api");
api.MapGet("/commands", (HttpContext ctx, ICommandCatalog catalog) =>
    Results.Ok(ApiEnvelope<IReadOnlyCollection<CommandDefinition>>.Success(catalog.GetAll(), GetTraceId(ctx))));
api.MapPost("/commands/dispatch", async (HttpContext ctx, ICommandDispatcher dispatcher, CommandDispatchRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.CommandId))
        return Results.BadRequest(ApiEnvelope<object>.Failure("commandId is required.", GetTraceId(ctx)));
    var response = await dispatcher.DispatchAsync(request, ctx.RequestAborted);
    return Results.Accepted($"/api/commands/dispatch/{response.DispatchId}",
        ApiEnvelope<CommandDispatchResponse>.Success(response, GetTraceId(ctx)));
});
api.MapGet("/config/{scope}", async (HttpContext ctx, IConfigurationStore store, string scope) =>
{
    var response = await store.GetAsync(scope, ctx.RequestAborted);
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(response, GetTraceId(ctx)));
});
api.MapPut("/config/{scope}", async (HttpContext ctx, IConfigurationStore store, IAuditSink auditSink, string scope, ConfigurationUpsertRequest request) =>
{
    var response = await store.UpsertAsync(scope, request, ctx.RequestAborted);
    await RecordBackendAuditAsync(
        ctx,
        auditSink,
        logger,
        "admin.config.updated",
        AuditSeverity.Information,
        resourceId: scope,
        metadata: new Dictionary<string, string>
        {
            ["updatedBy"] = request.UpdatedBy ?? string.Empty,
            ["keyCount"] = request.Values.Count.ToString()
        });
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(response, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");
api.MapPost("/audit-events", async (
    HttpContext ctx,
    IAuditSink auditSink,
    IConfigurationStore configurationStore,
    TelemetryPolicy telemetryPolicy,
    AuditEventRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.EventType))
        return Results.BadRequest(ApiEnvelope<object>.Failure("eventType is required.", GetTraceId(ctx)));
    if (!await telemetryPolicy.ShouldRecordAuditEventAsync(request, configurationStore, ctx.RequestAborted))
    {
        return Results.Accepted(
            "/api/audit-events/skipped",
            ApiEnvelope<AuditEventResponse>.Success(
                new AuditEventResponse
                {
                    EventId = "skipped",
                    RecordedAtUtc = DateTimeOffset.UtcNow,
                    Recorded = false,
                    Message = "Telemetry is disabled for non-security informational events."
                },
                GetTraceId(ctx)));
    }

    var response = await auditSink.RecordAsync(request, ctx.RequestAborted);
    return Results.Accepted($"/api/audit-events/{response.EventId}",
        ApiEnvelope<AuditEventResponse>.Success(response, GetTraceId(ctx)));
});
api.MapGet("/links", async (HttpContext ctx, ILinkMetadataStore store) =>
{
    var response = await store.GetAllAsync(ctx.RequestAborted);
    return Results.Ok(ApiEnvelope<IReadOnlyCollection<LinkMetadata>>.Success(response, GetTraceId(ctx)));
});
api.MapPost("/links", async (HttpContext ctx, ILinkMetadataStore store, CreateLinkMetadataRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.SourceDocumentId) || string.IsNullOrWhiteSpace(request.SourceAddress) ||
        string.IsNullOrWhiteSpace(request.TargetDocumentId) || string.IsNullOrWhiteSpace(request.TargetAddress))
        return Results.BadRequest(ApiEnvelope<object>.Failure(
            "sourceDocumentId, sourceAddress, targetDocumentId, and targetAddress are required.",
            GetTraceId(ctx)));
    var response = await store.CreateAsync(request, ctx.RequestAborted);
    return Results.Created($"/api/links/{response.LinkId}",
        ApiEnvelope<LinkMetadata>.Success(response, GetTraceId(ctx)));
});
api.MapPost("/links/{linkId}/refresh", async (HttpContext ctx, ILinkMetadataStore store, string linkId, LinkRefreshRequest request) =>
{
    if (string.IsNullOrWhiteSpace(linkId))
        return Results.BadRequest(ApiEnvelope<object>.Failure("linkId is required.", GetTraceId(ctx)));
    request.LinkId = linkId;
    var response = await store.MarkRefreshRequestedAsync(request, ctx.RequestAborted);
    return Results.Accepted($"/api/links/{linkId}",
        ApiEnvelope<LinkRefreshResponse>.Success(response, GetTraceId(ctx)));
});

// ── Corporate Dictionary ──
var dict = app.MapGroup("/api/dictionary");
dict.MapGet("/", (HttpContext ctx, IDictionaryService service) =>
    Results.Ok(ApiEnvelope<IReadOnlyList<DictionaryTerm>>.Success(service.GetAll(), GetTraceId(ctx))));
dict.MapPost("/", async (HttpContext ctx, DictionaryTerm term, IDictionaryService service, IAuditSink auditSink) =>
{
    if (string.IsNullOrWhiteSpace(term.Term))
        return Results.BadRequest(ApiEnvelope<object>.Failure("term is required.", GetTraceId(ctx)));
    var created = service.AddOrUpdate(term);
    await RecordBackendAuditAsync(
        ctx,
        auditSink,
        logger,
        "admin.dictionary.term.upserted",
        AuditSeverity.Information,
        resourceId: created.Id,
        metadata: new Dictionary<string, string>
        {
            ["term"] = created.Term,
            ["category"] = created.Category,
            ["severity"] = created.Severity
        });
    return Results.Created($"/api/dictionary/{created.Id}",
        ApiEnvelope<DictionaryTerm>.Success(created, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");
dict.MapGet("/export", (HttpContext ctx, IDictionaryService service) =>
    Results.Ok(ApiEnvelope<DictionaryExportResponse>.Success(
        new DictionaryExportResponse { Terms = service.GetAll() },
        GetTraceId(ctx)))).RequireAuthorization("AdminOnly");
dict.MapGet("/service-export", (HttpContext ctx, IDictionaryService service) =>
{
    if (!IsValidServiceToken(ctx, serviceToken))
    {
        return Results.Json(
            ApiEnvelope<object>.Failure("Valid service token is required.", GetTraceId(ctx)),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    return Results.Ok(ApiEnvelope<DictionaryExportResponse>.Success(
        new DictionaryExportResponse { Terms = service.GetAll() },
        GetTraceId(ctx)));
});
dict.MapPost("/import", async (HttpContext ctx, DictionaryImportRequest request, IDictionaryService service, IAuditSink auditSink) =>
{
    if (request.Terms.Count == 0)
        return Results.BadRequest(ApiEnvelope<object>.Failure("terms is required.", GetTraceId(ctx)));
    var response = service.Import(request);
    await RecordBackendAuditAsync(
        ctx,
        auditSink,
        logger,
        "admin.dictionary.imported",
        AuditSeverity.Information,
        metadata: new Dictionary<string, string>
        {
            ["imported"] = response.Imported.ToString(),
            ["overwrite"] = request.Overwrite.ToString()
        });
    return Results.Ok(ApiEnvelope<DictionaryImportResponse>.Success(response, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");
dict.MapDelete("/{id}", async (HttpContext ctx, string id, IDictionaryService service, IAuditSink auditSink) =>
{
    if (service.Delete(id))
    {
        await RecordBackendAuditAsync(
            ctx,
            auditSink,
            logger,
            "admin.dictionary.term.deleted",
            AuditSeverity.Information,
            resourceId: id);
        return Results.Ok(ApiEnvelope<object>.Success(new { deleted = id }, GetTraceId(ctx)));
    }
    return Results.NotFound(ApiEnvelope<object>.Failure($"Term '{id}' not found.", GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");
dict.MapPost("/check", (HttpContext ctx, DictionaryCheckRequest request, IDictionaryService service) =>
    Results.Ok(ApiEnvelope<DictionaryCheckResponse>.Success(service.Check(request), GetTraceId(ctx))));

// ── Dashboard Endpoint ──
app.MapGet("/api/dashboard/summary", async (HttpContext ctx, IAuditSink auditSink, int? hours) =>
{
    var windowHours = Math.Clamp(hours ?? 168, 1, 720);
    var since = DateTimeOffset.UtcNow.AddHours(-windowHours);
    var events = await auditSink.GetRecentAsync(5000, ctx.RequestAborted);
    var windowEvents = events
        .Where(e => e.Response.RecordedAtUtc >= since)
        .ToArray();

    var topCommands = windowEvents
        .Where(e => !string.IsNullOrWhiteSpace(e.Request.CommandId))
        .GroupBy(e => e.Request.CommandId!)
        .Select(g => new DashboardTopCommand { CommandId = g.Key, Count = g.Count() })
        .OrderByDescending(c => c.Count)
        .Take(10)
        .ToList();

    var byHost = windowEvents
        .GroupBy(e => e.Request.Host.ToString())
        .Select(g => new DashboardHostBucket { Host = g.Key, Count = g.Count() })
        .OrderByDescending(b => b.Count)
        .ToList();

    var activeUsers = windowEvents
        .Where(e => !string.IsNullOrWhiteSpace(e.Request.ActorId) && e.Request.ActorId != "anonymous")
        .Select(e => e.Request.ActorId!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();

    // 时间线：按天聚合
    var bucketCount = Math.Max(1, windowHours / 24);
    var bucketSpan = TimeSpan.FromHours(Math.Max(1, windowHours / bucketCount));
    var timeline = new List<DashboardTimelineBucket>();
    for (var bucketStart = since; bucketStart < DateTimeOffset.UtcNow; bucketStart = bucketStart.Add(bucketSpan))
    {
        var bucketEnd = bucketStart.Add(bucketSpan);
        timeline.Add(new DashboardTimelineBucket
        {
            Label = bucketStart.ToString("MM-dd HH:mm"),
            Count = windowEvents.Count(e => e.Response.RecordedAtUtc >= bucketStart && e.Response.RecordedAtUtc < bucketEnd)
        });
    }

    var response = new DashboardSummaryResponse
    {
        GeneratedAtUtc = DateTimeOffset.UtcNow,
        WindowHours = windowHours,
        TotalEvents = windowEvents.Length,
        ActiveUserCount = activeUsers,
        TopCommands = topCommands,
        ByHost = byHost,
        Timeline = timeline
    };

    return Results.Ok(ApiEnvelope<DashboardSummaryResponse>.Success(response, GetTraceId(ctx)));
});

// ── Version Management Endpoint ──
app.MapGet("/api/versions", (HttpContext ctx) =>
{
    var versions = new[]
    {
        new { version = "0.1.3", date = "2026-06-07", status = "current", notes = "Phase D: 146 commands, 337 tests, 16 panels" },
        new { version = "0.1.1", date = "2026-06-06", status = "previous", notes = "Initial pilot candidate" },
        new { version = "0.1.0", date = "2026-06-03", status = "previous", notes = "Phase A+B+C+D initial delivery" }
    };
    return Results.Ok(ApiEnvelope<object>.Success(new { versions, current = "0.1.3" }, GetTraceId(ctx)));
});

// ── User Groups Endpoint ──
var groupsStore = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
admin.MapGet("/groups", (HttpContext ctx) =>
{
    var result = groupsStore.Select(kvp => new { name = kvp.Key, members = kvp.Value.ToArray(), count = kvp.Value.Count });
    return Results.Ok(ApiEnvelope<object>.Success(new { groups = result }, GetTraceId(ctx)));
});
admin.MapPost("/groups", async (HttpContext ctx, IAuditSink auditSink) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
    var name = body.TryGetProperty("name", out var n) ? n.GetString() : null;
    var members = body.TryGetProperty("members", out var m) ? m.EnumerateArray().Select(e => e.GetString()!).ToArray() : Array.Empty<string>();
    if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(ApiEnvelope<object>.Failure("name is required.", GetTraceId(ctx)));

    groupsStore[name] = new HashSet<string>(members, StringComparer.OrdinalIgnoreCase);
    await RecordBackendAuditAsync(ctx, auditSink, logger, "admin.group.updated",
        AuditSeverity.Warning, resourceId: name,
        metadata: new Dictionary<string, string> { ["members"] = string.Join(",", members) });
    return Results.Ok(ApiEnvelope<object>.Success(new { name, members, count = members.Length }, GetTraceId(ctx)));
});
admin.MapDelete("/groups/{groupName}", async (HttpContext ctx, string groupName, IAuditSink auditSink) =>
{
    if (!groupsStore.TryRemove(groupName, out _))
        return Results.NotFound(ApiEnvelope<object>.Failure($"Group '{groupName}' not found.", GetTraceId(ctx)));
    await RecordBackendAuditAsync(ctx, auditSink, logger, "admin.group.deleted", AuditSeverity.Warning, resourceId: groupName);
    return Results.Ok(ApiEnvelope<object>.Success(new { deleted = groupName }, GetTraceId(ctx)));
});

// ── Audit Integrity Verification ──
admin.MapGet("/audit-events/verify", async (HttpContext ctx, IAuditSink auditSink) =>
{
    var events = await auditSink.GetRecentAsync(1000, ctx.RequestAborted);
    var ordered = events.OrderBy(e => e.Response.RecordedAtUtc).ToArray();
    int gaps = 0;
    for (int i = 1; i < ordered.Length; i++)
    {
        if (ordered[i].Response.RecordedAtUtc < ordered[i - 1].Response.RecordedAtUtc)
            gaps++;
    }
    return Results.Ok(ApiEnvelope<object>.Success(new
    {
        totalEvents = ordered.Length,
        hasGaps = gaps > 0,
        gapCount = gaps,
        oldestEvent = ordered.FirstOrDefault().Response.RecordedAtUtc,
        newestEvent = ordered.LastOrDefault().Response.RecordedAtUtc,
        status = gaps == 0 ? "intact" : "gaps_detected"
    }, GetTraceId(ctx)));
});

// ── Brand Template Endpoint ──
var brand = app.MapGroup("/api/brand");
brand.MapGet("/", async (HttpContext ctx, IConfigurationStore store) =>
{
    var template = await store.GetAsync("brand-template", ctx.RequestAborted);
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(template, GetTraceId(ctx)));
});
brand.MapPut("/", async (HttpContext ctx, IConfigurationStore store, IAuditSink auditSink, ConfigurationUpsertRequest req) =>
{
    var result = await store.UpsertAsync("brand-template", req, ctx.RequestAborted);
    await RecordBackendAuditAsync(ctx, auditSink, logger, "admin.brand.updated",
        AuditSeverity.Information, resourceId: "brand-template");
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(result, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");

// ── Clause Library Endpoint ──
var clauses = app.MapGroup("/api/clauses");
clauses.MapGet("/", async (HttpContext ctx, IConfigurationStore store) =>
{
    var lib = await store.GetAsync("clause-library", ctx.RequestAborted);
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(lib, GetTraceId(ctx)));
});
clauses.MapPut("/", async (HttpContext ctx, IConfigurationStore store, IAuditSink auditSink, ConfigurationUpsertRequest req) =>
{
    var result = await store.UpsertAsync("clause-library", req, ctx.RequestAborted);
    await RecordBackendAuditAsync(ctx, auditSink, logger, "admin.clause.updated",
        AuditSeverity.Information, resourceId: "clause-library");
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(result, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");

// ── Enterprise Policy Endpoint ──
app.MapGet("/api/policy", async (HttpContext ctx, IConfigurationStore configStore) =>
{
    var policy = await configStore.GetAsync("enterprise-policy", ctx.RequestAborted);
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(policy, GetTraceId(ctx)));
});

app.MapPut("/api/policy", async (HttpContext ctx, IConfigurationStore configStore, IAuditSink auditSink, ConfigurationUpsertRequest req) =>
{
    var result = await configStore.UpsertAsync("enterprise-policy", req, ctx.RequestAborted);
    await RecordBackendAuditAsync(ctx, auditSink, logger, "admin.policy.updated",
        AuditSeverity.Warning, resourceId: "enterprise-policy",
        metadata: new Dictionary<string, string> { ["updatedBy"] = req.UpdatedBy ?? "admin" });
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(result, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");

// ── AIWA Config Endpoint ──
app.MapGet("/api/aiwa/config", (HttpContext ctx, AiwaService aiwa) =>
{
    return Results.Ok(ApiEnvelope<object>.Success(new
    {
        provider = aiwa.Provider,
        model = aiwa.Model,
        modes = new[] { "summarize", "expand", "rewrite", "proofread", "translate", "explain" }
    }, GetTraceId(ctx)));
});

// ── AIWA Chat Endpoint ──
app.MapPost("/api/aiwa/chat", async (HttpContext ctx, AiwaChatRequest request, AiwaService aiwa) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest(ApiEnvelope<object>.Failure("message is required.", GetTraceId(ctx)));

    var mode = string.IsNullOrWhiteSpace(request.Mode) ? "chat" : request.Mode.Trim().ToLowerInvariant();
    try
    {
        var response = await aiwa.ChatAsync(request.Message, mode, ctx.RequestAborted);
        return Results.Ok(ApiEnvelope<AiwaChatResponse>.Success(new AiwaChatResponse
        {
            Response = response,
            Mode = mode,
            Model = aiwa.Model,
            FallbackMock = aiwa.Provider == "mock"
        }, GetTraceId(ctx)));
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "AIWA API call failed, falling back to mock mode");
        var mockResponse = GetMockResponse(request.Message, mode);
        return Results.Ok(ApiEnvelope<AiwaChatResponse>.Success(new AiwaChatResponse
        {
            Response = mockResponse,
            Mode = mode,
            Model = "mock-fallback",
            FallbackMock = true
        }, GetTraceId(ctx)));
    }
});

app.Run();

static string GetTraceId(HttpContext context) =>
    context.Items.TryGetValue("TraceId", out var traceId) && traceId is string value
        ? value
        : Guid.NewGuid().ToString("N");

static async Task RecordBackendAuditAsync(
    HttpContext context,
    IAuditSink auditSink,
    ILogger logger,
    string eventType,
    AuditSeverity severity,
    string? actorId = null,
    string? resourceId = null,
    string? commandId = null,
    Dictionary<string, string>? metadata = null)
{
    try
    {
        await auditSink.RecordAsync(new AuditEventRequest
        {
            EventType = eventType,
            ActorId = string.IsNullOrWhiteSpace(actorId) ? GetActorId(context) : actorId,
            Host = OfficeHost.Web,
            Severity = severity,
            CommandId = commandId,
            ResourceId = resourceId,
            Metadata = metadata ?? new Dictionary<string, string>()
        }, context.RequestAborted);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Backend audit event record failed: {EventType}", eventType);
    }
}

static string GetActorId(HttpContext context) =>
    context.User.FindFirstValue("sub") ??
    context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
    context.User.Identity?.Name ??
    "anonymous";

static async Task<DatabaseDiagnostics> GetDatabaseStatusAsync(HttpContext context, string provider)
{
    try
    {
        if (provider != "inmemory")
        {
            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ModelForgeDbContext>();
            var canConnect = await db.Database.CanConnectAsync(context.RequestAborted);
            return new DatabaseDiagnostics(provider, canConnect, null);
        }

        return new DatabaseDiagnostics("inmemory", true, null);
    }
    catch (Exception ex)
    {
        return new DatabaseDiagnostics(provider, false, ex.Message);
    }
}

static async Task<AdminDiagnosticsResponse> BuildAdminDiagnosticsAsync(
    HttpContext context,
    string provider,
    ICommandCatalog commandCatalog,
    ILinkMetadataStore linkStore,
    IDictionaryService dictionaryService,
    IConfigurationStore configurationStore,
    IConfiguration appConfiguration,
    IAuditSink auditSink)
{
    var database = await GetDatabaseStatusAsync(context, provider);
    var links = await linkStore.GetAllAsync(context.RequestAborted);
    var configuration = await configurationStore.GetAsync("default", context.RequestAborted);
    var auditEvents = await auditSink.GetRecentAsync(100, context.RequestAborted);
    var auditRetentionDays = GetAuditRetentionDays(configuration.Values, appConfiguration);
    var auditRetentionCutoffUtc = DateTimeOffset.UtcNow.AddDays(-auditRetentionDays);
    var auditEventsEligibleForRetentionPrune = await auditSink.CountBeforeAsync(
        auditRetentionCutoffUtc,
        context.RequestAborted);

    return new AdminDiagnosticsResponse
    {
        Version = new VersionInfoResponse(),
        DatabaseProvider = provider,
        DatabaseConnected = database.Connected,
        CommandCount = commandCatalog.GetAll().Count,
        LinkCount = links.Count,
        DictionaryTermCount = dictionaryService.GetAll().Count,
        RecentAuditEventCount = auditEvents.Count,
        AuditRetentionDays = auditRetentionDays,
        AuditRetentionCutoffUtc = auditRetentionCutoffUtc,
        AuditEventsEligibleForRetentionPrune = auditEventsEligibleForRetentionPrune,
        Configuration = RedactConfiguration(configuration.Values),
        Notes = BuildDiagnosticsNotes(database, provider)
    };
}

static Dictionary<string, string> BuildRuntimeDiagnostics() =>
    new()
    {
        ["machineName"] = Environment.MachineName,
        ["osVersion"] = Environment.OSVersion.VersionString,
        ["processArchitecture"] = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
        ["frameworkDescription"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        ["workingSetMb"] = (Environment.WorkingSet / 1024d / 1024d).ToString("F1"),
        ["processorCount"] = Environment.ProcessorCount.ToString()
    };

static IReadOnlyCollection<string> BuildDiagnosticsNotes(DatabaseDiagnostics database, string provider)
{
    var notes = new List<string>();
    if (provider == "inmemory")
        notes.Add("Using in-memory storage; data is not durable across restarts.");
    if (!database.Connected)
        notes.Add("Database connectivity check failed.");
    notes.Add("Diagnostics intentionally exclude secrets, authentication tokens, and workbook contents.");
    return notes;
}

static async Task<int> GetAuditRetentionDaysAsync(
    IConfigurationStore configurationStore,
    IConfiguration appConfiguration,
    CancellationToken cancellationToken)
{
    var configuration = await configurationStore.GetAsync("default", cancellationToken);
    return GetAuditRetentionDays(configuration.Values, appConfiguration);
}

static int GetAuditRetentionDays(Dictionary<string, string> configuration, IConfiguration appConfiguration)
{
    if (configuration.TryGetValue("AuditRetentionDays", out var value) &&
        int.TryParse(value, out var retentionDays))
    {
        return Math.Clamp(retentionDays, 1, 3650);
    }

    var configuredRetentionDays = appConfiguration.GetValue<int?>("AuditRetentionDays");
    if (configuredRetentionDays.HasValue)
        return Math.Clamp(configuredRetentionDays.Value, 1, 3650);

    return 90;
}

static Dictionary<string, string> RedactConfiguration(Dictionary<string, string> values) =>
    values.ToDictionary(item => item.Key, item => ShouldRedactConfigurationValue(item.Key) ? "[REDACTED]" : item.Value);

static bool ShouldRedactConfigurationValue(string key)
{
    var normalized = key
        .Replace("_", string.Empty, StringComparison.Ordinal)
        .Replace("-", string.Empty, StringComparison.Ordinal)
        .Replace(":", string.Empty, StringComparison.Ordinal)
        .ToLowerInvariant();
    var sensitiveMarkers = new[]
    {
        "apikey",
        "secret",
        "token",
        "password",
        "credential",
        "connectionstring",
        "jwt",
        "privatekey"
    };
    return sensitiveMarkers.Any(normalized.Contains);
}

static bool IsValidServiceToken(HttpContext context, string configuredToken)
{
    if (string.IsNullOrWhiteSpace(configuredToken))
        return false;

    var providedToken = context.Request.Headers["X-Service-Token"].FirstOrDefault();
    return !string.IsNullOrWhiteSpace(providedToken) &&
           string.Equals(providedToken, configuredToken, StringComparison.Ordinal);
}

static AdminAuditEventItem ToAdminAuditEventItem((AuditEventRequest Request, AuditEventResponse Response) entry) =>
    new()
    {
        EventId = entry.Response.EventId,
        EventType = entry.Request.EventType,
        ActorId = entry.Request.ActorId,
        Host = entry.Request.Host,
        Severity = entry.Request.Severity,
        CommandId = entry.Request.CommandId,
        ResourceId = entry.Request.ResourceId,
        RecordedAtUtc = entry.Response.RecordedAtUtc
    };

static AdminUserResponse ToAdminUserResponse(UserIdentity user) =>
    new()
    {
        Id = user.Id,
        Username = user.Username,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

static string BuildAuditEventsCsv(IReadOnlyList<(AuditEventRequest Request, AuditEventResponse Response)> events)
{
    var builder = new StringBuilder();
    builder.AppendLine("eventId,recordedAtUtc,eventType,actorId,host,severity,commandId,resourceId");
    foreach (var entry in events)
    {
        builder.AppendJoin(',', new[]
        {
            CsvEscape(entry.Response.EventId),
            CsvEscape(entry.Response.RecordedAtUtc.ToString("O")),
            CsvEscape(entry.Request.EventType),
            CsvEscape(entry.Request.ActorId),
            CsvEscape(entry.Request.Host.ToString()),
            CsvEscape(entry.Request.Severity.ToString()),
            CsvEscape(entry.Request.CommandId ?? string.Empty),
            CsvEscape(entry.Request.ResourceId ?? string.Empty)
        });
        builder.AppendLine();
    }

    return builder.ToString();
}

static string? NormalizeFilter(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

static int GetAuditFetchCount(AdminAuditEventsQuery query, int maxCount)
{
    if (HasAuditFilter(query))
        return maxCount;

    var requestedCount = Math.Max(query.Count ?? 0, (query.Page ?? 1) * (query.PageSize ?? 0));
    return Math.Clamp(requestedCount <= 0 ? maxCount : requestedCount, 1, maxCount);
}

static bool HasAuditFilter(AdminAuditEventsQuery query) =>
    !string.IsNullOrWhiteSpace(query.EventType) ||
    !string.IsNullOrWhiteSpace(query.ActorId) ||
    !string.IsNullOrWhiteSpace(query.CommandId) ||
    !string.IsNullOrWhiteSpace(query.ResourceId) ||
    !string.IsNullOrWhiteSpace(query.Search) ||
    query.Host.HasValue ||
    query.Severity.HasValue ||
    query.SinceUtc.HasValue ||
    query.UntilUtc.HasValue;

static IEnumerable<(AuditEventRequest Request, AuditEventResponse Response)> ApplyAuditEventFilters(
    IEnumerable<(AuditEventRequest Request, AuditEventResponse Response)> events,
    AdminAuditEventsQuery query) =>
    events.Where(item => AuditEventMatches(item, query));

static bool AuditEventMatches(
    (AuditEventRequest Request, AuditEventResponse Response) item,
    AdminAuditEventsQuery query)
{
    if (query.SinceUtc.HasValue && item.Response.RecordedAtUtc < query.SinceUtc.Value)
        return false;
    if (query.UntilUtc.HasValue && item.Response.RecordedAtUtc > query.UntilUtc.Value)
        return false;
    if (!TextEquals(query.EventType, item.Request.EventType))
        return false;
    if (!TextEquals(query.ActorId, item.Request.ActorId))
        return false;
    if (!TextEquals(query.CommandId, item.Request.CommandId))
        return false;
    if (!TextEquals(query.ResourceId, item.Request.ResourceId))
        return false;
    if (query.Host.HasValue && item.Request.Host != query.Host.Value)
        return false;
    if (query.Severity.HasValue && item.Request.Severity != query.Severity.Value)
        return false;
    return string.IsNullOrWhiteSpace(query.Search) || AuditEventContains(item, query.Search);
}

static bool TextEquals(string? expected, string? actual) =>
    string.IsNullOrWhiteSpace(expected) ||
    string.Equals(expected.Trim(), actual ?? string.Empty, StringComparison.OrdinalIgnoreCase);

static bool AuditEventContains(
    (AuditEventRequest Request, AuditEventResponse Response) item,
    string search)
{
    var candidates = new[]
    {
        item.Response.EventId,
        item.Request.EventType,
        item.Request.ActorId,
        item.Request.Host.ToString(),
        item.Request.Severity.ToString(),
        item.Request.CommandId,
        item.Request.ResourceId
    };

    return candidates.Any(value =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(search, StringComparison.OrdinalIgnoreCase));
}

static IReadOnlyCollection<AdminAuditSummaryBucket> BuildAuditSummaryBuckets(
    IEnumerable<(AuditEventRequest Request, AuditEventResponse Response)> events,
    Func<(AuditEventRequest Request, AuditEventResponse Response), string?> keySelector)
{
    return events
        .Select(item => keySelector(item))
        .Select(key => string.IsNullOrWhiteSpace(key) ? "unknown" : key!)
        .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
        .Select(group => new AdminAuditSummaryBucket { Key = group.Key, Count = group.Count() })
        .OrderByDescending(bucket => bucket.Count)
        .ThenBy(bucket => bucket.Key, StringComparer.OrdinalIgnoreCase)
        .Take(10)
        .ToArray();
}

static int GetAuditTimelineBucketHours(int windowHours) =>
    windowHours <= 24 ? 1 :
    windowHours <= 168 ? 24 :
    24 * 7;

static IReadOnlyCollection<AdminAuditTimelineBucket> BuildAuditTimelineBuckets(
    IReadOnlyCollection<(AuditEventRequest Request, AuditEventResponse Response)> events,
    DateTimeOffset startUtc,
    DateTimeOffset endUtc,
    int bucketHours)
{
    var bucketSpan = TimeSpan.FromHours(bucketHours);
    var buckets = new List<AdminAuditTimelineBucket>();
    for (var bucketStart = startUtc; bucketStart < endUtc; bucketStart = bucketStart.Add(bucketSpan))
    {
        var bucketEnd = bucketStart.Add(bucketSpan);
        buckets.Add(new AdminAuditTimelineBucket
        {
            StartUtc = bucketStart,
            EndUtc = bucketEnd > endUtc ? endUtc : bucketEnd,
            Count = events.Count(item =>
                item.Response.RecordedAtUtc >= bucketStart &&
                item.Response.RecordedAtUtc < bucketEnd)
        });
    }

    return buckets;
}

static IReadOnlyCollection<AdminAuditHeatmapCell> BuildAuditHeatmap(
    IReadOnlyCollection<(AuditEventRequest Request, AuditEventResponse Response)> events)
{
    var topRows = events
        .Select(item => string.IsNullOrWhiteSpace(item.Request.EventType) ? "unknown" : item.Request.EventType)
        .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .Take(8)
        .Select(group => group.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    return events
        .Select(item => new
        {
            RowKey = string.IsNullOrWhiteSpace(item.Request.EventType) ? "unknown" : item.Request.EventType,
            ColumnKey = item.Request.Host.ToString()
        })
        .Where(item => topRows.Contains(item.RowKey))
        .GroupBy(item => new { item.RowKey, item.ColumnKey })
        .Select(group => new AdminAuditHeatmapCell
        {
            RowKey = group.Key.RowKey,
            ColumnKey = group.Key.ColumnKey,
            Count = group.Count()
        })
        .OrderBy(cell => cell.RowKey, StringComparer.OrdinalIgnoreCase)
        .ThenBy(cell => cell.ColumnKey, StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static string CsvEscape(string value)
{
    if (value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    return value;
}

static string GetMockResponse(string message, string mode) => mode switch
{
    "summarize" => $"【Mock 摘要】已分析 {message.Length} 个字符的输入内容。核心要点：1) 财务数据概况，2) 关键指标趋势，3) 风险提示。（Ollama 不可达，当前为 Mock 响应。）",
    "expand" => $"【Mock 展开】输入内容深度分析：从行业对标、历史趋势和可比交易三个维度展开。原文前50字: {message[..Math.Min(message.Length, 50)]}。（Ollama 不可达，当前为 Mock 响应。）",
    "rewrite" => $"【Mock 改写】原文已润色为正式投资银行风格：{message}（Ollama 不可达，当前为 Mock 响应。）",
    "proofread" => $"【Mock 校对】已检查 {message.Split(' ').Length} 个词。未发现明显语法错误。建议：1) 检查数字格式一致性，2) 确认专有名词拼写。（Ollama 不可达，当前为 Mock 响应。）",
    "translate" => $"【Mock 翻译】原文翻译结果：{message}（Ollama 不可达，当前为 Mock 响应。）",
    _ => $"【Mock 响应】已收到你的消息（{message.Length} 字符）。当前 AI 后端 (Ollama) 不可达，返回 Mock 响应。请确保 Ollama 正在运行或设置 AIWA:OllamaUrl 配置。"
};

internal sealed record DatabaseDiagnostics(string Provider, bool Connected, string? Error);
