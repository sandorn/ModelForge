import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { JSDOM } from 'jsdom';

const webRoot = process.cwd();
const repoRoot = resolve(webRoot, '../..');
const manifestPath = resolve(repoRoot, 'manifest/modelForge.web.xml');
const functionFilePath = resolve(webRoot, 'public/function-file.html');

const allPptCommandIds = [
  'ppt.generate-agenda',
  'ppt.deck-check',
  'ppt.align-left',
  'ppt.align-center',
  'ppt.align-right',
  'ppt.align-top',
  'ppt.align-middle',
  'ppt.align-bottom',
  'ppt.distribute-horizontal',
  'ppt.distribute-vertical',
  'ppt.unify-width',
  'ppt.unify-height',
  'ppt.unify-size',
];

function getHostBlock(manifest: string, hostType: 'Workbook' | 'Presentation' | 'Document') {
  const pattern = new RegExp(`<Host xsi:type="${hostType}">([\\s\\S]*?)</Host>`);
  const match = manifest.match(pattern);
  return match?.[1] ?? '';
}

function getControlIds(hostBlock: string) {
  return [...hostBlock.matchAll(/<Control xsi:type="Button" id="([^"]+)"/g)].map((match) => match[1]);
}

function getCustomTabBlocks(manifest: string) {
  return [...manifest.matchAll(/<CustomTab id="[^"]+">([\s\S]*?)<\/CustomTab>/g)].map((match) => match[1]);
}

describe('Office manifest Ribbon command wiring', () => {
  const manifest = readFileSync(manifestPath, 'utf8');
  const functionFile = readFileSync(functionFilePath, 'utf8');

  it('declares FunctionFile for every host that uses ExecuteFunction', () => {
    for (const hostType of ['Workbook', 'Presentation', 'Document'] as const) {
      const hostBlock = getHostBlock(manifest, hostType);
      expect(hostBlock, `${hostType} host block`).not.toBe('');
      expect(hostBlock).toContain('<FunctionFile resid="FunctionFile.Url" />');
      expect(hostBlock).toContain('<Action xsi:type="ExecuteFunction">');
    }
  });

  it('maps Ribbon command prefixes to Sidecar host values', () => {
    const dom = new JSDOM(functionFile, { runScripts: 'dangerously' });
    const helpers = (dom.window as any).ModelForgeFunctionFile;

    expect(helpers.getHostForCommand('excel.fill-right')).toBe('excel');
    expect(helpers.getHostForCommand('ppt.align-left')).toBe('powerpoint');
    expect(helpers.getHostForCommand('word.build-cim')).toBe('word');
    expect(helpers.getHostForCommand('unknown.command')).toBe('excel');
  });

  it('posts ExecuteFunction commands to the Sidecar envelope endpoint', async () => {
    const calls: Array<{ url: string; init: RequestInit }> = [];
    const dom = new JSDOM(functionFile, {
      url: 'http://localhost:5173/function-file.html',
      runScripts: 'dangerously',
      beforeParse(window) {
        window.localStorage.setItem('modelforge_sidecar_token', 'ribbon-local-token');
        window.fetch = (async (url: string | URL | Request, init?: RequestInit) => {
          calls.push({ url: String(url), init: init ?? {} });
          return new Response(JSON.stringify({
            traceId: 'trace-test',
            data: {
              success: true,
              commandId: 'ppt.align-left',
              message: 'ok',
            },
          }));
        }) as typeof fetch;
      },
    });

    const helpers = (dom.window as any).ModelForgeFunctionFile;
    const result = await helpers.executeModelForgeCommand('ppt.align-left', 'trace-test');

    expect(result.message).toBe('ok');
    expect(calls).toHaveLength(1);
    expect(calls[0].url).toBe('http://localhost:5200/api/execute');
    expect(calls[0].init.method).toBe('POST');
    expect((calls[0].init.headers as Record<string, string>)['X-Trace-Id']).toBe('trace-test');
    expect((calls[0].init.headers as Record<string, string>)['X-ModelForge-Sidecar-Token']).toBe('ribbon-local-token');
    expect(JSON.parse(String(calls[0].init.body))).toEqual({
      commandId: 'ppt.align-left',
      host: 'powerpoint',
    });
  });

  it('throws Sidecar envelope errors for Ribbon commands', async () => {
    const dom = new JSDOM(functionFile, {
      runScripts: 'dangerously',
      beforeParse(window) {
        window.fetch = (async () => new Response(JSON.stringify({
          traceId: 'trace-test',
          data: {
            success: false,
            commandId: 'word.build-cim',
            message: 'Word 未运行。',
          },
          error: 'Word 未运行。',
        }), { status: 503 })) as typeof fetch;
      },
    });

    const helpers = (dom.window as any).ModelForgeFunctionFile;
    await expect(helpers.executeModelForgeCommand('word.build-cim', 'trace-test'))
      .rejects.toThrow('Word 未运行。');
  });

  it('formats visible feedback for Ribbon command results', () => {
    const dom = new JSDOM(functionFile, { runScripts: 'dangerously' });
    const helpers = (dom.window as any).ModelForgeFunctionFile;

    expect(helpers.getFeedbackMessage('ppt.align-left', { message: '已将 3 个形状左对齐。' }, false))
      .toContain('ModelForge 执行完成');
    expect(helpers.getFeedbackMessage('ppt.align-left', { message: '已将 3 个形状左对齐。' }, false))
      .toContain('ppt.align-left');
    expect(helpers.getFeedbackMessage('ppt.align-left', new Error('请先选中至少 2 个形状。'), true))
      .toContain('ModelForge 执行失败');
  });

  it('shows visible feedback after successful onExecute commands', async () => {
    let alertMessage = '';
    const dom = new JSDOM(functionFile, {
      runScripts: 'dangerously',
      beforeParse(window) {
        window.alert = (message?: unknown) => {
          alertMessage = String(message);
        };
        window.fetch = (async () => new Response(JSON.stringify({
          traceId: 'trace-test',
          data: {
            success: true,
            commandId: 'ppt.align-left',
            message: '已将 2 个形状左对齐。',
          },
        }))) as typeof fetch;
      },
    });
    const helpers = (dom.window as any).ModelForgeFunctionFile;
    let completed = false;

    await helpers.onExecute({
      source: { id: 'ppt.align-left' },
      completed: () => { completed = true; },
    });

    expect(completed).toBe(true);
    expect(alertMessage).toContain('ModelForge 执行完成');
    expect(alertMessage).toContain('已将 2 个形状左对齐。');
  });

  it('registers onExecute with Office.actions when Office.js is available', () => {
    let registeredName = '';
    let readyCalled = false;

    new JSDOM(functionFile, {
      runScripts: 'dangerously',
      beforeParse(window) {
        (window as any).Office = {
          onReady: (callback: () => void) => {
            readyCalled = true;
            callback();
          },
          actions: {
            associate: (name: string) => {
              registeredName = name;
            },
          },
        };
      },
    });

    expect(readyCalled).toBe(true);
    expect(registeredName).toBe('onExecute');
  });

  it('keeps all manifest ExecuteFunction ids covered by known host prefixes', () => {
    const controls = [
      ...getControlIds(getHostBlock(manifest, 'Workbook')),
      ...getControlIds(getHostBlock(manifest, 'Presentation')),
      ...getControlIds(getHostBlock(manifest, 'Document')),
    ].filter((id) => id !== 'ModelForge.OpenTaskPane');

    expect(controls.length).toBeGreaterThan(0);
    for (const controlId of controls) {
      expect(controlId).toMatch(/^(excel|ppt|word)\./);
    }
  });

  it('does not nest Ribbon groups inside other groups', () => {
    for (const tabBlock of getCustomTabBlocks(manifest)) {
      const groupBlocks = [...tabBlock.matchAll(/<Group id="[^"]+">([\s\S]*?)<\/Group>/g)].map((match) => match[1]);
      expect(groupBlocks.length).toBeGreaterThan(0);
      for (const groupBlock of groupBlocks) {
        expect(groupBlock).not.toContain('<Group id=');
      }
    }
  });

  it('provides required descriptions for every Ribbon supertip', () => {
    const supertips = [...manifest.matchAll(/<Supertip>([\s\S]*?)<\/Supertip>/g)].map((match) => match[1]);

    expect(supertips.length).toBeGreaterThan(0);
    for (const supertip of supertips) {
      expect(supertip).toContain('<Title resid=');
      expect(supertip).toContain('<Description resid=');
    }
    expect(manifest).toContain('<bt:String id="Command.Tooltip"');
  });

  it('exposes every supported PowerPoint command on the Ribbon', () => {
    const pptControls = getControlIds(getHostBlock(manifest, 'Presentation'));

    expect(pptControls).toEqual(expect.arrayContaining(allPptCommandIds));
    expect(pptControls.filter((id) => id.startsWith('ppt.')).sort()).toEqual([...allPptCommandIds].sort());
  });
});
