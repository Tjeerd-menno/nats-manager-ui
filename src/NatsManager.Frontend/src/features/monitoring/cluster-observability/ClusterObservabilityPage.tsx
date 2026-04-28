import { Stack, Title, Group, Tabs, Text, Alert } from '@mantine/core';
import { IconNetwork, IconServer, IconAlertTriangle } from '@tabler/icons-react';
import { useParams } from 'react-router-dom';
import { useClusterOverview } from './hooks/useClusterOverview';
import { ClusterOverviewCard } from './components/ClusterOverviewCard';
import { ClusterWarningList } from './components/ClusterWarningList';
import { ClusterUnavailableState } from './components/ClusterUnavailableState';
import { ClusterServerList } from './components/ClusterServerList';
import { ClusterTopologyGraph } from './ClusterTopologyGraph';
import { LoadingState } from '../../../shared/LoadingState';
import { EmptyState } from '../../../shared/EmptyState';

export default function ClusterObservabilityPage() {
  const { envId } = useParams<{ envId: string }>();
  const { data, isLoading, isError, error, refetch } = useClusterOverview(envId ?? null);

  if (!envId) {
    return (
      <Stack>
        <Title order={2}>Cluster Observability</Title>
        <EmptyState message="No environment selected." icon={IconNetwork} />
      </Stack>
    );
  }

  if (isLoading) {
    return <LoadingState message="Loading cluster overview…" />;
  }

  if (isError) {
    const message = error instanceof Error ? error.message : 'Failed to load cluster data.';
    return (
      <Stack>
        <Title order={2}>Cluster Observability</Title>
        <Alert color="red" icon={<IconAlertTriangle size={16} />} title="Error">
          {message}
        </Alert>
        <ClusterUnavailableState
          message="Unable to retrieve cluster observability data."
          onRetry={() => void refetch()}
        />
      </Stack>
    );
  }

  if (!data || data.status === 'Unavailable') {
    return (
      <Stack>
        <Title order={2}>Cluster Observability</Title>
        <ClusterUnavailableState
          message="Cluster observability data is currently unavailable for this environment."
          onRetry={() => void refetch()}
        />
      </Stack>
    );
  }

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Cluster Observability</Title>
        <Text size="sm" c="dimmed">Environment: {envId}</Text>
      </Group>

      <ClusterOverviewCard observation={data} />

      {data.warnings.length > 0 && (
        <ClusterWarningList warnings={data.warnings} />
      )}

      <Tabs defaultValue="servers" mt="md">
        <Tabs.List>
          <Tabs.Tab value="servers" leftSection={<IconServer size={14} />}>
            Servers
          </Tabs.Tab>
          <Tabs.Tab value="topology" leftSection={<IconNetwork size={14} />}>
            Topology
          </Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="servers" pt="md">
          <ClusterServerList envId={envId} />
        </Tabs.Panel>

        <Tabs.Panel value="topology" pt="md">
          <ClusterTopologyGraph relationships={data.topology} envId={envId} />
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
