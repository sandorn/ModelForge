import { describe, expect, it } from 'vitest';
import { __aiwaChatTestables } from '../components/AiwaChat';

describe('AiwaChat text helpers', () => {
  it('generates readable Chinese mock responses', () => {
    const response = __aiwaChatTestables.generateMockResponse('收入增长明显，成本保持稳定。', 'summarize');

    expect(response).toContain('总结 (Mock)');
    expect(response).toContain('核心内容概括');
    expect(response).not.toContain('馃');
    expect(response).not.toContain('锛');
  });

  it('formats dictionary matches with suggestions', () => {
    const result = __aiwaChatTestables.formatDictionaryResult({
      originalText: 'draft',
      matchCount: 1,
      matches: [{
        termId: 't1',
        term: 'draft',
        matchedText: 'draft',
        position: 0,
        suggestion: '草稿',
      }],
    });

    expect(result).toContain('命中 1 项');
    expect(result).toContain('建议替换为「草稿」');
  });
});
