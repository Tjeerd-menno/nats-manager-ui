import { useState } from 'react';
import { Paper, SegmentedControl, Code, Text, ScrollArea, Badge, Group } from '@mantine/core';

interface PayloadViewerProps {
  data: string | null | undefined;
  contentType?: string;
  maxLength?: number;
}

function detectContentType(data: string): string {
  const trimmed = data.trim();
  if (trimmed.startsWith('{') || trimmed.startsWith('[')) return 'json';
  if (trimmed.startsWith('<')) return 'xml';
  return 'text';
}

const SENSITIVE_PATTERNS = /("(?:password|secret|token|api[_-]?key|authorization|credential|private[_-]?key)")\s*:\s*"[^"]*"/gi;

function maskCredentials(text: string): string {
  return text.replace(SENSITIVE_PATTERNS, '$1: "***REDACTED***"');
}

export function PayloadViewer({ data, contentType, maxLength = 10000 }: PayloadViewerProps) {
  const [mode, setMode] = useState<string>('structured');

  if (data == null || data.length === 0) {
    return <Text size="sm" c="dimmed">No payload data</Text>;
  }

  const isTruncated = data.length > maxLength;
  const displayData = isTruncated ? data.slice(0, maxLength) : data;
  const maskedData = maskCredentials(displayData);
  const detected = contentType ?? detectContentType(maskedData);

  const renderStructured = () => {
    if (detected === 'json') {
      try {
        const parsed = JSON.parse(maskedData);
        return <Code block>{JSON.stringify(parsed, null, 2)}</Code>;
      } catch {
        return <Code block>{maskedData}</Code>;
      }
    }
    return <Code block>{maskedData}</Code>;
  };

  return (
    <Paper p="sm" withBorder>
      <Group justify="space-between" mb="xs">
        <Group gap="xs">
          <Badge size="sm" variant="light">{detected}</Badge>
          {isTruncated && (
            <Badge size="sm" color="orange" variant="light">
              Truncated ({data.length.toLocaleString()} bytes)
            </Badge>
          )}
        </Group>
        <SegmentedControl
          size="xs"
          value={mode}
          onChange={setMode}
          data={[
            { label: 'Structured', value: 'structured' },
            { label: 'Raw', value: 'raw' },
          ]}
        />
      </Group>
      <ScrollArea.Autosize mah={400}>
        {mode === 'structured' ? renderStructured() : <Code block>{maskedData}</Code>}
      </ScrollArea.Autosize>
    </Paper>
  );
}
