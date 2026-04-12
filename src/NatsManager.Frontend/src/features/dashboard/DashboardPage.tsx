import { Title, Text, SimpleGrid, Card, Badge, Group, Stack, Alert, Loader, Center } from '@mantine/core';
import { useDashboard } from './hooks/useDashboard';
import { useEnvironmentContext } from '../environments/EnvironmentContext';

export default function DashboardPage() {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { data, isLoading, error } = useDashboard(selectedEnvironmentId);

  if (!selectedEnvironmentId) {
    return (
      <div>
        <Title order={2}>Dashboard</Title>
        <Text c="dimmed" mt="sm">Select an environment to view the dashboard.</Text>
      </div>
    );
  }

  if (isLoading) {
    return <Center h={200}><Loader /></Center>;
  }

  if (error || !data) {
    return (
      <div>
        <Title order={2}>Dashboard</Title>
        <Text c="red" mt="sm">Failed to load dashboard data.</Text>
      </div>
    );
  }

  return (
    <Stack>
      <Title order={2}>Dashboard</Title>

      <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">Connection Status</Text>
          <Badge color={data.environment.connectionStatus === 'Available' ? 'green' : 'red'} size="lg" mt="xs">
            {data.environment.connectionStatus}
          </Badge>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">JetStream Streams</Text>
          <Text size="xl" fw={700}>{data.jetStream.streamCount}</Text>
          <Group gap="xs" mt="xs">
            <Text size="sm">{data.jetStream.consumerCount} consumers</Text>
            {data.jetStream.unhealthyConsumers > 0 && (
              <Badge color="red" size="sm">{data.jetStream.unhealthyConsumers} unhealthy</Badge>
            )}
          </Group>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">Key-Value Buckets</Text>
          <Text size="xl" fw={700}>{data.keyValue.bucketCount}</Text>
          <Text size="sm" mt="xs">{data.keyValue.totalKeys} total keys</Text>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">Total Messages</Text>
          <Text size="xl" fw={700}>{data.jetStream.totalMessages.toLocaleString()}</Text>
          <Text size="sm" mt="xs">{(data.jetStream.totalBytes / 1024 / 1024).toFixed(1)} MB</Text>
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
            >
              {alert.message}
            </Alert>
          ))}
        </Stack>
      )}
    </Stack>
  );
}
