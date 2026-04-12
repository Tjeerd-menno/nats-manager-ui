import { useState } from 'react';
import {
  Table,
  TextInput,
  Group,
  Button,
  ActionIcon,
  Badge,
  Text,
  Pagination,
  Stack,
  Menu,
} from '@mantine/core';
import { IconSearch, IconPlus, IconDotsVertical, IconTrash, IconEdit, IconPlugConnected, IconPlayerPlay, IconPlayerPause } from '@tabler/icons-react';
import { useEnvironments, useDeleteEnvironment, useTestConnection, useEnableDisableEnvironment } from '../hooks/useEnvironments';
import { ConnectionStatusBadge } from './ConnectionStatusBadge';
import { LoadingState } from '../../../shared/LoadingState';
import { EmptyState } from '../../../shared/EmptyState';
import type { EnvironmentListItem } from '../types';

interface EnvironmentListProps {
  onEdit: (env: EnvironmentListItem) => void;
  onCreate: () => void;
  onSelect: (env: EnvironmentListItem) => void;
}

export function EnvironmentList({ onEdit, onCreate, onSelect }: EnvironmentListProps) {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 25;

  const { data, isLoading } = useEnvironments({ page, pageSize, search: search || undefined });
  const deleteMutation = useDeleteEnvironment();
  const testMutation = useTestConnection();
  const enableDisableMutation = useEnableDisableEnvironment();

  if (isLoading) {
    return <LoadingState message="Loading environments..." />;
  }

  const environments = data?.data.items ?? [];
  const totalPages = data?.data.totalPages ?? 1;

  if (environments.length === 0 && !search) {
    return (
      <EmptyState
        message="No environments registered yet"
        action={<Button leftSection={<IconPlus size={16} />} onClick={onCreate}>Register Environment</Button>}
      />
    );
  }

  return (
    <Stack>
      <Group justify="space-between">
        <TextInput
          placeholder="Search environments..."
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(e) => {
            setSearch(e.currentTarget.value);
            setPage(1);
          }}
          style={{ flex: 1, maxWidth: 400 }}
        />
        <Button leftSection={<IconPlus size={16} />} onClick={onCreate}>
          Register Environment
        </Button>
      </Group>

      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Description</Table.Th>
            <Table.Th>Status</Table.Th>
            <Table.Th>Type</Table.Th>
            <Table.Th>Last Contact</Table.Th>
            <Table.Th>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {environments.map((env) => (
            <Table.Tr key={env.id} style={{ cursor: 'pointer' }} onClick={() => onSelect(env)}>
              <Table.Td>
                <Text fw={500}>{env.name}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm" c="dimmed" lineClamp={1}>{env.description}</Text>
              </Table.Td>
              <Table.Td>
                <ConnectionStatusBadge status={env.connectionStatus} />
              </Table.Td>
              <Table.Td>
                <Group gap="xs">
                  {env.isProduction && <Badge color="red" variant="light" size="xs">PROD</Badge>}
                  {!env.isEnabled && <Badge color="gray" variant="light" size="xs">Disabled</Badge>}
                </Group>
              </Table.Td>
              <Table.Td>
                <Text size="sm" c="dimmed">
                  {env.lastSuccessfulContact
                    ? new Date(env.lastSuccessfulContact).toLocaleString()
                    : 'Never'}
                </Text>
              </Table.Td>
              <Table.Td onClick={(e) => e.stopPropagation()}>
                <Menu position="bottom-end" withArrow>
                  <Menu.Target>
                    <ActionIcon variant="subtle">
                      <IconDotsVertical size={16} />
                    </ActionIcon>
                  </Menu.Target>
                  <Menu.Dropdown>
                    <Menu.Item
                      leftSection={<IconPlugConnected size={14} />}
                      onClick={() => testMutation.mutate(env.id)}
                    >
                      Test Connection
                    </Menu.Item>
                    <Menu.Item
                      leftSection={<IconEdit size={14} />}
                      onClick={() => onEdit(env)}
                    >
                      Edit
                    </Menu.Item>
                    <Menu.Item
                      leftSection={env.isEnabled ? <IconPlayerPause size={14} /> : <IconPlayerPlay size={14} />}
                      onClick={() => enableDisableMutation.mutate({ id: env.id, enable: !env.isEnabled })}
                    >
                      {env.isEnabled ? 'Disable' : 'Enable'}
                    </Menu.Item>
                    <Menu.Divider />
                    <Menu.Item
                      color="red"
                      leftSection={<IconTrash size={14} />}
                      onClick={() => deleteMutation.mutate(env.id)}
                    >
                      Delete
                    </Menu.Item>
                  </Menu.Dropdown>
                </Menu>
              </Table.Td>
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
