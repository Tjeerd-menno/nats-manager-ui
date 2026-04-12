import { Badge } from '@mantine/core';

interface EnvironmentBadgeProps {
  name: string;
  isProduction: boolean;
  connectionStatus: string;
}

const statusColorMap: Record<string, string> = {
  Available: 'green',
  Degraded: 'yellow',
  Unavailable: 'red',
  Unknown: 'gray',
};

export function EnvironmentBadge({ name, isProduction, connectionStatus }: EnvironmentBadgeProps) {
  return (
    <Badge
      color={statusColorMap[connectionStatus] ?? 'gray'}
      variant={isProduction ? 'filled' : 'light'}
    >
      {name}
      {isProduction ? ' (PROD)' : ''}
    </Badge>
  );
}
