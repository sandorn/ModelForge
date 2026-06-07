import type {
  ApiEnvelope,
  AdminDiagnosticsResponse,
  AdminAuditRetentionRequest,
  AdminAuditRetentionResponse,
  AdminAuditEventsResponse,
  AdminAuditEventsQuery,
  AdminAuditSummaryResponse,
  AdminRolesResponse,
  AdminUserCreateRequest,
  AdminUserResponse,
  AdminUserToggleResponse,
  CommandDefinition,
  CommandDispatchRequest,
  CommandDispatchResponse,
  ConfigurationResponse,
  ConfigurationUpsertRequest,
  CreateLinkMetadataRequest,
  DictionaryCheckRequest,
  DictionaryCheckResponse,
  DictionaryExportResponse,
  DictionaryImportRequest,
  DictionaryImportResponse,
  DictionaryTerm,
  HealthResponse,
  LinkMetadata,
  LinkRefreshRequest,
  LinkRefreshResponse,
  VersionInfoResponse,
} from '../types/contracts';
import { useAuthStore } from './authStore';

const DEFAULT_BACKEND_URL = import.meta.env.VITE_MODELFORGE_API_URL ?? 'http://localhost:5095';

function buildQueryString(query: object): string {
  const params = new URLSearchParams();
  Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return;
    }

    params.set(key, String(value));
  });

  const payload = params.toString();
  return payload ? `?${payload}` : '';
}

export class ApiClient {
  constructor(private readonly baseUrl: string = DEFAULT_BACKEND_URL) {}

  getBackendBaseUrl() {
    return this.baseUrl;
  }

  // ═══════════════════════════════════════════════════════
  //  Health & Version
  // ═══════════════════════════════════════════════════════

  async getHealth() {
    return this.get<HealthResponse>('/health');
  }

  async getVersion() {
    return this.get<VersionInfoResponse>('/api/version');
  }

  // ═══════════════════════════════════════════════════════
  //  Commands
  // ═══════════════════════════════════════════════════════

  async getCommands() {
    return this.get<CommandDefinition[]>('/api/commands');
  }

  async dispatchCommand(req: CommandDispatchRequest) {
    return this.post<CommandDispatchResponse>('/api/commands/dispatch', req);
  }

  // ═══════════════════════════════════════════════════════
  //  Configuration
  // ═══════════════════════════════════════════════════════

  async getConfig(scope: string) {
    return this.get<ConfigurationResponse>(`/api/config/${scope}`);
  }

  async upsertConfig(scope: string, req: ConfigurationUpsertRequest) {
    return this.put<ConfigurationResponse>(`/api/config/${scope}`, req);
  }

  // ═══════════════════════════════════════════════════════
  //  Audit
  // ═══════════════════════════════════════════════════════

  async postAuditEvent(body: Record<string, unknown>) {
    return this.post<{ eventId: string; recordedAtUtc: string }>('/api/audit-events', body);
  }

  // ═══════════════════════════════════════════════════════
  //  Admin
  // ═══════════════════════════════════════════════════════

  async getAdminUsers() {
    return this.get<AdminUserResponse[]>('/api/admin/users');
  }

  async createAdminUser(req: AdminUserCreateRequest) {
    return this.post<AdminUserResponse>('/api/admin/users', req);
  }

  async toggleAdminUser(userId: string) {
    return this.put<AdminUserToggleResponse>(`/api/admin/users/${encodeURIComponent(userId)}/toggle`, {});
  }

  async getAdminRoles() {
    return this.get<AdminRolesResponse>('/api/admin/roles');
  }

  async getAdminAuditEvents(query: AdminAuditEventsQuery = { count: 100 }) {
    return this.get<AdminAuditEventsResponse>(`/api/admin/audit-events${buildQueryString(query)}`);
  }

  async getAdminAuditSummary(hours = 168, query: AdminAuditEventsQuery = {}) {
    return this.get<AdminAuditSummaryResponse>(`/api/admin/audit-events/summary${buildQueryString({ ...query, hours } as AdminAuditEventsQuery & { hours: number })}`);
  }

