import { useState } from 'react';
import { Title, Table, Badge, Stack, Group, Select, Pagination, Text, Loader, Center, Code } from '@mantine/core';
import { useAuditEvents } from './hooks/useAudit';
import { formatDateTime } from '../../shared/formatting';

const ACTION_TYPES = ['Create', 'Update', 'Delete', 'TestInvoke', 'Publish', 'Subscribe', 'Login', 'Logout', 'PermissionChange'];
const RESOURCE_TYPES = ['Environment', 'Stream', 'Consumer', 'KvBucket', 'KvKey', 'ObjectBucket', 'ObjectItem', 'Service', 'User', 'Role'];

export default function AuditPage() {
  const [page, setPage] = useState(1);
  const [actionType, setActionType] = useState<string | null>(null);
  const [resourceType, setResourceType] = useState<string | null>(null);

  const { data, isLoading } = useAuditEvents({
    page,
    pageSize: 50,
    actionType: actionType ?? undefined,
    resourceType: resourceType ?? undefined,
  });

  return (
    <Stack>
      <Title order={2}>Audit Log</Title>

      <Group>
        <Select placeholder="Action type" data={ACTION_TYPES} value={actionType} onChange={setActionType} clearable style={{ width: 200 }} />
        <Select placeholder="Resource type" data={RESOURCE_TYPES} value={resourceType} onChange={setResourceType} clearable style={{ width: 200 }} />
      </Group>

      {isLoading ? (
        <Center h={200}><Loader /></Center>
      ) : (
        <>
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Timestamp</Table.Th>
                <Table.Th>Actor</Table.Th>
                <Table.Th>Action</Table.Th>
                <Table.Th>Resource</Table.Th>
                <Table.Th>Target</Table.Th>
                <Table.Th>Outcome</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data?.items.map((event) => (
                <Table.Tr key={event.id}>
                  <Table.Td><Text size="sm">{formatDateTime(event.timestamp)}</Text></Table.Td>
                  <Table.Td>{event.actorName}</Table.Td>
                  <Table.Td><Badge size="sm" variant="light">{event.actionType}</Badge></Table.Td>
                  <Table.Td><Badge size="sm" variant="outline">{event.resourceType}</Badge></Table.Td>
                  <Table.Td><Code>{event.resourceName}</Code></Table.Td>
                  <Table.Td><Badge size="sm" color={event.outcome === 'Success' ? 'green' : event.outcome === 'Failure' ? 'red' : 'yellow'}>{event.outcome}</Badge></Table.Td>
                </Table.Tr>
              ))}
              {data?.items.length === 0 && <Table.Tr><Table.Td colSpan={6}><Text c="dimmed" ta="center">No audit events found</Text></Table.Td></Table.Tr>}
            </Table.Tbody>
          </Table>

          {data && data.totalCount > data.pageSize && (
            <Center>
              <Pagination total={Math.ceil(data.totalCount / data.pageSize)} value={page} onChange={setPage} />
            </Center>
          )}
        </>
      )}
    </Stack>
  );
}
