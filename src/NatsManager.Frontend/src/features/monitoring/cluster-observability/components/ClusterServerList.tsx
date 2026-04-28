import { useState } from 'react';
import { Table, Badge, Text, TextInput, Stack, LoadingOverlay, Box } from '@mantine/core';
import { IconSearch } from '@tabler/icons-react';
import type { ServerStatus } from '../types';
import { useClusterServers } from '../hooks/useClusterServers';
import { OpenRelationshipMapButton } from '../../../relationships/components/OpenRelationshipMapButton';

function statusColor(s: ServerStatus): string {
  switch (s) {
    case 'Healthy': return 'green';
    case 'Warning': return 'yellow';
    case 'Stale': return 'orange';
    case 'Unavailable': return 'red';
    default: return 'gray';
  }
}

function formatMemory(bytes: number | null): string {
  if (bytes === null) return '—';
  if (bytes >= 1_073_741_824) return `${(bytes / 1_073_741_824).toFixed(1)} GB`;
  return `${(bytes / 1_048_576).toFixed(1)} MB`;
}

function formatRate(rate: number | null): string {
  if (rate === null) return '—';
  return `${rate.toFixed(1)}/s`;
}

function formatDate(iso: string): string {
  try {
    const date = new Date(iso);
    const diffMs = Date.now() - date.getTime();
    const diffSec = Math.floor(diffMs / 1000);
    if (diffSec < 60) return `${diffSec}s ago`;
    if (diffSec < 3600) return `${Math.floor(diffSec / 60)}m ago`;
    return date.toLocaleString();
  } catch {
    return iso;
  }
}

interface ClusterServerListProps {
  envId: string;
}

export function ClusterServerList({ envId }: ClusterServerListProps) {
  const [search, setSearch] = useState('');
  const { servers, isLoading } = useClusterServers(envId, { search });

  const rows = servers.map(s => (
    <Table.Tr key={s.serverId}>
      <Table.Td>
        <Badge color={statusColor(s.status)} variant="light">
          {s.status}
        </Badge>
      </Table.Td>
      <Table.Td>
        <Text size="sm" fw={500}>{s.serverName ?? s.serverId}</Text>
        {s.serverName && (
          <Text size="xs" c="dimmed">{s.serverId}</Text>
        )}
      </Table.Td>
      <Table.Td>{s.version ?? '—'}</Table.Td>
      <Table.Td>{s.connections ?? '—'}</Table.Td>
      <Table.Td>{s.slowConsumers ?? '—'}</Table.Td>
      <Table.Td>{formatMemory(s.memoryBytes)}</Table.Td>
      <Table.Td>{formatRate(s.inMsgsPerSecond)}</Table.Td>
      <Table.Td>{formatRate(s.outMsgsPerSecond)}</Table.Td>
      <Table.Td>{formatDate(s.lastObservedAt)}</Table.Td>
      <Table.Td>
        <OpenRelationshipMapButton
          environmentId={envId}
          resourceId={s.serverId}
          resourceType="Server"
          label="Map"
        />
      </Table.Td>
    </Table.Tr>
  ));

  return (
    <Stack gap="sm">
      <TextInput
        placeholder="Search by server ID or name…"
        leftSection={<IconSearch size={14} />}
        value={search}
        onChange={e => setSearch(e.currentTarget.value)}
        w={320}
      />
      <Box pos="relative">
        <LoadingOverlay visible={isLoading} />
        {!isLoading && servers.length === 0 ? (
          <Text c="dimmed" ta="center" py="xl">
            {search ? 'No servers match your search.' : 'No servers available.'}
          </Text>
        ) : (
          <Table striped highlightOnHover withTableBorder withColumnBorders>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Status</Table.Th>
                <Table.Th>Name / ID</Table.Th>
                <Table.Th>Version</Table.Th>
                <Table.Th>Connections</Table.Th>
                <Table.Th>Slow Consumers</Table.Th>
                <Table.Th>Memory</Table.Th>
                <Table.Th>In Msgs/s</Table.Th>
                <Table.Th>Out Msgs/s</Table.Th>
                <Table.Th>Last Observed</Table.Th>
                <Table.Th>Relationships</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>{rows}</Table.Tbody>
          </Table>
        )}
      </Box>
    </Stack>
  );
}
