import { Stack, Title, Group, Text, Card, Badge, Table, Button, Code } from '@mantine/core';
import { modals } from '@mantine/modals';
import { IconPencil, IconTrash } from '@tabler/icons-react';
import { useState } from 'react';
import { useKvKey, useKvKeyHistory, useDeleteKvKey } from '../hooks/useKv';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { LoadingState } from '../../../shared/LoadingState';
import { KvKeyEditor } from './KvKeyEditor';

interface KvKeyDetailProps {
  bucketName: string;
  keyName: string;
}

function tryDecodeBase64(value: string | null): string {
  if (!value) return '(empty)';
  try {
    return atob(value);
  } catch {
    return value;
  }
}

export function KvKeyDetail({ bucketName, keyName }: KvKeyDetailProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const [editorOpened, setEditorOpened] = useState(false);

  const { data: entry, isLoading } = useKvKey(selectedEnvironmentId, bucketName, keyName);
  const { data: historyData, isLoading: historyLoading } = useKvKeyHistory(selectedEnvironmentId, bucketName, keyName);
  const deleteKey = useDeleteKvKey(selectedEnvironmentId, bucketName);

  if (isLoading) {
    return <LoadingState message="Loading key details..." />;
  }

  if (!entry) {
    return <Text>Key not found</Text>;
  }

  const handleDelete = () => {
    modals.openConfirmModal({
      title: 'Delete Key',
      children: <Text size="sm">Are you sure you want to delete key &quot;{keyName}&quot;? This action cannot be undone.</Text>,
      labels: { confirm: 'Delete', cancel: 'Cancel' },
      confirmProps: { color: 'red' },
      onConfirm: () => deleteKey.mutate(keyName),
    });
  };

  const decodedValue = tryDecodeBase64(entry.value);

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={3}>{keyName}</Title>
        <Group>
          <Button leftSection={<IconPencil size={16} />} variant="light" onClick={() => setEditorOpened(true)}>
            Edit
          </Button>
          <Button leftSection={<IconTrash size={16} />} color="red" variant="light" onClick={handleDelete}>
            Delete
          </Button>
        </Group>
      </Group>

      <KvKeyEditor
        opened={editorOpened}
        onClose={() => setEditorOpened(false)}
        bucketName={bucketName}
        editKey={keyName}
        editValue={decodedValue}
        editRevision={entry.revision}
      />

      <Card withBorder p="md">
        <Stack gap="xs">
          <Group>
            <Text size="sm" c="dimmed" w={120}>Revision:</Text>
            <Badge variant="light">{entry.revision}</Badge>
          </Group>
          <Group>
            <Text size="sm" c="dimmed" w={120}>Operation:</Text>
            <Badge
              variant="light"
              color={entry.operation === 'Put' ? 'green' : entry.operation === 'Del' ? 'red' : 'gray'}
            >
              {entry.operation}
            </Badge>
          </Group>
          <Group>
            <Text size="sm" c="dimmed" w={120}>Size:</Text>
            <Text size="sm">{entry.size} bytes</Text>
          </Group>
          <Group>
            <Text size="sm" c="dimmed" w={120}>Updated:</Text>
            <Text size="sm">{new Date(entry.createdAt).toLocaleString()}</Text>
          </Group>
        </Stack>
      </Card>

      <Stack gap="xs">
        <Text fw={500}>Value</Text>
        <Code block style={{ maxHeight: 300, overflow: 'auto' }}>
          {decodedValue}
        </Code>
      </Stack>

      <Stack gap="xs">
        <Text fw={500}>History</Text>
        {historyLoading ? (
          <LoadingState message="Loading history..." />
        ) : (
          <Table striped>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Revision</Table.Th>
                <Table.Th>Operation</Table.Th>
                <Table.Th>Size</Table.Th>
                <Table.Th>Timestamp</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {(historyData?.entries ?? []).map((h) => (
                <Table.Tr key={h.revision}>
                  <Table.Td>{h.revision}</Table.Td>
                  <Table.Td>
                    <Badge
                      variant="light"
                      size="sm"
                      color={h.operation === 'Put' ? 'green' : h.operation === 'Del' ? 'red' : 'gray'}
                    >
                      {h.operation}
                    </Badge>
                  </Table.Td>
                  <Table.Td>{h.size} bytes</Table.Td>
                  <Table.Td>{new Date(h.createdAt).toLocaleString()}</Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        )}
      </Stack>
    </Stack>
  );
}
