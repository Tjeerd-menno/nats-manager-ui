import { Title, Text, SimpleGrid, Card, Badge, Group, Stack, Alert, ThemeIcon } from '@mantine/core';
import {
  IconPlugConnected,
  IconBolt,
  IconKey,
  IconMail,
  IconAlertTriangle,
} from '@tabler/icons-react';
import { useDashboard } from './hooks/useDashboard';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { LoadingState } from '../../shared/LoadingState';
import { EmptyState } from '../../shared/EmptyState';
import { useDashboardHealth, useDashboardJetStream } from '../../read-model/projections';

export default function DashboardPage() {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { data, isLoading, error } = useDashboard(selectedEnvironmentId);
  const readModelHealth = useDashboardHealth(selectedEnvironmentId);
  const readModelJetStream = useDashboardJetStream(selectedEnvironmentId);

  if (!selectedEnvironmentId) {
    return (
      <Stack>
        <Title order={2}>Dashboard</Title>
        <EmptyState
          message="Select an environment to view the dashboard."
          icon={IconPlugConnected}
        />
      </Stack>
    );
  }

  if (isLoading) {
    return <LoadingState message="Loading dashboard…" />;
  }

  if (error || !data) {
    return (
      <Stack>
        <Title order={2}>Dashboard</Title>
        <Alert color="red" icon={<IconAlertTriangle size={16} />} title="Error">
          Failed to load dashboard data.
        </Alert>
      </Stack>
    );
  }

  const connectionStatus = readModelHealth.connectionStatus === 'Connected'
    ? 'Available'
    : readModelHealth.connectionStatus === 'Reconnecting'
      ? 'Degraded'
      : readModelHealth.connectionStatus === 'Disconnected' && readModelHealth.lastUpdatedUtc
        ? 'Unavailable'
        : data.environment.connectionStatus;
  const jetStream = readModelJetStream.isEnabled === null
    ? data.jetStream
    : {
        ...data.jetStream,
        streamCount: readModelJetStream.streamCount,
        consumerCount: readModelJetStream.consumerCount,
        totalMessages: readModelJetStream.totalMessages,
        totalBytes: readModelJetStream.totalBytes,
      };

  return (
    <Stack>
      <Title order={2}>Dashboard</Title>

      <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Group justify="space-between" mb="xs">
            <Text size="sm" c="dimmed" fw={500}>Connection Status</Text>
            <ThemeIcon variant="light" color={connectionStatus === 'Available' ? 'green' : 'red'} size="md" radius="md">
              <IconPlugConnected size={16} />
            </ThemeIcon>
          </Group>
          <Badge color={connectionStatus === 'Available' ? 'green' : 'red'} size="lg" variant="light">
            {connectionStatus}
          </Badge>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Group justify="space-between" mb="xs">
            <Text size="sm" c="dimmed" fw={500}>JetStream Streams</Text>
            <ThemeIcon variant="light" color="violet" size="md" radius="md">
              <IconBolt size={16} />
            </ThemeIcon>
          </Group>
          <Text size="xl" fw={700}>{jetStream.streamCount}</Text>
          <Group gap="xs" mt="xs">
            <Text size="sm" c="dimmed">{jetStream.consumerCount} consumers</Text>
            {jetStream.unhealthyConsumers > 0 && (
              <Badge color="red" size="sm" variant="light">{jetStream.unhealthyConsumers} unhealthy</Badge>
            )}
          </Group>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Group justify="space-between" mb="xs">
            <Text size="sm" c="dimmed" fw={500}>Key-Value Buckets</Text>
            <ThemeIcon variant="light" color="teal" size="md" radius="md">
              <IconKey size={16} />
            </ThemeIcon>
          </Group>
          <Text size="xl" fw={700}>{data.keyValue.bucketCount}</Text>
          <Text size="sm" c="dimmed" mt="xs">{data.keyValue.totalKeys} total keys</Text>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Group justify="space-between" mb="xs">
            <Text size="sm" c="dimmed" fw={500}>Total Messages</Text>
            <ThemeIcon variant="light" color="blue" size="md" radius="md">
              <IconMail size={16} />
            </ThemeIcon>
          </Group>
          <Text size="xl" fw={700}>{jetStream.totalMessages.toLocaleString()}</Text>
          <Text size="sm" c="dimmed" mt="xs">{(jetStream.totalBytes / 1024 / 1024).toFixed(1)} MB</Text>
        </Card>
      </SimpleGrid>

      {data.alerts.length > 0 && (
        <Stack gap="xs">
          <Title order={4}>Alerts</Title>
          {data.alerts.map((alert, index) => (
            <Alert
              key={index}
              color={alert.severity === 'Error' ? 'red' : 'yellow'}
              title={`${alert.resourceType}: ${alert.resourceName}`}
              icon={<IconAlertTriangle size={16} />}
            >
              {alert.message}
            </Alert>
          ))}
        </Stack>
      )}
    </Stack>
  );
}
