import { Badge, Group } from '@mantine/core';
import type { MonitoringConnectionStatus } from '../types';

interface Props {
  status: MonitoringConnectionStatus;
}

const statusConfig = {
  connected: { color: 'green', label: 'Connected' },
  reconnecting: { color: 'yellow', label: 'Reconnecting…' },
  disconnected: { color: 'red', label: 'Disconnected' },
  connecting: { color: 'gray', label: 'Connecting…' },
} as const;

export function MonitoringStatusBadge({ status }: Props) {
  const config = statusConfig[status];
  return (
    <Group gap="xs">
      <Badge color={config.color} variant="light">
        {config.label}
      </Badge>
    </Group>
  );
}
