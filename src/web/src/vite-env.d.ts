/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_MODELFORGE_API_URL?: string;
  readonly VITE_MODELFORGE_SIDECAR_URL?: string;
  readonly VITE_MODELFORGE_SIDECAR_TOKEN?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
