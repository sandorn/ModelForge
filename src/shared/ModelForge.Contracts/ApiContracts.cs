using System;
using System.Collections.Generic;

namespace ModelForge.Contracts
{
    // ═══════════════════════════════════════════════════════════════
    //  Unified API Envelope
    // ═══════════════════════════════════════════════════════════════

    public sealed class ApiEnvelope<T>
    {
        public string TraceId { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Error { get; set; }

        public static ApiEnvelope<T> Success(T data, string traceId) =>
            new() { TraceId = traceId, Data = data };

        public static ApiEnvelope<T> Failure(string error, string traceId) =>
            new() { TraceId = traceId, Error = error };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Health & Version
    // ═══════════════════════════════════════════════════════════════

    public sealed class HealthResponse
    {
        public string Status { get; set; } = "Healthy";
        public string Service { get; set; } = "ModelForge.Backend";
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class VersionInfoResponse
    {
        public string Product { get; set; } = "ModelForge";
        public string Component { get; set; } = "Backend API";
        public string Version { get; set; } = "0.1.3";
        public string ApiVersion { get; set; } = "v1";
        public DateTimeOffset BuildTimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Authentication & Admin
    // ═══════════════════════════════════════════════════════════════

    public sealed class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public sealed class AdminUserCreateRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Analyst";
    }

    public sealed class AdminUserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class AdminUserToggleResponse
    {
        public string UserId { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    public sealed class AdminRolePermissionResponse
    {
        public string Role { get; set; } = string.Empty;
        public IReadOnlyCollection<string> Permissions { get; set; } = Array.Empty<string>();
        public bool BuiltIn { get; set; } = true;
    }

    public sealed class AdminRolesResponse
    {
        public IReadOnlyCollection<AdminRolePermissionResponse> Roles { get; set; } = Array.Empty<AdminRolePermissionResponse>();
    }

    public sealed class AdminAuditEventItem
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string ActorId { get; set; } = string.Empty;
        public OfficeHost Host { get; set; }
        public AuditSeverity Severity { get; set; }
        public string? CommandId { get; set; }
        public string? ResourceId { get; set; }
        public DateTimeOffset RecordedAtUtc { get; set; }
    }

    public sealed class PaginationResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }

    public sealed class AdminAuditEventsResponse
    {
        public IReadOnlyCollection<AdminAuditEventItem> Items { get; set; } = Array.Empty<AdminAuditEventItem>();
        public PaginationResponse Pagination { get; set; } = new();
        public AdminAuditEventsQuery Query { get; set; } = new();
    }

    public sealed class AdminAuditEventsQuery
    {
        public int? Count { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? EventType { get; set; }
        public string? ActorId { get; set; }
        public OfficeHost? Host { get; set; }
        public AuditSeverity? Severity { get; set; }
        public string? CommandId { get; set; }
        public string? ResourceId { get; set; }
        public string? Search { get; set; }
        public DateTimeOffset? SinceUtc { get; set; }
        public DateTimeOffset? UntilUtc { get; set; }
    }

    public sealed class AdminAuditSummaryBucket
    {
        public string Key { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class AdminAuditTimelineBucket
    {
        public DateTimeOffset StartUtc { get; set; }
        public DateTimeOffset EndUtc { get; set; }
        public int Count { get; set; }
    }

    public sealed class AdminAuditHeatmapCell
    {
        public string RowKey { get; set; } = string.Empty;
        public string ColumnKey { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class AdminAuditSummaryResponse
    {
        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public int WindowHours { get; set; }
        public int BucketHours { get; set; }
        public int TotalEvents { get; set; }
        public IReadOnlyCollection<AdminAuditSummaryBucket> ByEventType { get; set; } = Array.Empty<AdminAuditSummaryBucket>();
        public IReadOnlyCollection<AdminAuditSummaryBucket> ByHost { get; set; } = Array.Empty<AdminAuditSummaryBucket>();
        public IReadOnlyCollection<AdminAuditSummaryBucket> ByActor { get; set; } = Array.Empty<AdminAuditSummaryBucket>();
        public IReadOnlyCollection<AdminAuditTimelineBucket> Timeline { get; set; } = Array.Empty<AdminAuditTimelineBucket>();
        public IReadOnlyCollection<AdminAuditHeatmapCell> Heatmap { get; set; } = Array.Empty<AdminAuditHeatmapCell>();
        public AdminAuditEventsQuery Query { get; set; } = new();
    }

    public sealed class AdminAuditRetentionRequest
    {
        public int? RetentionDays { get; set; }
        public bool DryRun { get; set; } = true;
    }

    public sealed class AdminAuditRetentionResponse
    {
        public int RetentionDays { get; set; }
        public DateTimeOffset CutoffUtc { get; set; }
        public int MatchedEvents { get; set; }
        public int DeletedEvents { get; set; }
        public bool DryRun { get; set; }
        public DateTimeOffset ExecutedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class AdminDiagnosticsResponse
    {
        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public VersionInfoResponse Version { get; set; } = new();
        public string DatabaseProvider { get; set; } = "inmemory";
        public bool DatabaseConnected { get; set; } = true;
        public int CommandCount { get; set; }
        public int LinkCount { get; set; }
        public int DictionaryTermCount { get; set; }
        public int RecentAuditEventCount { get; set; }
        public int AuditRetentionDays { get; set; }
        public DateTimeOffset AuditRetentionCutoffUtc { get; set; }
        public int AuditEventsEligibleForRetentionPrune { get; set; }
        public Dictionary<string, string> Configuration { get; set; } = new();
        public IReadOnlyCollection<string> Notes { get; set; } = Array.Empty<string>();
    }

    public sealed class AdminDiagnosticsBundleResponse
    {
        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public AdminDiagnosticsResponse Summary { get; set; } = new();
        public Dictionary<string, string> Runtime { get; set; } = new();
        public IReadOnlyCollection<AdminAuditEventItem> RecentAuditEvents { get; set; } = Array.Empty<AdminAuditEventItem>();
        public IReadOnlyCollection<string> Notes { get; set; } = Array.Empty<string>();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Configuration
    // ═══════════════════════════════════════════════════════════════

    public sealed class ConfigurationResponse
    {
        public string Scope { get; set; } = "default";
        public Dictionary<string, string> Values { get; set; } = new();
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class ConfigurationUpsertRequest
    {
        public Dictionary<string, string> Values { get; set; } = new();
        public string? UpdatedBy { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Command Catalog & Dispatch
    // ═══════════════════════════════════════════════════════════════

    public sealed class CommandDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public OfficeHost Host { get; set; }
        public CommandExecutionTarget Target { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? DefaultShortcut { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public sealed class CommandDispatchRequest
    {
        public string CommandId { get; set; } = string.Empty;
        public OfficeHost Host { get; set; }
        public string? UserId { get; set; }
        public string? WorkbookId { get; set; }
        public Dictionary<string, string> Arguments { get; set; } = new();
    }

    public sealed class CommandDispatchResponse
    {
        public string DispatchId { get; set; } = string.Empty;
        public string CommandId { get; set; } = string.Empty;
        public CommandStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset AcceptedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sidecar Execution (shared DTO for Sidecar ↔ Web Add-in)
    // ═══════════════════════════════════════════════════════════════

    public sealed class SidecarExecuteRequest
    {
        public string CommandId { get; set; } = string.Empty;
        public string Host { get; set; } = "excel";
        public Dictionary<string, string>? Arguments { get; set; }
    }

    public sealed class SidecarExecuteResponse
    {
        public bool Success { get; set; }
        public string CommandId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Result { get; set; }
    }

    public sealed class SidecarStatusResponse
    {
        public bool Connected { get; set; }
        public string? Workbook { get; set; }
        public string? Worksheet { get; set; }
        public string? Selection { get; set; }
        public string? Version { get; set; }
        public string? Error { get; set; }
    }

    public sealed class ShortcutItem
    {
        public string CommandId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
    }

    public sealed class ShortcutExportResponse
    {
        public IReadOnlyCollection<ShortcutItem> Shortcuts { get; set; } = Array.Empty<ShortcutItem>();
        public int Count => Shortcuts.Count;
        public DateTimeOffset ExportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class ShortcutImportRequest
    {
        public List<ShortcutItem> Shortcuts { get; set; } = new();
    }

    public sealed class ShortcutImportResponse
    {
        public int Imported { get; set; }
        public IReadOnlyCollection<ShortcutItem> Shortcuts { get; set; } = Array.Empty<ShortcutItem>();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Audit
    // ═══════════════════════════════════════════════════════════════

    public sealed class AuditEventRequest
    {
        public string EventType { get; set; } = string.Empty;
        public string ActorId { get; set; } = "anonymous";
        public OfficeHost Host { get; set; }
        public AuditSeverity Severity { get; set; } = AuditSeverity.Information;
        public string? CommandId { get; set; }
        public string? ResourceId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public sealed class AuditEventResponse
    {
        public string EventId { get; set; } = string.Empty;
        public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public bool Recorded { get; set; } = true;
        public string? Message { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Corporate Dictionary
    // ═══════════════════════════════════════════════════════════════

    public sealed class DictionaryTerm
    {
        public string Id { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string? Replacement { get; set; }
        public string? RegexPattern { get; set; }
        public string Category { get; set; } = "General";
        public string Severity { get; set; } = "Warning";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public sealed class TermMatch
    {
        public string TermId { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string MatchedText { get; set; } = string.Empty;
        public int Position { get; set; }
        public string? Suggestion { get; set; }
    }

    public sealed class DictionaryCheckRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? Language { get; set; } = "zh-CN";
    }

    public sealed class DictionaryCheckResponse
    {
        public string OriginalText { get; set; } = string.Empty;
        public List<TermMatch> Matches { get; set; } = new();
        public int MatchCount => Matches.Count;
        public string? CleanedText { get; set; }
    }

    public sealed class DictionaryImportRequest
    {
        public List<DictionaryTerm> Terms { get; set; } = new();
        public bool Overwrite { get; set; } = true;
    }

    public sealed class DictionaryImportError
    {
        public int Index { get; set; }
        public string? Term { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    public sealed class DictionaryImportResponse
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<DictionaryImportError> Errors { get; set; } = new();
        public IReadOnlyCollection<DictionaryTerm> Terms { get; set; } = Array.Empty<DictionaryTerm>();
    }

    public sealed class DictionaryExportResponse
    {
        public IReadOnlyCollection<DictionaryTerm> Terms { get; set; } = Array.Empty<DictionaryTerm>();
        public int Count => Terms.Count;
        public DateTimeOffset ExportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Link Metadata
    // ═══════════════════════════════════════════════════════════════

    public sealed class LinkMetadata
    {
        public string LinkId { get; set; } = string.Empty;
        public LinkSourceType SourceType { get; set; }
        public string SourceDocumentId { get; set; } = string.Empty;
        public string SourceAddress { get; set; } = string.Empty;
        public LinkTargetType TargetType { get; set; }
        public string TargetDocumentId { get; set; } = string.Empty;
        public string TargetAddress { get; set; } = string.Empty;
        public string RefreshPolicy { get; set; } = "manual";
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastRefreshedAtUtc { get; set; }
    }

    public sealed class CreateLinkMetadataRequest
    {
        public LinkSourceType SourceType { get; set; }
        public string SourceDocumentId { get; set; } = string.Empty;
        public string SourceAddress { get; set; } = string.Empty;
        public LinkTargetType TargetType { get; set; }
        public string TargetDocumentId { get; set; } = string.Empty;
        public string TargetAddress { get; set; } = string.Empty;
        public string RefreshPolicy { get; set; } = "manual";
    }

    public sealed class LinkRefreshRequest
    {
        public string LinkId { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = "anonymous";
    }

    public sealed class LinkRefreshResponse
    {
        public string LinkId { get; set; } = string.Empty;
        public CommandStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Enums
    // ═══════════════════════════════════════════════════════════════

    public enum OfficeHost
    {
        Unknown = 0,
        Excel = 1,
        PowerPoint = 2,
        Word = 3,
        Web = 4
    }

    public enum CommandExecutionTarget
    {
        Sidecar = 0,
        WebAddIn = 1,
        Backend = 2
    }

    public enum CommandStatus
    {
        Accepted = 0,
        Completed = 1,
        Failed = 2,
        Deferred = 3
    }

    public enum LinkSourceType
    {
        ExcelRange = 0,
        ExcelChart = 1,
        ExcelPivotTable = 2
    }

    public enum LinkTargetType
    {
        PowerPointShape = 0,
        PowerPointChart = 1,
        WordInlineShape = 2,
        WordTable = 3
    }

    public enum AuditSeverity
    {
        Debug = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}
