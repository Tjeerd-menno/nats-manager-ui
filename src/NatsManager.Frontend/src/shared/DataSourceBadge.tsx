import { Badge, type BadgeProps } from '@mantine/core';

interface DataSourceBadgeProps extends Omit<BadgeProps, 'children'> {
  source: 'observed' | 'configured' | 'derived' | 'inferred';
}

const sourceConfig: Record<DataSourceBadgeProps['source'], { color: string; label: string }> = {
  observed: { color: 'green', label: 'Observed' },
  configured: { color: 'blue', label: 'Configured' },
  derived: { color: 'orange', label: 'Derived' },
  inferred: { color: 'gray', label: 'Inferred' },
};

export function DataSourceBadge({ source, ...rest }: DataSourceBadgeProps) {
  const config = sourceConfig[source] ?? sourceConfig.inferred;
  return (
    <Badge size="xs" variant="light" color={config.color} {...rest}>
      {config.label}
    </Badge>
  );
}
