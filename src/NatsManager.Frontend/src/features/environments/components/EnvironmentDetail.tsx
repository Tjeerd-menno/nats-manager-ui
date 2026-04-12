import { Card, Group, Stack, Text, Title, Badge, SimpleGrid, Button, Loader } from '@mantine/core';
import { IconPlugConnected } from '@tabler/icons-react';
import { useEnvironment, useTestConnection } from '../hooks/useEnvironments';
import { ConnectionStatusBadge } from './ConnectionStatusBadge';
import { LoadingState } from '../../../shared/LoadingState';

interface EnvironmentDetailProps {
  environmentId: string;
}

export function EnvironmentDetail({ environmentId }: EnvironmentDetailProps) {
  const { data: environment, isLoading } = useEnvironment(environmentId);
  const testMutation = useTestConnection();

  if (isLoading || !environment) {
    return <LoadingState message="Loading environment details..." />;
  }

  return (
    <Stack>
      <Group justify="space-between">
        <Group>
          <Title order={2}>{environment.name}</Title>
          <ConnectionStatusBadge status={environment.connectionStatus} />
          {environment.isProduction && <Badge color="red" variant="light">PRODUCTION</Badge>}
          {!environment.isEnabled && <Badge color="gray" variant="light">Disabled</Badge>}
        </Group>
        <Button
          leftSection={testMutation.isPending ? <Loader size={14} /> : <IconPlugConnected size={16} />}
          variant="outline"
          onClick={() => testMutation.mutate(environmentId)}
          loading={testMutation.isPending}
        >
          Test Connection
        </Button>
      </Group>

      {environment.description && (
        <Text c="dimmed">{environment.description}</Text>
      )}

      <SimpleGrid cols={{ base: 1, sm: 2 }}>
        <Card withBorder>
          <Stack gap="xs">
            <Text size="sm" c="dimmed">Server URL</Text>
            <Text fw={500}>{environment.serverUrl}</Text>
          </Stack>
        </Card>
        <Card withBorder>
          <Stack gap="xs">
            <Text size="sm" c="dimmed">Credential Type</Text>
            <Text fw={500}>{environment.credentialType}</Text>
          </Stack>
        </Card>
        <Card withBorder>
          <Stack gap="xs">
            <Text size="sm" c="dimmed">Last Successful Contact</Text>
            <Text fw={500}>
              {environment.lastSuccessfulContact
                ? new Date(environment.lastSuccessfulContact).toLocaleString()
                : 'Never'}
            </Text>
          </Stack>
        </Card>
        <Card withBorder>
          <Stack gap="xs">
            <Text size="sm" c="dimmed">Created</Text>
            <Text fw={500}>{new Date(environment.createdAt).toLocaleString()}</Text>
          </Stack>
        </Card>
      </SimpleGrid>

      {testMutation.isSuccess && (
        <Card withBorder>
          <Title order={4} mb="sm">Connection Test Result</Title>
          <SimpleGrid cols={{ base: 1, sm: 2 }}>
            <Group>
              <Text size="sm" c="dimmed">Reachable:</Text>
              <Badge color={testMutation.data.reachable ? 'green' : 'red'}>
                {testMutation.data.reachable ? 'Yes' : 'No'}
              </Badge>
            </Group>
            {testMutation.data.latencyMs != null && (
              <Group>
                <Text size="sm" c="dimmed">Latency:</Text>
                <Text fw={500}>{testMutation.data.latencyMs}ms</Text>
              </Group>
            )}
            {testMutation.data.serverVersion && (
              <Group>
                <Text size="sm" c="dimmed">Server Version:</Text>
                <Text fw={500}>{testMutation.data.serverVersion}</Text>
              </Group>
            )}
            <Group>
              <Text size="sm" c="dimmed">JetStream:</Text>
              <Badge color={testMutation.data.jetStreamAvailable ? 'green' : 'gray'}>
                {testMutation.data.jetStreamAvailable ? 'Available' : 'Not Available'}
              </Badge>
            </Group>
          </SimpleGrid>
        </Card>
      )}
    </Stack>
  );
}
