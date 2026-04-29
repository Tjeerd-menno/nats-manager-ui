import { useState } from 'react';
import { Stack, Title, Group, Text, Table, TextInput, Badge, Card, Grid, Button } from '@mantine/core';
import { IconSearch, IconPlus } from '@tabler/icons-react';
import { useKvBucket, useKvKeys } from '../hooks/useKv';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { LoadingState } from '../../../shared/LoadingState';
import { KvKeyEditor } from './KvKeyEditor';
import { OpenRelationshipMapButton } from '../../relationships/components/OpenRelationshipMapButton';
import { formatBytes, formatDateTime } from '../../../shared/formatting';

interface KvBucketDetailProps {
  bucketName: string;
  onKeySelect: (key: string) => void;
}

export function KvBucketDetail({ bucketName, onKeySelect }: KvBucketDetailProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const [search, setSearch] = useState('');
  const [editorOpened, setEditorOpened] = useState(false);

  const { data: bucket, isLoading: bucketLoading } = useKvBucket(selectedEnvironmentId, bucketName);
  const { data: keysData, isLoading: keysLoading } = useKvKeys(selectedEnvironmentId, bucketName, search || undefined);

  if (bucketLoading || keysLoading) {
    return <LoadingState message="Loading bucket details..." />;
  }

  const keys = keysData?.items ?? [];

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={3}>{bucketName}</Title>
        {selectedEnvironmentId && (
          <OpenRelationshipMapButton
            environmentId={selectedEnvironmentId}
            resourceId={bucketName}
            resourceType="KvBucket"
          />
        )}
      </Group>

      {bucket && (
        <Grid>
          <Grid.Col span={3}><Card withBorder p="sm"><Text size="xs" c="dimmed">Keys</Text><Text fw={500}>{bucket.keyCount}</Text></Card></Grid.Col>
          <Grid.Col span={3}><Card withBorder p="sm"><Text size="xs" c="dimmed">Size</Text><Text fw={500}>{formatBytes(bucket.byteCount)}</Text></Card></Grid.Col>
          <Grid.Col span={3}><Card withBorder p="sm"><Text size="xs" c="dimmed">History</Text><Text fw={500}>{bucket.history}</Text></Card></Grid.Col>
          <Grid.Col span={3}><Card withBorder p="sm"><Text size="xs" c="dimmed">TTL</Text><Text fw={500}>{bucket.ttl ? `${bucket.ttl}s` : 'None'}</Text></Card></Grid.Col>
        </Grid>
      )}

      <Group>
        <TextInput
          placeholder="Search keys..."
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(e) => setSearch(e.currentTarget.value)}
          style={{ flex: 1, maxWidth: 400 }}
        />
        <Button leftSection={<IconPlus size={16} />} onClick={() => setEditorOpened(true)}>Add Key</Button>
      </Group>

      <KvKeyEditor opened={editorOpened} onClose={() => setEditorOpened(false)} bucketName={bucketName} />

      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Key</Table.Th>
            <Table.Th>Revision</Table.Th>
            <Table.Th>Operation</Table.Th>
            <Table.Th>Size</Table.Th>
            <Table.Th>Updated</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {keys.map((entry) => (
            <Table.Tr key={entry.key} onClick={() => onKeySelect(entry.key)} style={{ cursor: 'pointer' }}>
              <Table.Td><Text fw={500}>{entry.key}</Text></Table.Td>
              <Table.Td>{entry.revision}</Table.Td>
              <Table.Td>
                <Badge variant="light" size="sm" color={entry.operation === 'Put' ? 'green' : entry.operation === 'Del' ? 'red' : 'gray'}>
                  {entry.operation}
                </Badge>
              </Table.Td>
              <Table.Td>{formatBytes(entry.size)}</Table.Td>
              <Table.Td><Text size="sm" c="dimmed">{formatDateTime(entry.createdAt)}</Text></Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </Stack>
  );
}