  async downloadAdminAuditCsv(query: AdminAuditEventsQuery = { count: 500 }) {
    const response = await fetch(`${this.baseUrl}/api/admin/audit-events/export${buildQueryString(query)}`, {
      headers: this.buildHeaders(),
    });
    if (!response.ok) {
      throw new Error(`Request failed: ${response.status} ${response.statusText}`);
    }
    return response.blob();
  }

  async applyAdminAuditRetention(req: AdminAuditRetentionRequest) {
    return this.post<AdminAuditRetentionResponse>('/api/admin/audit-events/retention', req);
  }

  async getAdminDiagnostics() {
    return this.get<AdminDiagnosticsResponse>('/api/admin/diagnostics');
  }

  async downloadAdminDiagnosticsBundle() {
    const response = await fetch(`${this.baseUrl}/api/admin/diagnostics/bundle`, {
      headers: this.buildHeaders(),
    });
    if (!response.ok) {
      throw new Error(`Request failed: ${response.status} ${response.statusText}`);
    }
    return response.blob();
  }

  // ═══════════════════════════════════════════════════════
  //  Corporate Dictionary
  // ═══════════════════════════════════════════════════════

  async getDictionaryTerms() {
    return this.get<DictionaryTerm[]>('/api/dictionary/');
  }

  async upsertDictionaryTerm(term: Partial<DictionaryTerm>) {
    return this.post<DictionaryTerm>('/api/dictionary/', term);
  }

  async deleteDictionaryTerm(id: string) {
    return this.delete<{ deleted: string }>(`/api/dictionary/${encodeURIComponent(id)}`);
  }

  async checkDictionaryText(req: DictionaryCheckRequest) {
    return this.post<DictionaryCheckResponse>('/api/dictionary/check', req);
  }

  async exportDictionaryTerms() {
    return this.get<DictionaryExportResponse>('/api/dictionary/export');
  }

  async importDictionaryTerms(req: DictionaryImportRequest) {
    return this.post<DictionaryImportResponse>('/api/dictionary/import', req);
  }

  // ═══════════════════════════════════════════════════════
  //  Link Metadata
  // ═══════════════════════════════════════════════════════

  async getLinks() {
    return this.get<LinkMetadata[]>('/api/links');
  }

  async createLink(req: CreateLinkMetadataRequest) {
    return this.post<LinkMetadata>('/api/links', req);
  }

  async refreshLink(linkId: string, req: LinkRefreshRequest) {
    return this.post<LinkRefreshResponse>(`/api/links/${linkId}/refresh`, req);
  }

  // ═══════════════════════════════════════════════════════
  //  Auth
  // ═══════════════════════════════════════════════════════

  async login(username: string, password: string) {
    const body = { username, password };
    return this.post<{ token: string; userId: string; username: string; role: string; expiresAt: string }>('/api/auth/login', body);
  }

  // ═══════════════════════════════════════════════════════
  //  HTTP helpers
  // ═══════════════════════════════════════════════════════

  private async get<T>(path: string) {
    const response = await fetch(`${this.baseUrl}${path}`, {
      headers: this.buildHeaders(),
    });
    return this.unwrap<T>(response);
  }

  private async post<T>(path: string, body: unknown) {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: 'POST',
      headers: this.buildHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(body),
    });
    return this.unwrap<T>(response);
  }

  private async put<T>(path: string, body: unknown) {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: 'PUT',
      headers: this.buildHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(body),
    });
    return this.unwrap<T>(response);
  }

  private async delete<T>(path: string) {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: 'DELETE',
      headers: this.buildHeaders(),
    });
    return this.unwrap<T>(response);
  }

  private async unwrap<T>(response: Response): Promise<T> {
    if (!response.ok) {
      throw new Error(`Request failed: ${response.status} ${response.statusText}`);
    }
    const envelope = (await response.json()) as ApiEnvelope<T>;
    if (envelope.error) {
      throw new Error(envelope.error);
    }
    if (envelope.data === undefined) {
      throw new Error('Backend response missing data field.');
    }
    return envelope.data;
  }

  private buildHeaders(extra?: Record<string, string>): Headers {
    const headers = new Headers({
      Accept: 'application/json',
      'X-Trace-Id': crypto.randomUUID(),
      ...extra,
    });
    const token = useAuthStore.getState().token;
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  }
}

export const apiClient = new ApiClient();
