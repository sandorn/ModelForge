import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Button, Text, Title3, Card, CardHeader } from '@fluentui/react-components';

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

/**
 * React 错误边界组件。
 * 捕获子组件树中的渲染错误，防止整个应用崩溃。
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    this.setState({ errorInfo });
    console.error('[ErrorBoundary] 未捕获的渲染错误:', error, errorInfo.componentStack);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          height: '100vh', background: '#f5f7fb',
        }}>
          <Card style={{ width: 400, padding: 24 }}>
            <CardHeader header={<Title3>出现错误</Title3>} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <Text size={300}>
                应用发生了意外错误。请尝试刷新页面。
              </Text>
              {this.state.error && (
                <Text size={100} style={{ color: '#888', wordBreak: 'break-word' }}>
                  {this.state.error.message}
                </Text>
              )}
              <Button appearance="primary" onClick={this.handleReset}>
                重试
              </Button>
            </div>
          </Card>
        </div>
      );
    }

    return this.props.children;
  }
}
