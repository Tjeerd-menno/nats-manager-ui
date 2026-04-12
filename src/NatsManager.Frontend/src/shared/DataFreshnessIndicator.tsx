import { Badge } from '@mantine/core';

interface DataFreshnessIndicatorProps {
  freshness: 'live' | 'recent' | 'stale';
  timestamp?: string;
}

const colorMap = {
  live: 'green',
  recent: 'yellow',
  stale: 'red',
} as const;

export function DataFreshnessIndicator({ freshness, timestamp }: DataFreshnessIndicatorProps) {
  return (
    <Badge
      color={colorMap[freshness]}
      variant="dot"
      title={timestamp ? `Last updated: ${new Date(timestamp).toLocaleString()}` : undefined}
    >
      {freshness}
    </Badge>
  );
}
