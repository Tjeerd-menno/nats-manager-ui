import { Badge, Tooltip } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import type { ResourceHealthStatus } from '../types';

interface WarningOverlayProps {
  status: ResourceHealthStatus;
}

export function WarningOverlay({ status }: WarningOverlayProps) {
  if (status === 'Healthy' || status === 'Unknown') {
    return null;
  }

  const color = status === 'Warning' ? 'yellow' : status === 'Stale' ? 'gray' : 'red';

  return (
    <Tooltip label={`Resource status: ${status}`}>
      <Badge
        aria-label={`Resource status ${status}`}
        color={color}
        leftSection={<IconAlertTriangle size={10} />}
        size="xs"
        variant="filled"
      >
        {status}
      </Badge>
    </Tooltip>
  );
}
