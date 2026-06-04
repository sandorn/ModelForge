export type ApiEnvelope<T> = {
  traceId: string;
  data?: T;
  error?: string;
};

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

export type CommandDefinition = {
  id: string;
  displayName: string;
  host: OfficeHost;
  target: CommandExecutionTarget;
  category: string;
  defaultShortcut?: string;
  description: string;
};

export enum OfficeHost {
  Unknown = 0,
  Excel = 1,
  PowerPoint = 2,
  Word = 3,
  Web = 4
}

export enum CommandExecutionTarget {
  Sidecar = 0,
  WebAddIn = 1,
  Backend = 2
}
