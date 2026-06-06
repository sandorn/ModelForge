using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using ModelForge.Backend.Auth;
using ModelForge.Backend.Data;
using ModelForge.Backend.Services;
using ModelForge.Contracts;

var builder = WebApplication.CreateBuilder(args);
var logger = LoggerFactory.Create(c => c.AddConsole()).CreateLogger("ModelForge.Backend");

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

auth.MapPost("/login", (HttpContext ctx, LoginRequest request, JwtService jwt, InMemoryUserStore store) =>
{
    var user = store.ValidateCredentials(request.Username, request.Password);
    if (user == null)
        return Results.Unauthorized();

    var token = jwt.IssueToken(user.Id, user.Username, user.Role);
    var loginResponse = new LoginResponse
    {
        Token = token,
        UserId = user.Id,
        Username = user.Username,
        Role = user.Role,
        ExpiresAt = DateTime.UtcNow.AddHours(jwtOptions.TokenLifetimeHours)
    };
    return Results.Ok(ApiEnvelope<LoginResponse>.Success(loginResponse, GetTraceId(ctx)));
});

auth.MapGet("/me", (HttpContext ctx, ClaimsPrincipal user, InMemoryUserStore store) =>
{
    var userId = user.FindFirstValue("sub");
    if (userId == null) return Results.Unauthorized();
    var identity = store.GetById(userId);
    if (identity == null) return Results.NotFound();
    return Results.Ok(ApiEnvelope<object>.Success(new { identity.Id, identity.Username, identity.Role, identity.IsActive }, GetTraceId(ctx)));
}).RequireAuthorization();

// ── Admin Endpoints ──
var admin = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly");
admin.MapGet("/users", (InMemoryUserStore store) =>
    Results.Ok(store.GetAll().Select(u => new { u.Id, u.Username, u.Role, u.IsActive, u.CreatedAt })));
admin.MapPost("/users", (LoginRequest req, InMemoryUserStore store) =>
{
    var user = store.AddUser(req.Username, req.Password, "Analyst");
    return Results.Created($"/api/admin/users/{user.Id}", new { user.Id, user.Username, user.Role });
});
admin.MapPut("/users/{userId}/toggle", (string userId, InMemoryUserStore store) =>
{
    var user = store.GetById(userId);
    if (user is null) return Results.NotFound();
    store.SetActive(userId, !user.IsActive);
    return Results.Ok(new { userId, active = !user.IsActive });
});
admin.MapGet("/audit-events", async (HttpContext ctx, IAuditSink auditSink, int? count, int? page, int? pageSize) =>
{
    var effectiveCount = Math.Clamp(count ?? 50, 1, 500);
    var events = await auditSink.GetRecentAsync(effectiveCount, ctx.RequestAborted);
    var result = events.Select(e => new
    {
        e.Response.EventId,
        e.Request.EventType,
        e.Request.ActorId,
        Host = e.Request.Host,
        Severity = e.Request.Severity,
        e.Request.CommandId,
        e.Request.ResourceId,
        e.Response.RecordedAtUtc
    });
    var pagination = new { page = page ?? 1, pageSize = pageSize ?? 50, total = result.Count() };
    return Results.Ok(ApiEnvelope<object>.Success(new { items = result, pagination }, GetTraceId(ctx)));
});

// ── Public Endpoints ──
app.MapGet("/health", async (HttpContext ctx) =>
{
    var health = new HealthResponse();
    object? dbStatus = null;
    try
    {
        if (provider != "inmemory")
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ModelForgeDbContext>();
            var canConnect = await db.Database.CanConnectAsync();
            dbStatus = new { provider, connected = canConnect };
        }
        else
        {
            dbStatus = new { provider = "inmemory", connected = true };
        }
    }
    catch (Exception ex)
    {
        dbStatus = new { provider, connected = false, error = ex.Message };
    }
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
        return Results.BadRequest(new { error = "commandId is required." });
    var response = await dispatcher.DispatchAsync(request, ctx.RequestAborted);
    return Results.Accepted($"/api/commands/dispatch/{response.DispatchId}",
        ApiEnvelope<CommandDispatchResponse>.Success(response, GetTraceId(ctx)));
});
api.MapGet("/config/{scope}", async (HttpContext ctx, IConfigurationStore store, string scope) =>
{
    var response = await store.GetAsync(scope, ctx.RequestAborted);
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(response, GetTraceId(ctx)));
});
api.MapPut("/config/{scope}", async (HttpContext ctx, IConfigurationStore store, string scope, ConfigurationUpsertRequest request) =>
{
    var response = await store.UpsertAsync(scope, request, ctx.RequestAborted);
    return Results.Ok(ApiEnvelope<ConfigurationResponse>.Success(response, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");
api.MapPost("/audit-events", async (HttpContext ctx, IAuditSink auditSink, AuditEventRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.EventType))
        return Results.BadRequest(new { error = "eventType is required." });
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
        return Results.BadRequest(new { error = "sourceDocumentId, sourceAddress, targetDocumentId, and targetAddress are required." });
    var response = await store.CreateAsync(request, ctx.RequestAborted);
    return Results.Created($"/api/links/{response.LinkId}",
        ApiEnvelope<LinkMetadata>.Success(response, GetTraceId(ctx)));
});
api.MapPost("/links/{linkId}/refresh", async (HttpContext ctx, ILinkMetadataStore store, string linkId, LinkRefreshRequest request) =>
{
    if (string.IsNullOrWhiteSpace(linkId))
        return Results.BadRequest(new { error = "linkId is required." });
    request.LinkId = linkId;
    var response = await store.MarkRefreshRequestedAsync(request, ctx.RequestAborted);
    return Results.Accepted($"/api/links/{linkId}",
        ApiEnvelope<LinkRefreshResponse>.Success(response, GetTraceId(ctx)));
});

// ── Corporate Dictionary ──
var dict = app.MapGroup("/api/dictionary");
dict.MapGet("/", (HttpContext ctx, IDictionaryService service) =>
    Results.Ok(ApiEnvelope<object>.Success(service.GetAll(), GetTraceId(ctx))));
dict.MapPost("/", (HttpContext ctx, DictionaryTerm term, IDictionaryService service) =>
{
    if (string.IsNullOrWhiteSpace(term.Id) || string.IsNullOrWhiteSpace(term.Term))
        return Results.BadRequest(new { error = "id and preferred are required." });
    var created = service.AddOrUpdate(term);
    return Results.Created($"/api/dictionary/{created.Id}",
        ApiEnvelope<object>.Success(created, GetTraceId(ctx)));
}).RequireAuthorization("AdminOnly");
dict.MapDelete("/{id}", (HttpContext ctx, string id, IDictionaryService service) =>
{
    if (service.Delete(id))
        return Results.Ok(ApiEnvelope<object>.Success(new { deleted = id }, GetTraceId(ctx)));
    return Results.NotFound(new { error = $"Term '{id}' not found." });
}).RequireAuthorization("AdminOnly");
dict.MapPost("/check", (HttpContext ctx, DictionaryCheckRequest request, IDictionaryService service) =>
    Results.Ok(ApiEnvelope<object>.Success(service.Check(request), GetTraceId(ctx))));

app.Run();

static string GetTraceId(HttpContext context) =>
    context.Items.TryGetValue("TraceId", out var traceId) && traceId is string value
        ? value
        : Guid.NewGuid().ToString("N");
