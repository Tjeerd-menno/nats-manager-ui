import { useState } from 'react';
import {
  Title,
  Text,
  Card,
  Stack,
  Group,
  Badge,
  Button,
  Modal,
  Loader,
  Center,
  SimpleGrid,
  Code,
} from '@mantine/core';
import { useCoreNatsStatus } from './hooks/useCoreNats';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { OpenRelationshipMapButton } from '../relationships/components/OpenRelationshipMapButton';
import { SubjectBrowser } from './components/SubjectBrowser';
import { PublishMessageForm } from './components/PublishMessageForm';
import { LiveMessageViewer } from './components/LiveMessageViewer';

export default function CoreNatsPage() {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { data: serverInfo, isLoading } = useCoreNatsStatus(selectedEnvironmentId);
  const [publishOpen, setPublishOpen] = useState(false);

  if (!selectedEnvironmentId) {
    return (
      <div>
        <Title order={2}>Core NATS</Title>
        <Text c="dimmed" mt="sm">
          Select an environment to view NATS server info.
        </Text>
      </div>
    );
  }

  if (isLoading) return <Center h={200}><Loader /></Center>;

  if (!serverInfo) {
    return (
      <div>
        <Title order={2}>Core NATS</Title>
        <Text c="red" mt="sm">
          Unable to retrieve server information.
        </Text>
      </div>
    );
  }

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Core NATS</Title>
        <Group>
          <OpenRelationshipMapButton
            environmentId={selectedEnvironmentId}
            resourceId={serverInfo.serverId}
            resourceType="Server"
          />
          <Button onClick={() => setPublishOpen(true)}>Publish Message</Button>
        </Group>
      </Group>

      <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">Server</Text>
          <Text fw={700}>{serverInfo.serverName || serverInfo.serverId}</Text>
          <Group gap="xs" mt="xs">
            <Badge>v{serverInfo.version}</Badge>
            <Code>{serverInfo.host}:{serverInfo.port}</Code>
          </Group>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">Connections</Text>
          <Text size="xl" fw={700}>{serverInfo.connections}</Text>
          <Text size="sm" c="dimmed" mt="xs">Max payload: {(serverInfo.maxPayload / 1024).toFixed(0)} KB</Text>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">JetStream</Text>
          <Badge color={serverInfo.jetStreamEnabled ? 'green' : 'gray'} size="lg">
            {serverInfo.jetStreamEnabled ? 'Enabled' : 'Disabled'}
          </Badge>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">Messages In</Text>
          <Text size="xl" fw={700}>{serverInfo.inMsgs.toLocaleString()}</Text>
          <Text size="sm" c="dimmed" mt="xs">{(serverInfo.inBytes / 1024 / 1024).toFixed(1)} MB</Text>
        </Card>

        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Text size="sm" c="dimmed">Messages Out</Text>
          <Text size="xl" fw={700}>{serverInfo.outMsgs.toLocaleString()}</Text>
          <Text size="sm" c="dimmed" mt="xs">{(serverInfo.outBytes / 1024 / 1024).toFixed(1)} MB</Text>
        </Card>
      </SimpleGrid>

      <Card shadow="sm" padding="lg" radius="md" withBorder>
        <Title order={4} mb="sm">Active Subjects</Title>
        <SubjectBrowser environmentId={selectedEnvironmentId} />
      </Card>

      <Card shadow="sm" padding="lg" radius="md" withBorder>
        <Title order={4} mb="sm">Live Message Viewer</Title>
        <LiveMessageViewer environmentId={selectedEnvironmentId} />
      </Card>

      <Modal opened={publishOpen} onClose={() => setPublishOpen(false)} title="Publish Message" size="lg">
        <PublishMessageForm environmentId={selectedEnvironmentId} />
      </Modal>
    </Stack>
  );
}
