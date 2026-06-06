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
        public string Version { get; set; } = "0.1.0-stage1";
        public string ApiVersion { get; set; } = "v1";
        public DateTimeOffset BuildTimestampUtc { get; set; } = DateTimeOffset.UtcNow;
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