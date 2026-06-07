import { useEffect, useRef, useState } from 'react';
import { Badge, Button, Text, Textarea } from '@fluentui/react-components';
import { apiClient } from '../services/apiClient';
import { recordUiAction } from '../services/uiAudit';
import type { DictionaryCheckResponse } from '../types/contracts';

type AiwaMode = 'summarize' | 'elaborate' | 'rephrase' | 'proofread' | 'translate';

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  mode?: AiwaMode;
  timestamp: string;
}

const MODE_LABELS: Record<AiwaMode, string> = {
  summarize: '总结',
  elaborate: '展开',
  rephrase: '改写',
  proofread: '校对',
  translate: '翻译',
};

export function AiwaChat() {
  const [messages, setMessages] = useState<Message[]>([{
    id: 'welcome',
    role: 'assistant',
    content: '你好，我是 ModelForge AI 写作助手（AIWA）。选择模式后输入文本，我可以帮你总结、展开、改写、校对或翻译。',
    timestamp: new Date().toISOString(),
  }]);
  const [input, setInput] = useState('');
  const [mode, setMode] = useState<AiwaMode>('summarize');
  const [loading, setLoading] = useState(false);
  const [dictionaryEnabled, setDictionaryEnabled] = useState(true);
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const send = async () => {
    const text = input.trim();
    if (!text) return;
    recordUiAction({
      action: 'aiwa.send',
      metadata: { mode, length: text.length, dictionaryEnabled },
    });

    const userMessage: Message = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      mode,
      timestamp: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setLoading(true);

    await new Promise((resolve) => setTimeout(resolve, 800));
    const mockResponse = generateMockResponse(text, mode);
    const response = await applyDictionaryGuardrails(mockResponse, dictionaryEnabled);

    setMessages((prev) => [...prev, {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: response,
      timestamp: new Date().toISOString(),
    }]);
    setLoading(false);
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      void send();
    }
  };

  return (
    <div className="aiwa-container">
      <div className="aiwa-header">
        <Text weight="bold" size={500}>AIWA</Text>
        <Badge appearance="tint" color="brand">本地 Mock</Badge>
        <Badge appearance="tint" color={dictionaryEnabled ? 'success' : 'warning'}>
          Dictionary {dictionaryEnabled ? 'On' : 'Off'}
        </Badge>
      </div>

      <div className="aiwa-modes">
        {(Object.keys(MODE_LABELS) as AiwaMode[]).map((item) => (
          <button
            key={item}
            className={`aiwa-mode-btn${mode === item ? ' active' : ''}`}
            onClick={() => {
              recordUiAction({ action: 'aiwa.mode.change', metadata: { mode: item } });
              setMode(item);
            }}
          >
            {MODE_LABELS[item]}
          </button>
        ))}
        <button
          className={`aiwa-mode-btn${dictionaryEnabled ? ' active' : ''}`}
          onClick={() => {
            recordUiAction({ action: 'aiwa.dictionary.toggle', metadata: { enabled: !dictionaryEnabled } });
            setDictionaryEnabled((prev) => !prev);
          }}
        >
          术语检查
        </button>
      </div>

      <div className="aiwa-messages">
        {messages.map((message) => (
          <div key={message.id} className={`aiwa-msg ${message.role}`}>
            <div className="aiwa-msg-header">
              <Text size={100} weight="semibold">
                {message.role === 'user' ? '你' : 'AIWA'}
                {message.mode && ` · ${MODE_LABELS[message.mode]}`}
              </Text>
            </div>
            <Text>{message.content}</Text>
          </div>
        ))}
        {loading && <div className="aiwa-msg assistant"><Text>AIWA 思考中...</Text></div>}
        <div ref={endRef} />
      </div>

      <div className="aiwa-input-area">
        <Textarea
          placeholder={`输入文本，AIWA 将帮你${MODE_LABELS[mode]}...`}
          value={input}
          onChange={(_, data) => setInput(data.value)}
          onKeyDown={handleKeyDown}
          rows={3}
          resize="vertical"
        />
        <Button appearance="primary" onClick={() => void send()} disabled={loading || !input.trim()}>
          发送
        </Button>
      </div>
    </div>
  );
}

function generateMockResponse(text: string, mode: AiwaMode): string {
  switch (mode) {
    case 'summarize':
      return `📝 **总结 (Mock)**\n\n这是对 "${text.substring(0, 30)}..." 的摘要：\n\n- 核心内容概括\n- 主要观点提炼\n- 关键数据点\n\n> ⚠️ 当前为本地 Mock 响应。连接 LLM API 后提供真实 AI 总结。`;
    case 'elaborate':
      return `📖 **展开 (Mock)**\n\n基于 "${text.substring(0, 30)}..." 的详细阐述：\n\n1. 背景说明\n2. 详细分析\n3. 示例与推导\n4. 结论\n\n> ⚠️ 当前为本地 Mock 响应。`;
    case 'rephrase':
      return `✏️ **改写 (Mock)**\n\n原文: "${text.substring(0, 50)}${text.length > 50 ? '...' : ''}"\n\n改写版本（更正式）:\n> 待 LLM API 接入后提供真实改写。\n\n改写版本（更简洁）:\n> 待 LLM API 接入后提供真实改写。`;
    case 'proofread':
      return `🔍 **校对 (Mock)**\n\n对 "${text.substring(0, 50)}..." 的校对结果：\n\n- ✅ 整体结构合理\n- ⚠️ 可能存在的语法问题\n- 💡 用词建议\n\n> ⚠️ 当前为本地 Mock 响应。`;
    case 'translate':
      return `🌐 **翻译 (Mock)**\n\n源文本: "${text.substring(0, 50)}..."\n\nEnglish:\n> Mock translation — LLM API connection required.\n\n> ⚠️ 当前为本地 Mock 响应。`;
    default:
      return '未知模式。请选择总结 / 展开 / 改写 / 校对 / 翻译。';
  }
}

async function applyDictionaryGuardrails(text: string, enabled: boolean) {
  if (!enabled) return text;

  try {
    const check = await apiClient.checkDictionaryText({ text, language: 'zh-CN' });
    if (check.matchCount === 0) {
      return `${text}\n\n✅ **Corporate Dictionary**：未发现术语风险。`;
    }

    return `${check.cleanedText ?? text}\n\n${formatDictionaryResult(check)}`;
  } catch (error) {
    const message = error instanceof Error ? error.message : 'unknown error';
    return `${text}\n\n⚠️ **Corporate Dictionary**：检查失败（${message}）。`;
  }
}

function formatDictionaryResult(check: DictionaryCheckResponse) {
  const items = check.matches.slice(0, 5).map((match) => {
    const suggestion = match.suggestion ? ` → 建议替换为「${match.suggestion}」` : '';
    return `- ${match.matchedText}（规则：${match.term}，位置：${match.position}）${suggestion}`;
  });
  const suffix = check.matches.length > 5 ? `\n- 另有 ${check.matches.length - 5} 项命中未展示` : '';
  return `⚠️ **Corporate Dictionary**：命中 ${check.matchCount} 项\n${items.join('\n')}${suffix}`;
}

export const __aiwaChatTestables = {
  generateMockResponse,
  formatDictionaryResult,
};
