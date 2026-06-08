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

// ── Auth & Admin ──
export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  token: string;
  userId: string;
  username: string;
  role: string;
  expiresAt: string;
};

export type AdminUserCreateRequest = {
  username: string;
  password: string;
  role?: string;
};

export type AdminUserResponse = {
  id: string;
  username: string;
  role: string;
  isActive: boolean;
  createdAt: string;
};

export type AdminUserToggleResponse = {
  userId: string;
  active: boolean;
};

export type AdminRolePermissionResponse = {
  role: string;
  permissions: string[];
  builtIn: boolean;
};

export type AdminRolesResponse = {
  roles: AdminRolePermissionResponse[];
};

export type AdminAuditEventItem = {
  eventId: string;
  eventType: string;
  actorId: string;
  host: OfficeHost;
  severity: AuditSeverity;
  commandId?: string;
  resourceId?: string;
  recordedAtUtc: string;
};

export type AdminAuditEventsResponse = {
  items: AdminAuditEventItem[];
  pagination: {
    page: number;
    pageSize: number;
    total: number;
  };
  query: AdminAuditEventsQuery;
};

export type AdminAuditEventsQuery = {
  count?: number;
  page?: number;
  pageSize?: number;
  eventType?: string;
  actorId?: string;
  host?: OfficeHost;
  severity?: AuditSeverity;
  commandId?: string;
  resourceId?: string;
  search?: string;
  sinceUtc?: string;
  untilUtc?: string;
};

export type AdminAuditSummaryBucket = {
  key: string;
  count: number;
};

export type AdminAuditTimelineBucket = {
  startUtc: string;
  endUtc: string;
  count: number;
};

export type AdminAuditHeatmapCell = {
  rowKey: string;
  columnKey: string;
  count: number;
};

export type AdminAuditSummaryResponse = {
  generatedAtUtc: string;
  windowHours: number;
  bucketHours: number;
  totalEvents: number;
  byEventType: AdminAuditSummaryBucket[];
  byHost: AdminAuditSummaryBucket[];
  byActor: AdminAuditSummaryBucket[];
  timeline: AdminAuditTimelineBucket[];
  heatmap: AdminAuditHeatmapCell[];
  query: AdminAuditEventsQuery;
};

export type AdminAuditRetentionRequest = {
  retentionDays?: number;
  dryRun?: boolean;
};

export type AdminAuditRetentionResponse = {
  retentionDays: number;
  cutoffUtc: string;
  matchedEvents: number;
  deletedEvents: number;
  dryRun: boolean;
  executedAtUtc: string;
};

export type AdminDiagnosticsResponse = {
  generatedAtUtc: string;
  version: VersionInfoResponse;
  databaseProvider: string;
  databaseConnected: boolean;
  commandCount: number;
  linkCount: number;
  dictionaryTermCount: number;
  recentAuditEventCount: number;
  auditRetentionDays: number;
  auditRetentionCutoffUtc: string;
  auditEventsEligibleForRetentionPrune: number;
  configuration: Record<string, string>;
  notes: string[];
};

export type AdminDiagnosticsBundleResponse = {
  generatedAtUtc: string;
  summary: AdminDiagnosticsResponse;
  runtime: Record<string, string>;
  recentAuditEvents: AdminAuditEventItem[];
  notes: string[];
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

export type ShortcutItem = {
  commandId: string;
  displayName: string;
  shortcut: string;
};

export type ShortcutExportResponse = {
  shortcuts: ShortcutItem[];
  count: number;
  exportedAtUtc: string;
};

export type ShortcutImportRequest = {
  shortcuts: ShortcutItem[];
};

export type ShortcutImportResponse = {
  imported: number;
  shortcuts: ShortcutItem[];
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
  recorded: boolean;
  message?: string;
};

// ── Corporate Dictionary ──
export type DictionaryTerm = {
  id: string;
  term: string;
  replacement?: string;
  regexPattern?: string;
  category: string;
  severity: string;
  updatedAt: string;
};

export type TermMatch = {
  termId: string;
  term: string;
  matchedText: string;
  position: number;
  suggestion?: string;
};

export type DictionaryCheckRequest = {
  text: string;
  language?: string;
};

export type DictionaryCheckResponse = {
  originalText: string;
  matches: TermMatch[];
  matchCount: number;
  cleanedText?: string;
};

export type DictionaryImportRequest = {
  terms: DictionaryTerm[];
  overwrite?: boolean;
};

export type DictionaryImportError = {
  index: number;
  term?: string;
  error: string;
};

export type DictionaryImportResponse = {
  imported: number;
  skipped: number;
  errors: DictionaryImportError[];
  terms: DictionaryTerm[];
};

export type DictionaryExportResponse = {
  terms: DictionaryTerm[];
  count: number;
  exportedAtUtc: string;
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

// ── Dashboard ──
export type DashboardTopCommand = {
  commandId: string;
  count: number;
};

export type DashboardHostBucket = {
  host: string;
  count: number;
};

export type DashboardTimelineBucket = {
  label: string;
  count: number;
};

export type DashboardSummaryResponse = {
  generatedAtUtc: string;
  windowHours: number;
  totalEvents: number;
  activeUserCount: number;
  topCommands: DashboardTopCommand[];
  byHost: DashboardHostBucket[];
  timeline: DashboardTimelineBucket[];
};

// ── AIWA Chat ──
export type AiwaChatRequest = {
  message: string;
  mode: string;
};

export type AiwaChatResponse = {
  response: string;
  mode: string;
  model: string;
  fallbackMock: boolean;
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
