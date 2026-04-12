import { useState } from 'react';
import { Table, TextInput, Group, Text, Pagination, Stack, Badge, Button } from '@mantine/core';
import { IconSearch, IconPlus } from '@tabler/icons-react';
import { useStreams } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { LoadingState } from '../../../shared/LoadingState';
import { EmptyState } from '../../../shared/EmptyState';
import { StreamForm } from './StreamForm';

interface StreamListProps {
  onSelect: (streamName: string) => void;
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i] ?? 'B'}`;
}

export function StreamList({ onSelect }: StreamListProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [formOpened, setFormOpened] = useState(false);

  const { data, isLoading } = useStreams(selectedEnvironmentId, { page, pageSize: 25, search: search || undefined });

  if (!selectedEnvironmentId) {
    return <EmptyState message="Select an environment to view streams" />;
  }

  if (isLoading) {
    return <LoadingState message="Loading streams..." />;
  }

  const streams = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  if (streams.length === 0 && !search) {
    return (
      <Stack>
        <Group justify="flex-end">
          <Button leftSection={<IconPlus size={16} />} onClick={() => setFormOpened(true)}>
            Create Stream
          </Button>
        </Group>
        <StreamForm opened={formOpened} onClose={() => setFormOpened(false)} />
        <EmptyState message="No streams found in this environment" />
      </Stack>
    );
  }

  return (
    <Stack>
      <Group>
        <TextInput
          placeholder="Search streams..."
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(e) => { setSearch(e.currentTarget.value); setPage(1); }}
          style={{ flex: 1, maxWidth: 400 }}
        />
        <Button leftSection={<IconPlus size={16} />} onClick={() => setFormOpened(true)}>
          Create Stream
        </Button>
      </Group>

      <StreamForm opened={formOpened} onClose={() => setFormOpened(false)} />

      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Subjects</Table.Th>
            <Table.Th>Retention</Table.Th>
            <Table.Th>Messages</Table.Th>
            <Table.Th>Size</Table.Th>
            <Table.Th>Consumers</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {streams.map((stream) => (
            <Table.Tr key={stream.name} style={{ cursor: 'pointer' }} onClick={() => onSelect(stream.name)}>
              <Table.Td><Text fw={500}>{stream.name}</Text></Table.Td>
              <Table.Td>
                <Group gap={4}>
                  {stream.subjects.slice(0, 3).map((s) => (
                    <Badge key={s} variant="light" size="xs">{s}</Badge>
                  ))}
                  {stream.subjects.length > 3 && <Badge variant="light" size="xs">+{stream.subjects.length - 3}</Badge>}
                </Group>
              </Table.Td>
              <Table.Td><Badge variant="outline" size="sm">{stream.retentionPolicy}</Badge></Table.Td>
              <Table.Td>{stream.messages.toLocaleString()}</Table.Td>
              <Table.Td>{formatBytes(stream.bytes)}</Table.Td>
              <Table.Td>{stream.consumerCount}</Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      {totalPages > 1 && (
        <Group justify="center">
          <Pagination value={page} onChange={setPage} total={totalPages} />
        </Group>
      )}
    </Stack>
  );
}
