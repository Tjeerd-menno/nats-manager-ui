import { useState } from 'react';
import {
  Stack,
  Group,
  TextInput,
  Button,
  Badge,
  NumberInput,
  Table,
  Text,
  Alert,
  ActionIcon,
} from '@mantine/core';
import { IconPlayerPlay, IconPlayerPause, IconTrash, IconChevronDown, IconChevronRight } from '@tabler/icons-react';
import { PayloadViewer } from '../../../shared/PayloadViewer';
import { useLiveMessages } from '../hooks/useCoreNats';
import type { NatsLiveMessage } from '../types';
import { EmptyState } from '../../../shared/EmptyState';

function decodeBase64Bytes(base64: string): Uint8Array {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

function bytesToHex(bytes: Uint8Array): string {
  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join(' ');
}

function formatPayload(msg: NatsLiveMessage): string {
  const bytes = decodeBase64Bytes(msg.payloadBase64);
  if (msg.isBinary) {
    const hex = bytesToHex(bytes);
    const preview = hex.length > 80 ? `${hex.slice(0, 80)}…` : hex;
    return `Binary payload (${msg.payloadSize} bytes, hex): ${preview}`;
  }
  try {
    const text = new TextDecoder().decode(bytes);
    return text.length > 80 ? text.slice(0, 80) + '…' : text;
  } catch {
    return `<${msg.payloadSize} bytes>`;
  }
}

function decodePayload(msg: NatsLiveMessage): string {
  const bytes = decodeBase64Bytes(msg.payloadBase64);
  if (msg.isBinary) {
    return bytesToHex(bytes);
  }
  try {
    return new TextDecoder().decode(bytes);
  } catch {
    return msg.payloadBase64;
  }
}

function MessageRow({ msg }: { msg: NatsLiveMessage }) {
  const [expanded, setExpanded] = useState(false);
  const headerCount = Object.keys(msg.headers).length;

  return (
    <>
      <Table.Tr
        style={{ cursor: 'pointer' }}
        onClick={() => setExpanded((e) => !e)}
      >
        <Table.Td>
          <ActionIcon variant="subtle" size="xs">
            {expanded ? <IconChevronDown size={12} /> : <IconChevronRight size={12} />}
          </ActionIcon>
        </Table.Td>
        <Table.Td>
          <Text ff="monospace" size="sm">
            {msg.subject}
          </Text>
        </Table.Td>
        <Table.Td>
          <Text size="xs" c="dimmed">
            {new Date(msg.receivedAt).toLocaleTimeString()}
          </Text>
        </Table.Td>
        <Table.Td>
          <Text size="sm" style={{ maxWidth: 300, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {formatPayload(msg)}
          </Text>
        </Table.Td>
        <Table.Td>
          {headerCount > 0 && (
            <Badge size="xs" variant="light">
              {headerCount}
            </Badge>
          )}
        </Table.Td>
      </Table.Tr>

      {expanded && (
        <Table.Tr>
          <Table.Td colSpan={5} style={{ background: 'var(--mantine-color-default-hover)' }}>
            <Stack p="xs" gap="xs">
              {msg.isBinary && (
                <Text size="xs" fw={600}>
                  Binary payload rendered as hex bytes
                </Text>
              )}
              <PayloadViewer data={decodePayload(msg)} contentType={msg.isBinary ? 'hex' : undefined} />
              {headerCount > 0 && (
                <div>
                  <Text size="xs" fw={600} mb={4}>
                    Headers
                  </Text>
                  {Object.entries(msg.headers).map(([key, value]) => (
                    <Group key={key} gap="xs">
                      <Text size="xs" ff="monospace" fw={600}>
                        {key}:
                      </Text>
                      <Text size="xs" ff="monospace">
                        {value}
                      </Text>
                    </Group>
                  ))}
                </div>
              )}
              {msg.replyTo && (
                <Group gap="xs">
                  <Text size="xs" fw={600}>
                    Reply-To:
                  </Text>
                  <Text size="xs" ff="monospace">
                    {msg.replyTo}
                  </Text>
                </Group>
              )}
            </Stack>
          </Table.Td>
        </Table.Tr>
      )}
    </>
  );
}

interface LiveMessageViewerProps {
  environmentId: string;
}

export function LiveMessageViewer({ environmentId }: LiveMessageViewerProps) {
  const [subjectInput, setSubjectInput] = useState('');
  const [subjectError, setSubjectError] = useState<string | undefined>();
  const {
    messages,
    isConnected,
    isPaused,
    pendingCount,
    cap,
    setCap,
    subscribe,
    unsubscribe,
    pause,
    resume,
    clear,
  } = useLiveMessages(environmentId);

  const handleSubscribe = () => {
    if (!subjectInput.trim()) {
      setSubjectError('Subject pattern is required');
      return;
    }
    if (subjectInput.includes(' ')) {
      setSubjectError('Subject pattern must not contain spaces');
      return;
    }
    setSubjectError(undefined);
    subscribe(subjectInput.trim());
  };

  const handleUnsubscribe = () => {
    unsubscribe();
  };

  return (
    <Stack>
      <Group gap="sm">
        <TextInput
          placeholder="Subject pattern, e.g. orders.>"
          value={subjectInput}
          onChange={(e) => {
            setSubjectInput(e.currentTarget.value);
            setSubjectError(undefined);
          }}
          error={subjectError}
          disabled={isConnected}
          style={{ flex: 1 }}
          aria-label="Subject pattern"
        />
        {!isConnected ? (
          <Button onClick={handleSubscribe}>Subscribe</Button>
        ) : (
          <Button color="red" variant="light" onClick={handleUnsubscribe}>
            Unsubscribe
          </Button>
        )}
        <Badge color={isConnected ? 'green' : 'gray'} variant="light">
          {isConnected ? 'Connected' : 'Disconnected'}
        </Badge>
      </Group>

      {subjectInput.includes(' ') && !isConnected && (
        <Alert color="orange" title="Invalid subject">
          Subject patterns must not contain spaces.
        </Alert>
      )}

      <Group gap="sm">
        <Group gap="xs">
          <Text size="sm">Cap:</Text>
          <NumberInput
            value={cap}
            onChange={(v) => setCap(Number(v))}
            min={100}
            max={500}
            step={50}
            style={{ width: 100 }}
            size="xs"
            aria-label="Message cap"
          />
        </Group>

        <Button
          size="xs"
          variant="light"
          leftSection={isPaused ? <IconPlayerPlay size={14} /> : <IconPlayerPause size={14} />}
          onClick={isPaused ? resume : pause}
          disabled={!isConnected}
        >
          {isPaused ? 'Resume' : 'Pause'}
          {isPaused && pendingCount > 0 && (
            <Badge size="xs" color="orange" ml={4}>
              {pendingCount}
            </Badge>
          )}
        </Button>

        <ActionIcon
          variant="light"
          color="gray"
          onClick={clear}
          title="Clear messages"
          aria-label="Clear messages"
        >
          <IconTrash size={16} />
        </ActionIcon>
      </Group>

      {messages.length === 0 ? (
        <EmptyState
          message={isConnected ? 'Waiting for messages…' : 'Subscribe to a subject to see live messages'}
        />
      ) : (
        <Table striped>
          <Table.Thead>
            <Table.Tr>
              <Table.Th style={{ width: 32 }} />
              <Table.Th>Subject</Table.Th>
              <Table.Th>Time</Table.Th>
              <Table.Th>Payload Preview</Table.Th>
              <Table.Th>Headers</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {messages.map((msg, i) => (
              <MessageRow key={`${msg.receivedAt}-${i}`} msg={msg} />
            ))}
          </Table.Tbody>
        </Table>
      )}
    </Stack>
  );
}
