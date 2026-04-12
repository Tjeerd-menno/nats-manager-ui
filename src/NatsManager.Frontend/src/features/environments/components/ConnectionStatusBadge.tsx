import { Badge } from '@mantine/core';
import type { ConnectionStatus } from '../types';

const statusColors: Record<ConnectionStatus, string> = {
  Available: 'green',
  Degraded: 'yellow',
  Unavailable: 'red',
  Unknown: 'gray',
};

const statusLabels: Record<ConnectionStatus, string> = {
  Available: 'Available',
  Degraded: 'Degraded',
  Unavailable: 'Unavailable',
  Unknown: 'Unknown',
};

interface ConnectionStatusBadgeProps {
  status: ConnectionStatus;
}

export function ConnectionStatusBadge({ status }: ConnectionStatusBadgeProps) {
  return (
    <Badge color={statusColors[status]} variant="filled" size="sm">
      {statusLabels[status]}
    </Badge>
  );
}
