import { Title, Stack, Alert, Badge, Group, Text } from '@mantine/core';
import { IconAlertTriangle, IconActivity } from '@tabler/icons-react';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { EmptyState } from '../../shared/EmptyState';
import { LoadingState } from '../../shared/LoadingState';
import { MonitoringStatusBadge } from './components/MonitoringStatusBadge';
import { ServerMetricsChart } from './components/ServerMetricsChart';
import { JetStreamMetricsCard } from './components/JetStreamMetricsCard';
import { useMonitoringHub } from './hooks/useMonitoringHub';

function formatHealthStatus(status: 'Ok' | 'Degraded' | 'Unavailable'): { color: string; label: string } {
  switch (status) {
    case 'Ok': return { color: 'green', label: 'Healthy' };
    case 'Degraded': return { color: 'yellow', label: 'Degraded' };
    case 'Unavailable': return { color: 'red', label: 'Unavailable' };
  }
}

export default function MonitoringPage() {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { snapshots, latestSnapshot, connectionStatus, isNotConfigured, error } = useMonitoringHub(selectedEnvironmentId);

  if (!selectedEnvironmentId) {
    return (
      <Stack>
        <Title order={2}>Monitoring</Title>
        <EmptyState message="Select an environment to view monitoring." icon={IconActivity} />
      </Stack>
    );
  }

  if (connectionStatus === 'connecting' && snapshots.length === 0) {
    return <LoadingState message="Connecting to monitoring…" />;
  }

  if (isNotConfigured) {
    return (
      <Stack>
        <Title order={2}>Monitoring</Title>
        <EmptyState
          message="Monitoring is not configured for this environment. Edit the environment to add a Monitoring URL."
          icon={IconActivity}
        />
      </Stack>
    );
  }

  const healthBadge = latestSnapshot?.healthStatus
    ? formatHealthStatus(latestSnapshot.healthStatus)
    : null;

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Monitoring</Title>
        <Group>
          {healthBadge && (
            <Badge color={healthBadge.color} variant="light">
              Health: {healthBadge.label}
            </Badge>
          )}
          <MonitoringStatusBadge status={connectionStatus} />
          {latestSnapshot && (
            <Text size="sm" c="dimmed">
              Last updated: {new Date(latestSnapshot.timestamp).toLocaleTimeString()}
            </Text>
          )}
        </Group>
      </Group>

      {connectionStatus === 'disconnected' && (
        <Alert color="red" icon={<IconAlertTriangle size={16} />} title="Monitoring Unavailable">
          {error ?? 'Connection lost. Attempting to reconnect…'}
        </Alert>
      )}

      {latestSnapshot?.status === 'Unavailable' && (
        <Alert color="red" icon={<IconAlertTriangle size={16} />} title="Monitoring Endpoint Unavailable">
          The NATS monitoring endpoint is currently unreachable. Charts keep the last usable metrics.
        </Alert>
      )}

      {connectionStatus === 'reconnecting' && (
        <Alert color="yellow" title="Reconnecting…">
          Reconnecting to monitoring stream…
        </Alert>
      )}

      <ServerMetricsChart snapshots={snapshots} />
      <JetStreamMetricsCard snapshots={snapshots} />
    </Stack>
  );
}
