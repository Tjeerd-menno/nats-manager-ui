import { useState } from 'react';
import { Title, Text, Card, Stack, Group, Badge, Button, TextInput, Textarea, Modal, Loader, Center, SimpleGrid, Code } from '@mantine/core';
import { useCoreNatsStatus, usePublishMessage } from './hooks/useCoreNats';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { OpenRelationshipMapButton } from '../relationships/components/OpenRelationshipMapButton';

export default function CoreNatsPage() {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { data: serverInfo, isLoading } = useCoreNatsStatus(selectedEnvironmentId);
  const publishMutation = usePublishMessage(selectedEnvironmentId);
  const [publishOpen, setPublishOpen] = useState(false);
  const [subject, setSubject] = useState('');
  const [payload, setPayload] = useState('');

  if (!selectedEnvironmentId) {
    return (
      <div>
        <Title order={2}>Core NATS</Title>
        <Text c="dimmed" mt="sm">Select an environment to view NATS server info.</Text>
      </div>
    );
  }

  if (isLoading) return <Center h={200}><Loader /></Center>;

  if (!serverInfo) {
    return (
      <div>
        <Title order={2}>Core NATS</Title>
        <Text c="red" mt="sm">Unable to retrieve server information.</Text>
      </div>
    );
  }

  const handlePublish = () => {
    publishMutation.mutate({ subject, payload: payload || undefined }, {
      onSuccess: () => { setPublishOpen(false); setSubject(''); setPayload(''); },
    });
  };

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

      <Modal opened={publishOpen} onClose={() => setPublishOpen(false)} title="Publish Message">
        <Stack>
          <TextInput label="Subject" value={subject} onChange={(e) => setSubject(e.currentTarget.value)} required />
          <Textarea label="Payload" value={payload} onChange={(e) => setPayload(e.currentTarget.value)} minRows={3} />
          <Button onClick={handlePublish} loading={publishMutation.isPending}>Publish</Button>
          {publishMutation.isSuccess && <Text c="green" size="sm">Message published successfully</Text>}
          {publishMutation.error && <Text c="red" size="sm">Error: {(publishMutation.error as Error).message}</Text>}
        </Stack>
      </Modal>
    </Stack>
  );
}
