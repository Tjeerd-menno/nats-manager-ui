import { Select, Group } from '@mantine/core';
import { useEnvironments } from '../hooks/useEnvironments';
import { ConnectionStatusBadge } from './ConnectionStatusBadge';
import type { ConnectionStatus } from '../types';

interface EnvironmentSelectorProps {
  selectedId: string | null;
  onSelect: (id: string | null) => void;
}

export function EnvironmentSelector({ selectedId, onSelect }: EnvironmentSelectorProps) {
  const { data } = useEnvironments({ pageSize: 100 });

  const options = (data?.data.items ?? []).map((env) => ({
    value: env.id,
    label: env.name,
    status: env.connectionStatus,
  }));

  return (
    <Select
      placeholder="Select environment"
      data={options.map((o) => ({ value: o.value, label: o.label }))}
      value={selectedId}
      onChange={onSelect}
      clearable
      searchable
      size="sm"
      renderOption={({ option }) => {
        const envOption = options.find((o) => o.value === option.value);
        return (
          <Group gap="sm">
            <span>{option.label}</span>
            {envOption && <ConnectionStatusBadge status={envOption.status as ConnectionStatus} />}
          </Group>
        );
      }}
    />
  );
}
