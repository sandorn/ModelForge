import { AuditSeverity, OfficeHost } from '../types/contracts';
import { apiClient } from './apiClient';
import { useAuthStore } from './authStore';

export type UiAuditAction =
  | 'nav.open'
  | 'app.refresh'
  | 'auth.logout'
  | 'omnibar.open'
  | 'command.execute'
  | 'shortcut.refresh'
  | 'shortcut.save'
  | 'shortcut.export'
  | 'shortcut.import'
  | 'sidecar.token.save'
  | 'deck.check'
  | 'deck.export_pdf'
  | 'links.refresh'
  | 'admin.tab.open'
  | 'admin.audit.drilldown'
  | 'admin.audit.export_csv'
  | 'admin.audit.retention.preview'
  | 'admin.audit.retention.prune'
  | 'admin.diagnostics.refresh'
  | 'admin.diagnostics.download'
  | 'dictionary.term.add'
  | 'dictionary.term.delete'
  | 'dictionary.check'
  | 'dictionary.export'
  | 'dictionary.import'
  | 'aiwa.send'
  | 'aiwa.mode.change'
  | 'aiwa.dictionary.toggle';

export type UiAuditOptions = {
  action: UiAuditAction;
  commandId?: string;
  resourceId?: string;
  metadata?: Record<string, string | number | boolean | undefined>;
};

function normalizeMetadata(metadata?: UiAuditOptions['metadata']): Record<string, string> {
  const normalized: Record<string, string> = {};
  Object.entries(metadata ?? {}).forEach(([key, value]) => {
    if (value !== undefined) {
      normalized[key] = String(value);
    }
  });
  return normalized;
}

export function buildUiAuditPayload(options: UiAuditOptions) {
  const user = useAuthStore.getState().user;
  return {
    eventType: `ui.${options.action}`,
    actorId: user?.userId ?? user?.username ?? 'web-anonymous',
    host: OfficeHost.Web,
    severity: AuditSeverity.Information,
    commandId: options.commandId,
    resourceId: options.resourceId,
    metadata: normalizeMetadata({
      source: 'web-addin',
      username: user?.username,
      role: user?.role,
      ...options.metadata,
    }),
  };
}

export function recordUiAction(options: UiAuditOptions): void {
  void apiClient.postAuditEvent(buildUiAuditPayload(options)).catch(() => {
    // UI telemetry is best-effort and must never block user operations.
  });
}
