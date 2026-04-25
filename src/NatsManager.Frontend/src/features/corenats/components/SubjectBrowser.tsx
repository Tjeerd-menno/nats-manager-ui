import { useState, useMemo, useRef } from 'react';
import { TextInput, Table, Alert, Text, Stack } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { LoadingState } from '../../../shared/LoadingState';
import { EmptyState } from '../../../shared/EmptyState';
import { useSubjects } from '../hooks/useCoreNats';

interface SubjectBrowserProps {
  environmentId: string;
}

export function SubjectBrowser({ environmentId }: SubjectBrowserProps) {
  const { data, isLoading, isMonitoringAvailable } = useSubjects(environmentId);
  const [filter, setFilter] = useState('');
  const [debouncedFilter, setDebouncedFilter] = useState('');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleFilterChange = (value: string) => {
    setFilter(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => setDebouncedFilter(value), 300);
  };

  const filtered = useMemo(
    () =>
      (data ?? []).filter((s) =>
        debouncedFilter ? s.subject.toLowerCase().includes(debouncedFilter.toLowerCase()) : true
      ),
    [data, debouncedFilter]
  );

  if (isLoading) return <LoadingState label="Loading subjects…" />;

  if (!isMonitoringAvailable) {
    return (
      <Alert icon={<IconInfoCircle size={16} />} title="Subject discovery unavailable" color="gray">
        Subject discovery is unavailable — monitoring endpoint not reachable for this server
        configuration. The rest of the page is still functional.
      </Alert>
    );
  }

  return (
    <Stack gap="sm">
      <TextInput
        placeholder="Filter subjects…"
        value={filter}
        onChange={(e) => handleFilterChange(e.currentTarget.value)}
        aria-label="Filter subjects"
      />

      {filtered.length === 0 ? (
        debouncedFilter ? (
          <EmptyState message="No subjects match your filter" />
        ) : (
          <EmptyState message="No active subscriptions found" />
        )
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Subject</Table.Th>
              <Table.Th>Subscriptions</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {filtered.map((s) => (
              <Table.Tr key={s.subject}>
                <Table.Td>
                  <Text ff="monospace" size="sm">
                    {s.subject}
                  </Text>
                </Table.Td>
                <Table.Td>{s.subscriptions}</Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}
    </Stack>
  );
}
