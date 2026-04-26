import { useState } from 'react';
import {
  Stack,
  TextInput,
  Textarea,
  SegmentedControl,
  Button,
  Group,
  ActionIcon,
  Text,
  Notification,
} from '@mantine/core';
import { IconTrash, IconPlus, IconCheck, IconX } from '@tabler/icons-react';
import type { PayloadFormat, PublishRequest } from '../types';
import { usePublishMessage } from '../hooks/useCoreNats';

function isValidJson(value: string): boolean {
  try {
    JSON.parse(value);
    return true;
  } catch {
    return false;
  }
}

function isValidHexBytes(value: string): boolean {
  return value.length % 2 === 0 && /^[0-9a-fA-F]*$/.test(value);
}

interface HeaderRow {
  id: string;
  key: string;
  value: string;
}

interface PublishMessageFormProps {
  environmentId: string;
}

export function PublishMessageForm({ environmentId }: PublishMessageFormProps) {
  const [subject, setSubject] = useState('');
  const [payload, setPayload] = useState('');
  const [payloadFormat, setPayloadFormat] = useState<PayloadFormat>('PlainText');
  const [replyTo, setReplyTo] = useState('');
  const [headers, setHeaders] = useState<HeaderRow[]>([]);
  const [showSuccess, setShowSuccess] = useState(false);
  const [showError, setShowError] = useState(false);

  const publishMutation = usePublishMessage(environmentId);

  const jsonError = payloadFormat === 'Json' && payload.length > 0 && !isValidJson(payload);
  const hexError = payloadFormat === 'HexBytes' && payload.length > 0 && !isValidHexBytes(payload);
  const emptyKeyError = headers.some((h) => h.key.trim() === '');
  const normalizedKeys = headers
    .map((h) => h.key.trim().toLowerCase())
    .filter((key) => key.length > 0);
  const duplicateKeys = new Set(normalizedKeys.filter((key, index) => normalizedKeys.indexOf(key) !== index));
  const duplicateKeyError = duplicateKeys.size > 0;

  const isSubmitDisabled =
    !subject || publishMutation.isPending || jsonError || hexError || emptyKeyError || duplicateKeyError;

  const addHeader = () =>
    setHeaders((prev) => [...prev, { id: crypto.randomUUID(), key: '', value: '' }]);

  const removeHeader = (id: string) =>
    setHeaders((prev) => prev.filter((h) => h.id !== id));

  const updateHeader = (id: string, field: 'key' | 'value', newValue: string) =>
    setHeaders((prev) => prev.map((h) => (h.id === id ? { ...h, [field]: newValue } : h)));

  const handlePublish = () => {
    setShowSuccess(false);
    setShowError(false);

    const headersMap: Record<string, string> = {};
    for (const h of headers) {
      const key = h.key.trim();
      if (key) headersMap[key] = h.value;
    }

    const request: PublishRequest = {
      subject,
      payload: payload || undefined,
      payloadFormat,
      headers: headersMap,
      replyTo: replyTo || undefined,
    };

    publishMutation.mutate(request, {
      onSuccess: () => setShowSuccess(true),
      onError: () => setShowError(true),
    });
  };

  return (
    <Stack>
      <TextInput
        label="Subject"
        placeholder="e.g. orders.created"
        value={subject}
        onChange={(e) => setSubject(e.currentTarget.value)}
        required
      />

      <div>
        <Text size="sm" fw={500} mb={4}>
          Payload Format
        </Text>
        <SegmentedControl
          value={payloadFormat}
          onChange={(v) => setPayloadFormat(v as PayloadFormat)}
          data={[
            { label: 'Plain Text', value: 'PlainText' },
            { label: 'JSON', value: 'Json' },
            { label: 'Hex Bytes', value: 'HexBytes' },
          ]}
          fullWidth
        />
      </div>

      <Textarea
        label="Payload"
        placeholder={
          payloadFormat === 'Json'
            ? '{"key":"value"}'
            : payloadFormat === 'HexBytes'
              ? '48656c6c6f'
              : 'Message content'
        }
        value={payload}
        onChange={(e) => setPayload(e.currentTarget.value)}
        minRows={3}
        error={
          jsonError
            ? 'Payload is not valid JSON'
            : hexError
              ? 'Payload is not valid hex bytes'
              : undefined
        }
      />

      <TextInput
        label="Reply-To (optional)"
        placeholder="e.g. orders.reply"
        value={replyTo}
        onChange={(e) => setReplyTo(e.currentTarget.value)}
      />

      <Stack gap="xs">
        <Group justify="space-between">
          <Text size="sm" fw={500}>
            Headers
          </Text>
          <Button
            variant="subtle"
            size="xs"
            leftSection={<IconPlus size={14} />}
            onClick={addHeader}
          >
            Add Header
          </Button>
        </Group>

        {headers.map((h) => (
          <Group key={h.id} gap="xs" align="flex-start">
            <TextInput
              placeholder="Key"
              value={h.key}
              onChange={(e) => updateHeader(h.id, 'key', e.currentTarget.value)}
              error={
                h.key.trim() === ''
                  ? 'Key required'
                  : duplicateKeys.has(h.key.trim().toLowerCase())
                    ? 'Duplicate key'
                    : undefined
              }
              style={{ flex: 1 }}
            />
            <TextInput
              placeholder="Value"
              value={h.value}
              onChange={(e) => updateHeader(h.id, 'value', e.currentTarget.value)}
              style={{ flex: 2 }}
            />
            <ActionIcon
              color="red"
              variant="subtle"
              onClick={() => removeHeader(h.id)}
              aria-label="Remove header"
              mt={4}
            >
              <IconTrash size={16} />
            </ActionIcon>
          </Group>
        ))}
      </Stack>

      <Button onClick={handlePublish} loading={publishMutation.isPending} disabled={isSubmitDisabled}>
        Publish
      </Button>

      {showSuccess && (
        <Notification
          icon={<IconCheck size={18} />}
          color="green"
          title="Message published"
          onClose={() => setShowSuccess(false)}
        >
          Message published successfully.
        </Notification>
      )}

      {showError && (
        <Notification
          icon={<IconX size={18} />}
          color="red"
          title="Publish failed"
          onClose={() => setShowError(false)}
        >
          {(publishMutation.error as Error | null)?.message ?? 'An error occurred.'}
        </Notification>
      )}
    </Stack>
  );
}
