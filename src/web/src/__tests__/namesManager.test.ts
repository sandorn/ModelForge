import { describe, it, expect } from 'vitest';

/**
 * Tests for NamesManagerPanel data parsing logic.
 * Tests the parseNamesResult function extracted from NamesManagerPanel.tsx.
 */

interface NameInfo {
  name: string;
  refersTo: string;
  isVisible: boolean;
  isValid: boolean;
  error?: string;
}

interface NamesReport {
  allNames: NameInfo[];
  invalidNames: NameInfo[];
  totalCount: number;
  invalidCount: number;
  deletedCount?: number;
  deleteErrors?: string[];
}

function parseNamesResult(raw: string): NamesReport | null {
  try {
    const data = JSON.parse(raw);
    return {
      allNames: (data.AllNames || data.allNames || []).map((n: any) => ({
        name: n.Name || n.name || '',
        refersTo: n.RefersTo || n.refersTo || '',
        isVisible: n.IsVisible ?? n.isVisible ?? true,
        isValid: n.IsValid ?? n.isValid ?? true,
        error: n.Error || n.error || undefined,
      })),
      invalidNames: (data.InvalidNames || data.invalidNames || []).map((n: any) => ({
        name: n.Name || n.name || '',
        refersTo: n.RefersTo || n.refersTo || '',
        isVisible: n.IsVisible ?? n.isVisible ?? true,
        isValid: false,
        error: n.Error || n.error || undefined,
      })),
      totalCount: data.TotalCount ?? data.totalCount ?? 0,
      invalidCount: data.InvalidCount ?? data.invalidCount ?? 0,
      deletedCount: data.DeletedCount ?? data.deletedCount,
      deleteErrors: data.DeleteErrors || data.deleteErrors || [],
    };
  } catch {
    return null;
  }
}

describe('NamesManagerPanel parseNamesResult', () => {
  it('parses valid scan result with C# PascalCase', () => {
    const raw = JSON.stringify({
      AllNames: [
        { Name: 'TestRange', RefersTo: '=Sheet1!$A$1:$B$5', IsVisible: true, IsValid: true, Error: null },
        { Name: 'BrokenRef', RefersTo: '=#REF!', IsVisible: true, IsValid: false, Error: 'Reference cannot be resolved' },
      ],
      InvalidNames: [
        { Name: 'BrokenRef', RefersTo: '=#REF!', IsVisible: true, IsValid: false, Error: 'Reference cannot be resolved' },
      ],
      TotalCount: 2,
      InvalidCount: 1,
    });

    const result = parseNamesResult(raw);
    expect(result).not.toBeNull();
    expect(result!.totalCount).toBe(2);
    expect(result!.invalidCount).toBe(1);
    expect(result!.allNames).toHaveLength(2);
    expect(result!.invalidNames).toHaveLength(1);
    expect(result!.allNames[0].name).toBe('TestRange');
    expect(result!.allNames[0].isValid).toBe(true);
    expect(result!.allNames[1].name).toBe('BrokenRef');
    expect(result!.allNames[1].isValid).toBe(false);
    expect(result!.allNames[1].error).toBe('Reference cannot be resolved');
  });

  it('parses result with lowercase keys (TypeScript style)', () => {
    const raw = JSON.stringify({
      allNames: [{ name: 'Test', refersTo: '=A1', isVisible: true, isValid: true }],
      invalidNames: [],
      totalCount: 1,
      invalidCount: 0,
    });

    const result = parseNamesResult(raw);
    expect(result).not.toBeNull();
    expect(result!.totalCount).toBe(1);
    expect(result!.invalidCount).toBe(0);
    expect(result!.allNames[0].name).toBe('Test');
  });

  it('returns null for invalid JSON', () => {
    const result = parseNamesResult('not json');
    expect(result).toBeNull();
  });

  it('handles empty names list', () => {
    const raw = JSON.stringify({
      AllNames: [],
      InvalidNames: [],
      TotalCount: 0,
      InvalidCount: 0,
    });

    const result = parseNamesResult(raw);
    expect(result).not.toBeNull();
    expect(result!.totalCount).toBe(0);
    expect(result!.allNames).toHaveLength(0);
  });

  it('handles delete result with DeletedCount', () => {
    const raw = JSON.stringify({
      AllNames: [{ Name: 'Keep', RefersTo: '=A1', IsVisible: true, IsValid: true }],
      InvalidNames: [],
      TotalCount: 1,
      InvalidCount: 0,
      DeletedCount: 3,
      DeleteErrors: [],
    });

    const result = parseNamesResult(raw);
    expect(result).not.toBeNull();
    expect(result!.deletedCount).toBe(3);
    expect(result!.deleteErrors).toHaveLength(0);
  });

  it('handles delete errors', () => {
    const raw = JSON.stringify({
      AllNames: [],
      InvalidNames: [],
      TotalCount: 0,
      InvalidCount: 0,
      DeleteErrors: ['Failed to delete BrokenRef: Permission denied'],
    });

    const result = parseNamesResult(raw);
    expect(result).not.toBeNull();
    expect(result!.deleteErrors).toHaveLength(1);
    expect(result!.deleteErrors![0]).toContain('Permission denied');
  });

  it('handles missing optional deleteCount', () => {
    const raw = JSON.stringify({
      AllNames: [],
      InvalidNames: [],
      TotalCount: 0,
      InvalidCount: 0,
    });

    const result = parseNamesResult(raw);
    expect(result).not.toBeNull();
    expect(result!.deletedCount).toBeUndefined();
    expect(result!.deleteErrors).toHaveLength(0);
  });

  it('handles null values in name fields', () => {
    const raw = JSON.stringify({
      AllNames: [{ Name: null, RefersTo: null, IsVisible: true, IsValid: true }],
      InvalidNames: [],
      TotalCount: 1,
      InvalidCount: 0,
    });

    const result = parseNamesResult(raw);
    expect(result).not.toBeNull();
    expect(result!.allNames[0].name).toBe('');
    expect(result!.allNames[0].refersTo).toBe('');
  });
});
