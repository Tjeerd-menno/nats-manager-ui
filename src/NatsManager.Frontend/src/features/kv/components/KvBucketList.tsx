import { useState } from 'react';
import { Table, TextInput, Group, Text, Stack, Badge, Button, ActionIcon } from '@mantine/core';
import { modals } from '@mantine/modals';
import { IconSearch, IconPlus, IconTrash } from '@tabler/icons-react';
import { useKvBuckets, useDeleteKvBucket } from '../hooks/useKv';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { LoadingState } from '../../../shared/LoadingState';
import { EmptyState } from '../../../shared/EmptyState';
import { KvBucketForm } from './KvBucketForm';

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i] ?? 'B'}`;
}

interface KvBucketListProps {
  onSelect: (bucketName: string) => void;
}

export function KvBucketList({ onSelect }: KvBucketListProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const [search, setSearch] = useState('');
  const [formOpened, setFormOpened] = useState(false);

  const { data: buckets, isLoading } = useKvBuckets(selectedEnvironmentId);
  const deleteBucket = useDeleteKvBucket(selectedEnvironmentId);

  if (!selectedEnvironmentId) {
    return <EmptyState message="Select an environment to view KV buckets" />;
  }

  if (isLoading) {
    return <LoadingState message="Loading buckets..." />;
  }

  const filtered = (buckets ?? []).filter(
    (b) => !search || b.bucketName.toLowerCase().includes(search.toLowerCase()),
  );

  if (filtered.length === 0 && !search) {
    return (
      <Stack>
        <Group>
          <Button leftSection={<IconPlus size={16} />} onClick={() => setFormOpened(true)}>
            Create Bucket
          </Button>
        </Group>
        <KvBucketForm opened={formOpened} onClose={() => setFormOpened(false)} />
        <EmptyState message="No KV buckets found in this environment" />
      </Stack>
    );
  }

  const handleDelete = (bucketName: string) => {
    modals.openConfirmModal({
      title: 'Delete Bucket',
      children: <Text size="sm">Are you sure you want to delete bucket &quot;{bucketName}&quot;? This action cannot be undone.</Text>,
      labels: { confirm: 'Delete', cancel: 'Cancel' },
      confirmProps: { color: 'red' },
      onConfirm: () => deleteBucket.mutate(bucketName),
    });
  };

  return (
    <Stack>
      <Group>
        <TextInput
          placeholder="Search buckets..."
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(e) => setSearch(e.currentTarget.value)}
          style={{ flex: 1, maxWidth: 400 }}
        />
        <Button leftSection={<IconPlus size={16} />} onClick={() => setFormOpened(true)}>
          Create Bucket
        </Button>
      </Group>

      <KvBucketForm opened={formOpened} onClose={() => setFormOpened(false)} />

      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Keys</Table.Th>
            <Table.Th>Size</Table.Th>
            <Table.Th>History</Table.Th>
            <Table.Th>TTL</Table.Th>
            <Table.Th>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {filtered.map((bucket) => (
            <Table.Tr
              key={bucket.bucketName}
              onClick={() => onSelect(bucket.bucketName)}
              style={{ cursor: 'pointer' }}
            >
              <Table.Td>
                <Text fw={500}>{bucket.bucketName}</Text>
              </Table.Td>
              <Table.Td>{bucket.keyCount}</Table.Td>
              <Table.Td>{formatBytes(bucket.byteCount)}</Table.Td>
              <Table.Td>
                <Badge variant="light" size="sm">{bucket.history}</Badge>
              </Table.Td>
              <Table.Td>
                {bucket.ttl ? `${bucket.ttl}s` : <Text c="dimmed" size="sm">None</Text>}
              </Table.Td>
              <Table.Td>
                <ActionIcon
                  variant="subtle"
                  color="red"
                  onClick={(e) => { e.stopPropagation(); handleDelete(bucket.bucketName); }}
                >
                  <IconTrash size={16} />
                </ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </Stack>
  );
}
