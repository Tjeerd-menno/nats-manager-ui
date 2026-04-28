import { Badge } from '@mantine/core';
import type { ObservationFreshness } from '../types';

interface ClusterFreshnessBadgeProps {
  freshness: ObservationFreshness;
}

const freshnessConfig: Record<ObservationFreshness, { color: string; label: string }> = {
  Live: { color: 'green', label: 'Live' },
  Stale: { color: 'yellow', label: 'Stale' },
  Partial: { color: 'orange', label: 'Partial' },
  Unavailable: { color: 'red', label: 'Unavailable' },
};

export function ClusterFreshnessBadge({ freshness }: ClusterFreshnessBadgeProps) {
  const config = freshnessConfig[freshness] ?? freshnessConfig.Unavailable;
  return (
    <Badge color={config.color} variant="dot">
      {config.label}
    </Badge>
  );
}
