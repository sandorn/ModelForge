import { afterEach, describe, expect, it } from 'vitest';
import { COMMAND_GROUPS, __appTestables } from '../App';

describe('App command groups', () => {
  afterEach(() => {
    localStorage.clear();
  });

  it('uses full Excel command ids from the backend catalog', () => {
    const commandIds = Object.values(COMMAND_GROUPS).flat();

    expect(commandIds.length).toBeGreaterThan(0);
    expect(commandIds).toContain('excel.fill-right');
    expect(commandIds).toContain('excel.model-check');
    expect(commandIds).toContain('excel.link-to-powerpoint');
    expect(commandIds.every((id) => id.startsWith('excel.'))).toBe(true);
  });

  it('does not use stale short command ids that cannot match the backend catalog', () => {
    const commandIds = Object.values(COMMAND_GROUPS).flat();

    expect(commandIds).not.toContain('fill-right');
    expect(commandIds).not.toContain('model-check');
    expect(commandIds).not.toContain('link-to-powerpoint');
  });

  it('parses shortcut export objects', () => {
    const result = __appTestables.parseShortcutImportPayload(JSON.stringify({
      shortcuts: [
        { commandId: 'excel.fill-right', displayName: '快速向右填充', shortcut: 'Ctrl+Alt+R' },
      ],
    }));

    expect(result).toHaveLength(1);
    expect(result[0].commandId).toBe('excel.fill-right');
  });

  it('parses raw shortcut arrays', () => {
    const result = __appTestables.parseShortcutImportPayload(JSON.stringify([
      { commandId: 'excel.fill-down', displayName: '快速向下填充', shortcut: 'Ctrl+Alt+D' },
    ]));

    expect(result).toHaveLength(1);
    expect(result[0].shortcut).toBe('Ctrl+Alt+D');
  });

  it('rejects invalid shortcut import JSON shape', () => {
    expect(() => __appTestables.parseShortcutImportPayload(JSON.stringify({ items: [] })))
      .toThrow('导入文件必须是快捷键数组');
  });

  it('stores trimmed Sidecar local API tokens', () => {
    __appTestables.saveStoredSidecarToken('  token-value  ');

    expect(__appTestables.getStoredSidecarToken()).toBe('token-value');
  });

  it('clears Sidecar local API token when blank', () => {
    localStorage.setItem('modelforge_sidecar_token', 'token-value');

    __appTestables.saveStoredSidecarToken('   ');

    expect(__appTestables.getStoredSidecarToken()).toBe('');
  });
});
