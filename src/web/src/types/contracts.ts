// ═══════════════════════════════════════════════════════════════
//  ModelForge TypeScript Contracts
//  Must stay in sync with C# src/shared/ModelForge.Contracts/ApiContracts.cs
// ═══════════════════════════════════════════════════════════════

// ── Unified API Envelope ──
export type ApiEnvelope<T> = {
  traceId: string;
  data?: T;
  error?: string;
};

// ── Health & Version ──
export type HealthResponse = {
  status: string;
  service: string;
  timestampUtc: string;
};

export type VersionInfoResponse = {
  product: string;
  component: string;
  version: string;
  apiVersion: string;
  buildTimestampUtc: string;
};

// ── Configuration ──
export type ConfigurationResponse = {
  scope: string;
  values: Record<string, string>;
  updatedAtUtc: string;
};

export type ConfigurationUpsertRequest = {
  values: Record<string, string>;
  updatedBy?: string;
};

// ── Command Catalog & Dispatch ──
export type CommandDefinition = {
  id: string;
  displayName: string;
  host: OfficeHost;
  target: CommandExecutionTarget;
  category: string;
  defaultShortcut?: string;
  description: string;
};

export type CommandDispatchRequest = {
  commandId: string;
  host: OfficeHost;
  userId?: string;
  workbookId?: string;
  arguments?: Record<string, string>;
};

export type CommandDispatchResponse = {
  dispatchId: string;
  commandId: string;
  status: CommandStatus;
  message: string;
  acceptedAtUtc: string;
};

// ── Sidecar Execution ──
export type SidecarExecuteRequest = {
  commandId: string;
  host: string;
  arguments?: Record<string, string>;
};

export type SidecarExecuteResponse = {
  success: boolean;
  commandId: string;
  message: string;
  result?: string;
};

export type SidecarStatusResponse = {
  connected: boolean;
  workbook?: string;
  worksheet?: string;
  selection?: string;
  version?: string;
  error?: string;
};

// ── Audit ──
export type AuditEventRequest = {
  eventType: string;
  actorId: string;
  host: OfficeHost;
  severity: AuditSeverity;
  commandId?: string;
  resourceId?: string;
  metadata?: Record<string, string>;
};

export type AuditEventResponse = {
  eventId: string;
  recordedAtUtc: string;
};

// ── Link Metadata ──
export type LinkMetadata = {
  linkId: string;
  sourceType: LinkSourceType;
  sourceDocumentId: string;
  sourceAddress: string;
  targetType: LinkTargetType;
  targetDocumentId: string;
  targetAddress: string;
  refreshPolicy: string;
  createdAtUtc: string;
  lastRefreshedAtUtc?: string;
};

export type CreateLinkMetadataRequest = {
  sourceType: LinkSourceType;
  sourceDocumentId: string;
  sourceAddress: string;
  targetType: LinkTargetType;
  targetDocumentId: string;
  targetAddress: string;
  refreshPolicy?: string;
};

export type LinkRefreshRequest = {
  linkId: string;
  requestedBy: string;
};

export type LinkRefreshResponse = {
  linkId: string;
  status: CommandStatus;
  message: string;
  requestedAtUtc: string;
};

// ── Enums (must match C# numeric values) ──
export enum OfficeHost {
  Unknown = 0,
  Excel = 1,
  PowerPoint = 2,
  Word = 3,
  Web = 4,
}

export enum CommandExecutionTarget {
  Sidecar = 0,
  WebAddIn = 1,
  Backend = 2,
}

export enum CommandStatus {
  Accepted = 0,
  Completed = 1,
  Failed = 2,
  Deferred = 3,
}

export enum LinkSourceType {
  ExcelRange = 0,
  ExcelChart = 1,
  ExcelPivotTable = 2,
}

export enum LinkTargetType {
  PowerPointShape = 0,
  PowerPointChart = 1,
  WordInlineShape = 2,
  WordTable = 3,
}

export enum AuditSeverity {
  Debug = 0,
  Information = 1,
  Warning = 2,
  Error = 3,
  Critical = 4,
}