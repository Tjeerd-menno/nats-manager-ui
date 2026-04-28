import { Card, Group, Stack, Text, Title, Badge, SimpleGrid } from '@mantine/core';
import { useConsumer } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { ConsumerHealthBadge } from './ConsumerHealthBadge';
import { LoadingState } from '../../../shared/LoadingState';
import { OpenRelationshipMapButton } from '../../relationships/components/OpenRelationshipMapButton';

interface ConsumerDetailProps {
  streamName: string;
  consumerName: string;
}

export function ConsumerDetail({ streamName, consumerName }: ConsumerDetailProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { data: consumer, isLoading } = useConsumer(selectedEnvironmentId, streamName, consumerName);

  if (isLoading || !consumer) {
    return <LoadingState message="Loading consumer details..." />;
  }

  return (
    <Stack>
      <Group>
        <Title order={2}>{consumer.name}</Title>
        <ConsumerHealthBadge isHealthy={consumer.isHealthy} />
        <div style={{ flex: 1 }} />
        {selectedEnvironmentId && (
          <OpenRelationshipMapButton
            environmentId={selectedEnvironmentId}
            resourceId={`${streamName}/${consumerName}`}
            resourceType="Consumer"
          />
        )}
      </Group>

      {consumer.description && <Text c="dimmed">{consumer.description}</Text>}

      <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }}>
        <Card withBorder>
          <Text size="sm" c="dimmed">Pending</Text>
          <Text fw={700} size="xl">{consumer.numPending.toLocaleString()}</Text>
        </Card>
        <Card withBorder>
          <Text size="sm" c="dimmed">Ack Pending</Text>
          <Text fw={700} size="xl">{consumer.numAckPending.toLocaleString()}</Text>
        </Card>
        <Card withBorder>
          <Text size="sm" c="dimmed">Redelivered</Text>
          <Text fw={700} size="xl">{consumer.numRedelivered.toLocaleString()}</Text>
        </Card>
        <Card withBorder>
          <Text size="sm" c="dimmed">Delivered</Text>
          <Text fw={700} size="xl">{consumer.state.delivered.toLocaleString()}</Text>
        </Card>
      </SimpleGrid>

      <Card withBorder>
        <Title order={4} mb="sm">Configuration</Title>
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <Group><Text size="sm" c="dimmed">Stream:</Text><Text size="sm">{consumer.streamName}</Text></Group>
          <Group><Text size="sm" c="dimmed">Deliver Policy:</Text><Badge variant="outline" size="sm">{consumer.deliverPolicy}</Badge></Group>
          <Group><Text size="sm" c="dimmed">Ack Policy:</Text><Badge variant="outline" size="sm">{consumer.ackPolicy}</Badge></Group>
          {consumer.filterSubject && <Group><Text size="sm" c="dimmed">Filter:</Text><Text size="sm">{consumer.filterSubject}</Text></Group>}
          <Group><Text size="sm" c="dimmed">Created:</Text><Text size="sm">{new Date(consumer.created).toLocaleString()}</Text></Group>
        </SimpleGrid>
      </Card>

      <Card withBorder>
        <Title order={4} mb="sm">State</Title>
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <Group><Text size="sm" c="dimmed">Ack Floor:</Text><Text size="sm">{consumer.state.ackFloor.toLocaleString()}</Text></Group>
          <Group><Text size="sm" c="dimmed">Delivered Seq:</Text><Text size="sm">{consumer.state.delivered.toLocaleString()}</Text></Group>
        </SimpleGrid>
      </Card>
    </Stack>
  );
}
