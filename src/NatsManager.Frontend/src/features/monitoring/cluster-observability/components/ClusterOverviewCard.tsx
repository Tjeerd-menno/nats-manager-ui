import { Card, Stack, Group, Text, Badge, SimpleGrid, Divider } from '@mantine/core';
import { IconServer, IconActivity, IconPlugConnected } from '@tabler/icons-react';
import { DataFreshnessIndicator } from '../../../../shared/DataFreshnessIndicator';
import { DataSourceBadge } from '../../../../shared/DataSourceBadge';
import { ClusterFreshnessBadge } from './ClusterFreshnessBadge';
import type { ClusterObservation, ClusterStatus } from '../types';

interface ClusterOverviewCardProps {
  observation: ClusterObservation;
}

const clusterStatusConfig: Record<ClusterStatus, { color: string; label: string }> = {
  Healthy: { color: 'green', label: 'Healthy' },
  Degraded: { color: 'yellow', label: 'Degraded' },
  Unavailable: { color: 'red', label: 'Unavailable' },
  Unknown: { color: 'gray', label: 'Unknown' },
};

function freshnessToIndicator(freshness: ClusterObservation['freshness']): 'live' | 'recent' | 'stale' {
  switch (freshness) {
    case 'Live': return 'live';
    case 'Stale':
    case 'Partial':
      return 'stale';
    default: return 'stale';
  }
}

function formatRate(value: number | null): string {
  if (value === null) return '—';
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M/s`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(1)}K/s`;
  return `${value.toFixed(1)}/s`;
}

export function ClusterOverviewCard({ observation }: ClusterOverviewCardProps) {
  const statusConfig = clusterStatusConfig[observation.status] ?? clusterStatusConfig.Unknown;

  return (
    <Card shadow="sm" padding="lg" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" wrap="nowrap">
          <Group gap="xs">
            <IconActivity size={20} stroke={1.5} />
            <Text fw={600} size="lg">Cluster Overview</Text>
          </Group>
          <Group gap="xs">
            <Badge color={statusConfig.color} variant="light">
              {statusConfig.label}
            </Badge>
            <ClusterFreshnessBadge freshness={observation.freshness} />
            <DataFreshnessIndicator
              freshness={freshnessToIndicator(observation.freshness)}
              timestamp={observation.observedAt}
            />
            <DataSourceBadge source="observed" />
          </Group>
        </Group>

        <Divider />

        <SimpleGrid cols={{ base: 2, sm: 3, md: 6 }}>
          <Stack gap={2}>
            <Text size="xs" c="dimmed">Servers</Text>
            <Group gap="xs" align="baseline">
              <Text size="xl" fw={700}>{observation.serverCount}</Text>
              <IconServer size={14} stroke={1.5} />
            </Group>
          </Stack>

          <Stack gap={2}>
            <Text size="xs" c="dimmed">Degraded</Text>
            <Text
              size="xl"
              fw={700}
              c={observation.degradedServerCount > 0 ? 'red' : 'green'}
            >
              {observation.degradedServerCount}
            </Text>
          </Stack>

          <Stack gap={2}>
            <Text size="xs" c="dimmed">JetStream</Text>
            {observation.jetStreamAvailable === null ? (
              <Badge color="gray" variant="light" size="sm">Unknown</Badge>
            ) : (
              <Badge
                color={observation.jetStreamAvailable ? 'green' : 'gray'}
                variant="light"
                size="sm"
              >
                {observation.jetStreamAvailable ? 'Enabled' : 'Disabled'}
              </Badge>
            )}
          </Stack>

          <Stack gap={2}>
            <Text size="xs" c="dimmed">Connections</Text>
            <Group gap="xs" align="baseline">
              <Text size="xl" fw={700}>
                {observation.connectionCount?.toLocaleString() ?? '—'}
              </Text>
              <IconPlugConnected size={14} stroke={1.5} />
            </Group>
          </Stack>

          <Stack gap={2}>
            <Text size="xs" c="dimmed">In Msgs</Text>
            <Text size="xl" fw={700}>{formatRate(observation.inMsgsPerSecond)}</Text>
          </Stack>

          <Stack gap={2}>
            <Text size="xs" c="dimmed">Out Msgs</Text>
            <Text size="xl" fw={700}>{formatRate(observation.outMsgsPerSecond)}</Text>
          </Stack>
        </SimpleGrid>

        <Text size="xs" c="dimmed">
          Last observed: {new Date(observation.observedAt).toLocaleString()}
        </Text>
      </Stack>
    </Card>
  );
}
