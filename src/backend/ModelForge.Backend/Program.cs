using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using ModelForge.Backend.Auth;
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

builder.Services.AddSingleton<IConfigurationStore, InMemoryConfigurationStore>();
builder.Services.AddSingleton<ICommandCatalog, CommandCatalog>();
builder.Services.AddSingleton<ICommandDispatcher, InMemoryCommandDispatcher>();
builder.Services.AddSingleton<IAuditSink, InMemoryAuditSink>();
builder.Services.AddSingleton<ILinkMetadataStore, InMemoryLinkMetadataStore>();
builder.Services.AddSingleton<IDictionaryService, InMemoryDictionaryService>();

var app = builder.Build();

app.UseCors("DevelopmentAddIn");
app.UseAuthentication();
app.UseAuthorization();

// TraceId middleware
app.Use(async (context, next) =>
{
    var traceId = context.Request.Headers.TryGetValue("X-Trace-Id", out var incoming)
        ? incoming.ToString()
        : Guid.NewGuid().ToString("N");
    context.Items["TraceId"] = traceId;
    context.Response.Headers["X-Trace-Id"] = traceId;
    await next();
});

// Global exception handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "未处理异常: {Path}", context.Request.Path);
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var envelope = ApiEnvelope<object>.Failure("内部服务器错误。", GetTraceId(context));
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

    return Results.Ok(ApiEnvelope<object>.Success(new
    {
        identity.Id,
        identity.Username,
        identity.Role,
        identity.IsActive
    }, GetTraceId(ctx)));
}).RequireAuthorization();

// ── Admin Endpoints (RBAC-protected) ──
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
    if (user is null)
        return Results.NotFound();
    var newState = !user.IsActive;
    store.SetActive(userId, newState);
    return Results.Ok(new { userId, active = newState });
});

// ── Public Endpoints ──
app.MapGet("/health", (HttpContext ctx) =>
    Results.Ok(ApiEnvelope<HealthResponse>.Success(new HealthResponse(), GetTraceId(ctx))));

app.MapGet("/api/version", (HttpContext ctx) =>
    Results.Ok(ApiEnvelope<VersionInfoResponse>.Success(new VersionInfoResponse(), GetTraceId(ctx))));

var api = app.MapGroup("/api");

api.MapGet("/commands", (HttpContext ctx, ICommandCatalog catalog) =>
    Results.Ok(ApiEnvelope<IReadOnlyCollection<CommandDefinition>>.Success(catalog.GetAll(), GetTraceId(ctx))));

api.MapPost("/commands/dispatch", async (HttpContext ctx, ICommandDispatcher dispatcher, CommandDispatchRequest request) =>
{
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
    var response = await store.CreateAsync(request, ctx.RequestAborted);
    return Results.Created($"/api/links/{response.LinkId}",
        ApiEnvelope<LinkMetadata>.Success(response, GetTraceId(ctx)));
});

api.MapPost("/links/{linkId}/refresh", async (HttpContext ctx, ILinkMetadataStore store, string linkId, LinkRefreshRequest request) =>
{
    request.LinkId = linkId;
    var response = await store.MarkRefreshRequestedAsync(request, ctx.RequestAborted);
    return Results.Accepted($"/api/links/{linkId}",
        ApiEnvelope<LinkRefreshResponse>.Success(response, GetTraceId(ctx)));
});

// ── Corporate Dictionary ──
var dict = app.MapGroup("/api/dictionary");

dict.MapGet("/", (IDictionaryService service) =>
    Results.Ok(service.GetAll()));

dict.MapPost("/", (DictionaryTerm term, IDictionaryService service) =>
{
    var created = service.AddOrUpdate(term);
    return Results.Created($"/api/dictionary/{created.Id}", created);
}).RequireAuthorization("AdminOnly");

dict.MapDelete("/{id}", (string id, IDictionaryService service) =>
{
    if (service.Delete(id)) return Results.NoContent();
    return Results.NotFound();
}).RequireAuthorization("AdminOnly");

dict.MapPost("/check", (DictionaryCheckRequest request, IDictionaryService service) =>
    Results.Ok(service.Check(request)));

app.Run();

static string GetTraceId(HttpContext context) =>
    context.Items.TryGetValue("TraceId", out var traceId) && traceId is string value
        ? value
        : Guid.NewGuid().ToString("N");
