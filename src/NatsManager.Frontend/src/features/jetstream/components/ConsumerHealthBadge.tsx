import { Badge } from '@mantine/core';

interface ConsumerHealthBadgeProps {
  isHealthy: boolean;
}

export function ConsumerHealthBadge({ isHealthy }: ConsumerHealthBadgeProps) {
  return (
    <Badge color={isHealthy ? 'green' : 'red'} variant="filled" size="sm">
      {isHealthy ? 'Healthy' : 'Unhealthy'}
    </Badge>
  );
}
