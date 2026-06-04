import { useState, useRef, useEffect } from 'react';
import { Button, Textarea, Text, Select, Badge } from '@fluentui/react-components';

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

/**
 * AIWA (AI Writing Assistant) Chat 前端。
 * 自然语言交互，支持 5 种模式。
 * 当前使用本地 mock 响应；生产环境通过后端桥接调用 LLM API。
 */
export function AiwaChat() {
  const [messages, setMessages] = useState<Message[]>([{
    id: 'welcome',
    role: 'assistant',
    content: '你好！我是 ModelForge AI 写作助手 (AIWA)。选择模式后在输入框中输入文本，我可以帮你总结、展开、改写、校对或翻译。',
    timestamp: new Date().toISOString(),
  }]);
  const [input, setInput] = useState('');
  const [mode, setMode] = useState<AiwaMode>('summarize');
  const [loading, setLoading] = useState(false);
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => { endRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages]);

  const send = async () => {
    const text = input.trim();
    if (!text) return;

    const userMsg: Message = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      mode,
      timestamp: new Date().toISOString(),
    };

    setMessages(prev => [...prev, userMsg]);
    setInput('');
    setLoading(true);

    // Mock AI response (生产环境替换为后端 API 调用)
    await new Promise(r => setTimeout(r, 800));
    const response = generateMockResponse(text, mode);

    setMessages(prev => [...prev, {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: response,
      timestamp: new Date().toISOString(),
    }]);
    setLoading(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      void send();
    }
  };

  return (
    <div className="aiwa-container">
      {/* Header */}
      <div className="aiwa-header">
        <Text weight="bold" size={500}>AIWA</Text>
        <Badge appearance="tint" color="brand">本地 Mock</Badge>
      </div>

      {/* Mode Selector */}
      <div className="aiwa-modes">
        {(Object.keys(MODE_LABELS) as AiwaMode[]).map(m => (
          <button
            key={m}
            className={`aiwa-mode-btn${mode === m ? ' active' : ''}`}
            onClick={() => setMode(m)}
          >
            {MODE_LABELS[m]}
          </button>
        ))}
      </div>

      {/* Messages */}
      <div className="aiwa-messages">
        {messages.map(msg => (
          <div key={msg.id} className={`aiwa-msg ${msg.role}`}>
            <div className="aiwa-msg-header">
              <Text size={100} weight="semibold">
                {msg.role === 'user' ? '你' : 'AIWA'}
                {msg.mode && ` · ${MODE_LABELS[msg.mode]}`}
              </Text>
            </div>
            <Text>{msg.content}</Text>
          </div>
        ))}
        {loading && <div className="aiwa-msg assistant"><Text>AIWA 思考中...</Text></div>}
        <div ref={endRef} />
      </div>

      {/* Input */}
      <div className="aiwa-input-area">
        <Textarea
          placeholder={`输入文本，AIWA 将帮你${MODE_LABELS[mode]}...`}
          value={input}
          onChange={(_, d) => setInput(d.value)}
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

// ─── Mock Response Generator ──────────────────────────────────────

function generateMockResponse(text: string, mode: AiwaMode): string {
  switch (mode) {
    case 'summarize':
      return `📝 **总结 (Mock)**\n\n这是对 "${text.substring(0, 30)}..." 的摘要：\n\n- 核心内容概括\n- 主要观点提炼\n- 关键数据点\n\n> ⚠️ 当前为本地 Mock 响应。连接 LLM API 后提供真实 AI 总结。`;
    case 'elaborate':
      return `📖 **展开 (Mock)**\n\n基于 "${text.substring(0, 30)}..." 的详细阐述：\n\n1. 背景说明\n2. 详细分析\n3. 示例与推导\n4. 结论\n\n> ⚠️ 当前为本地 Mock 响应。`;
    case 'rephrase':
      return `✏️ **改写 (Mock)**\n\n原文: "${text.substring(0, 50)}${text.length > 50 ? '...' : ''}"\n\n改写版本 (更正式):\n> 待 LLM API 接入后提供真实改写。\n\n改写版本 (更简洁):\n> 待 LLM API 接入后提供真实改写。`;
    case 'proofread':
      return `🔍 **校对 (Mock)**\n\n对 "${text.substring(0, 50)}..." 的校对结果：\n\n- ✅ 整体结构合理\n- ⚠️ 可能存在的语法问题\n- 💡 用词建议\n\n> ⚠️ 当前为本地 Mock 响应。`;
    case 'translate':
      return `🌐 **翻译 (Mock)**\n\n源文本: "${text.substring(0, 50)}..."\n\nEnglish:\n> Mock translation — LLM API connection required.\n\n> ⚠️ 当前为本地 Mock 响应。`;
    default:
      return '未知模式。请选择总结/展开/改写/校对/翻译。';
  }
}
