import { describe, expect, it } from 'vitest';
import { __deckCheckViewerTestables } from '../components/DeckCheckViewer';

type DeckIssue = {
  slide: number;
  type: 'font' | 'term' | 'number' | 'density' | 'logo';
  message: string;
};

type DeckReport = {
  slidesScanned: number;
  fontIssues: number;
  termIssues: number;
  missingSlideNumbers: number;
  denseTextSlides: number;
  logoIssues: number;
  logoPositionIssues: number;
  templateName?: string;
  reportTitle?: string;
  brandPrimaryColor?: string;
  brandAccentColor?: string;
  totalIssues: number;
  overallStatus: 'Pass' | 'Review' | 'ActionRequired';
  reportPath?: string;
  issues: DeckIssue[];
};

const parseDeckCheckResult = __deckCheckViewerTestables.parseDeckCheckResult as (raw: string) => DeckReport | null;

describe('DeckCheckViewer parseDeckCheckResult', () => {
  it('parses a clean presentation report', () => {
    const raw = JSON.stringify({
      SlidesScanned: 5,
      FontIssues: 0,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      LogoIssues: 0,
      LogoPositionIssues: 0,
      Issues: [],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.slidesScanned).toBe(5);
    expect(result!.fontIssues).toBe(0);
    expect(result!.logoIssues).toBe(0);
    expect(result!.logoPositionIssues).toBe(0);
    expect(result!.totalIssues).toBe(0);
    expect(result!.overallStatus).toBe('Pass');
    expect(result!.issues).toHaveLength(0);
  });

  it('parses font issues correctly', () => {
    const raw = JSON.stringify({
      SlidesScanned: 3,
      FontIssues: 2,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      LogoIssues: 0,
      Issues: [
        'Slide 1: font Times New Roman (Shape: Title)',
        'Slide 3: 字体 Comic Sans (Shape: TextBox)',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.fontIssues).toBe(2);
    expect(result!.issues).toHaveLength(2);
    expect(result!.issues[0].type).toBe('font');
    expect(result!.issues[1].type).toBe('font');
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
      LogoIssues: 0,
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
      LogoIssues: 0,
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
      LogoIssues: 0,
      Issues: [
        'Slide 4: Text density too high (2500 chars)',
        'Slide 7: Dense text (3000 characters)',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.denseTextSlides).toBe(2);
    expect(result!.issues[0].type).toBe('density');
  });

  it('parses logo issues and exported report path', () => {
    const raw = JSON.stringify({
      SlidesScanned: 4,
      FontIssues: 0,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      LogoIssues: 2,
      LogoPositionIssues: 1,
      TemplateName: 'ModelForge enterprise template',
      ReportTitle: 'ModelForge Brand Compliance Report',
      BrandPrimaryColor: '#1F3A5F',
      BrandAccentColor: '#3B82F6',
      TotalIssues: 3,
      OverallStatus: 'Review',
      ReportPath: 'C:\\Reports\\deck-check.pdf',
      Issues: [
        'Slide 1: Missing logo',
        'Slide 4: Logo position outside template bounds',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.logoIssues).toBe(2);
    expect(result!.logoPositionIssues).toBe(1);
    expect(result!.templateName).toBe('ModelForge enterprise template');
    expect(result!.reportTitle).toBe('ModelForge Brand Compliance Report');
    expect(result!.brandPrimaryColor).toBe('#1F3A5F');
    expect(result!.brandAccentColor).toBe('#3B82F6');
    expect(result!.totalIssues).toBe(3);
    expect(result!.overallStatus).toBe('Review');
    expect(result!.reportPath).toBe('C:\\Reports\\deck-check.pdf');
    expect(result!.issues).toHaveLength(2);
    expect(result!.issues[0].type).toBe('logo');
    expect(result!.issues[1].type).toBe('logo');
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
      LogoIssues: 1,
      Issues: [
        'Slide 1: font Arial Narrow (Shape: Title)',
        'Slide 2: Contains forbidden term DRAFT (Shape: Body)',
        'Slide 3: Missing slide number',
        'Slide 5: Text density too high (2200 chars)',
        'Slide 6: Missing logo',
      ],
    });

    const result = parseDeckCheckResult(raw);
    expect(result).not.toBeNull();
    expect(result!.issues).toHaveLength(5);
    expect(result!.issues[0].type).toBe('font');
    expect(result!.issues[1].type).toBe('term');
    expect(result!.issues[2].type).toBe('number');
    expect(result!.issues[3].type).toBe('density');
    expect(result!.issues[4].type).toBe('logo');
  });

  it('handles empty Issues array', () => {
    const raw = JSON.stringify({
      SlidesScanned: 1,
      FontIssues: 0,
      TermIssues: 0,
      MissingSlideNumbers: 0,
      DenseTextSlides: 0,
      LogoIssues: 0,
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
    expect(result!.logoIssues).toBe(0);
    expect(result!.totalIssues).toBe(0);
    expect(result!.overallStatus).toBe('Pass');
    expect(result!.issues).toHaveLength(0);
  });
});
