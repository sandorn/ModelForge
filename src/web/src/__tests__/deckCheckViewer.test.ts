import { describe, it, expect } from 'vitest';

/**
 * Tests for DeckCheckViewer data parsing logic.
 */

interface DeckIssue {
  slide: number;
  type: 'font' | 'term' | 'number' | 'density';
  message: string;
}

interface DeckReport {
  slidesScanned: number;
  fontIssues: number;
  termIssues: number;
  missingSlideNumbers: number;
  denseTextSlides: number;
  issues: DeckIssue[];
}

function parseDeckCheckResult(raw: string): DeckReport | null {
  try {
    const data = JSON.parse(raw);
    if (!data || typeof data !== 'object') return null;

    const issues: DeckIssue[] = [];
    if (Array.isArray(data.Issues)) {
      for (const item of data.Issues) {
        const match = String(item).match(/Slide (\d+):\s*(.+)/);
        if (match) {
          const slideNum = parseInt(match[1], 10);
          const msg = match[2];
          let type: DeckIssue['type'] = 'term';
          if (msg.includes('font')) type = 'font';
          if (msg.includes('slide number') || msg.includes('Number')) type = 'number';
          if (msg.includes('density') || msg.includes('char') || msg.includes('Text density')) type = 'density';
          issues.push({ slide: slideNum, type, message: msg });
        }
      }
    }

    return {
      slidesScanned: data.SlidesScanned ?? 0,
      fontIssues: data.FontIssues ?? 0,
      termIssues: data.TermIssues ?? 0,
      missingSlideNumbers: data.MissingSlideNumbers ?? 0,
      denseTextSlides: data.DenseTextSlides ?? 0,
      issues,
    };
  } catch {
    return null;
  }
}

describe('DeckCheckViewer parseDeckCheckResult', () => {
  it('parses a clean presentation report', () => {
    const raw = JSON.stringify({
      SlidesScanned: 5,
      FontIssues: 0,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      Issues: [],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.slidesScanned).toBe(5);
    expect(result!.fontIssues).toBe(0);
    expect(result!.issues).toHaveLength(0);
  });

  it('parses font issues correctly', () => {
    const raw = JSON.stringify({
      SlidesScanned: 3,
      FontIssues: 2,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      Issues: [
        'Slide 1: font Times New Roman (Shape: Title)',
        'Slide 3: font Comic Sans (Shape: TextBox)',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.fontIssues).toBe(2);
    expect(result!.issues).toHaveLength(2);
    expect(result!.issues[0].type).toBe('font');
    expect(result!.issues[0].slide).toBe(1);
    expect(result!.issues[0].message).toContain('Times New Roman');
  });

  it('parses term issues correctly', () => {
    const raw = JSON.stringify({
      SlidesScanned: 4,
      FontIssues: 0,
      TermIssues: 1,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      Issues: [
        'Slide 2: Contains forbidden term DRAFT (Shape: Body)',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.termIssues).toBe(1);
    expect(result!.issues[0].type).toBe('term');
  });

  it('parses missing slide numbers', () => {
    const raw = JSON.stringify({
      SlidesScanned: 10,
      FontIssues: 0,
      TermIssues: 0,
      MissingSlideNumbers: 3,
      DenseTextSlides: 0,
      Issues: [
        'Slide 2: Missing slide number',
        'Slide 5: Missing slide number',
        'Slide 9: Missing slide number',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.missingSlideNumbers).toBe(3);
    expect(result!.issues[0].type).toBe('number');
  });

  it('parses dense text slides', () => {
    const raw = JSON.stringify({
      SlidesScanned: 8,
      FontIssues: 0,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 2,
      Issues: [
        'Slide 4: Text density too high (2500 chars)',
        'Slide 7: Text density too high (3000 chars)',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.denseTextSlides).toBe(2);
    expect(result!.issues[0].type).toBe('density');
  });

  it('returns null for invalid JSON', () => {
    const result = parseDeckCheckResult('{broken');
    expect(result).toBeNull();
  });

  it('returns null for non-object JSON', () => {
    const result = parseDeckCheckResult('"just a string"');
    expect(result).toBeNull();
  });

  it('handles mixed issue types', () => {
    const raw = JSON.stringify({
      SlidesScanned: 6,
      FontIssues: 1,
      TermIssues: 1,
      MissingSlideNumbers: 1,
      DenseTextSlides: 1,
      Issues: [
        'Slide 1: font Arial Narrow (Shape: Title)',
        'Slide 2: Contains forbidden term DRAFT (Shape: Body)',
        'Slide 3: Missing slide number',
        'Slide 5: Text density too high (2200 chars)',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.issues).toHaveLength(4);
    expect(result!.issues[0].type).toBe('font');
    expect(result!.issues[1].type).toBe('term');
    expect(result!.issues[2].type).toBe('number');
    expect(result!.issues[3].type).toBe('density');
  });

  it('handles empty Issues array', () => {
    const raw = JSON.stringify({
      SlidesScanned: 1,
      FontIssues: 0,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      Issues: [],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.issues).toHaveLength(0);
  });

  it('handles missing optional fields', () => {
    const raw = JSON.stringify({
      SlidesScanned: 2,
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.slidesScanned).toBe(2);
    expect(result!.fontIssues).toBe(0);
    expect(result!.issues).toHaveLength(0);
  });
});
