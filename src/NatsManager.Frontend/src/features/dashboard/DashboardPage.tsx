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

export default function DashboardPage() {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { data, isLoading, error } = useDashboard(selectedEnvironmentId);

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

  return (
    <Stack>
      <Title order={2}>Dashboard</Title>

      <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Group justify="space-between" mb="xs">
            <Text size="sm" c="dimmed" fw={500}>Connection Status</Text>
            <ThemeIcon variant="light" color={data.environment.connectionStatus === 'Available' ? 'green' : 'red'} size="md" radius="md">
              <IconPlugConnected size={16} />
            </ThemeIcon>
          </Group>
          <Badge color={data.environment.connectionStatus === 'Available' ? 'green' : 'red'} size="lg" variant="light">
            {data.environment.connectionStatus}
          </Badge>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Group justify="space-between" mb="xs">
            <Text size="sm" c="dimmed" fw={500}>JetStream Streams</Text>
            <ThemeIcon variant="light" color="violet" size="md" radius="md">
              <IconBolt size={16} />
            </ThemeIcon>
          </Group>
          <Text size="xl" fw={700}>{data.jetStream.streamCount}</Text>
          <Group gap="xs" mt="xs">
            <Text size="sm" c="dimmed">{data.jetStream.consumerCount} consumers</Text>
            {data.jetStream.unhealthyConsumers > 0 && (
              <Badge color="red" size="sm" variant="light">{data.jetStream.unhealthyConsumers} unhealthy</Badge>
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
          <Text size="xl" fw={700}>{data.jetStream.totalMessages.toLocaleString()}</Text>
          <Text size="sm" c="dimmed" mt="xs">{(data.jetStream.totalBytes / 1024 / 1024).toFixed(1)} MB</Text>
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
