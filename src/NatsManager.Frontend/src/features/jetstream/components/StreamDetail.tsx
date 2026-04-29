import { useState } from 'react';
import { Card, Group, Stack, Text, Title, Badge, SimpleGrid, Table, Button, Box } from '@mantine/core';
import { IconPlus } from '@tabler/icons-react';
import { useStream } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { ConsumerHealthBadge } from './ConsumerHealthBadge';
import { StreamActions } from './StreamActions';
import { StreamForm } from './StreamForm';
import { ConsumerForm } from './ConsumerForm';
import { ConsumerActions } from './ConsumerActions';
import { MessageBrowser } from './MessageBrowser';
import { LoadingState } from '../../../shared/LoadingState';
import { OpenRelationshipMapButton } from '../../relationships/components/OpenRelationshipMapButton';
import { formatBytes } from '../../../shared/formatting';

interface StreamDetailProps {
  streamName: string;
  onConsumerSelect: (consumerName: string) => void;
  onDeleted?: () => void;
}

export function StreamDetail({ streamName, onConsumerSelect, onDeleted }: StreamDetailProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const { data, isLoading } = useStream(selectedEnvironmentId, streamName);
  const [editFormOpened, setEditFormOpened] = useState(false);
  const [consumerFormOpened, setConsumerFormOpened] = useState(false);

  if (isLoading || !data) {
    return <LoadingState message="Loading stream details..." />;
  }

  const { info, config, consumers } = data;

  return (
    <Stack>
      <Group>
        <Title order={2}>{info.name}</Title>
        <Badge variant="outline">{config.retentionPolicy}</Badge>
        <Badge variant="outline">{config.storageType}</Badge>
        <Box flex={1} />
        {selectedEnvironmentId && (
          <OpenRelationshipMapButton
            environmentId={selectedEnvironmentId}
            resourceId={streamName}
            resourceType="Stream"
          />
        )}
        <Button variant="outline" size="xs" onClick={() => setEditFormOpened(true)}>Edit</Button>
        <StreamActions streamName={streamName} onDeleted={onDeleted} />
      </Group>

      <StreamForm opened={editFormOpened} onClose={() => setEditFormOpened(false)} existingConfig={config} />

      {info.description && <Text c="dimmed">{info.description}</Text>}

      <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }}>
        <Card withBorder>
          <Text size="sm" c="dimmed">Messages</Text>
          <Text fw={700} size="xl">{info.messages.toLocaleString()}</Text>
        </Card>
        <Card withBorder>
          <Text size="sm" c="dimmed">Size</Text>
          <Text fw={700} size="xl">{formatBytes(info.bytes)}</Text>
        </Card>
        <Card withBorder>
          <Text size="sm" c="dimmed">Consumers</Text>
          <Text fw={700} size="xl">{info.consumerCount}</Text>
        </Card>
        <Card withBorder>
          <Text size="sm" c="dimmed">Replicas</Text>
          <Text fw={700} size="xl">{config.replicas}</Text>
        </Card>
      </SimpleGrid>

      <Card withBorder>
        <Title order={4} mb="sm">Configuration</Title>
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <Group><Text size="sm" c="dimmed">Subjects:</Text><Text size="sm">{config.subjects.join(', ')}</Text></Group>
          <Group><Text size="sm" c="dimmed">Max Messages:</Text><Text size="sm">{config.maxMessages === -1 ? 'Unlimited' : config.maxMessages.toLocaleString()}</Text></Group>
          <Group><Text size="sm" c="dimmed">Max Bytes:</Text><Text size="sm">{config.maxBytes === -1 ? 'Unlimited' : formatBytes(config.maxBytes)}</Text></Group>
          <Group><Text size="sm" c="dimmed">Discard Policy:</Text><Text size="sm">{config.discardPolicy}</Text></Group>
          <Group><Text size="sm" c="dimmed">Deny Delete:</Text><Text size="sm">{config.denyDelete ? 'Yes' : 'No'}</Text></Group>
          <Group><Text size="sm" c="dimmed">Deny Purge:</Text><Text size="sm">{config.denyPurge ? 'Yes' : 'No'}</Text></Group>
        </SimpleGrid>
      </Card>

      <Group>
        <Title order={3}>Consumers ({consumers.length})</Title>
        <Button leftSection={<IconPlus size={14} />} size="xs" onClick={() => setConsumerFormOpened(true)}>
          Add Consumer
        </Button>
      </Group>
      <ConsumerForm opened={consumerFormOpened} onClose={() => setConsumerFormOpened(false)} streamName={streamName} />
      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Deliver Policy</Table.Th>
            <Table.Th>Ack Policy</Table.Th>
            <Table.Th>Pending</Table.Th>
            <Table.Th>Ack Pending</Table.Th>
            <Table.Th>Health</Table.Th>
            <Table.Th>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {consumers.map((consumer) => (
            <Table.Tr key={consumer.name} style={{ cursor: 'pointer' }} onClick={() => onConsumerSelect(consumer.name)}>
              <Table.Td><Text fw={500}>{consumer.name}</Text></Table.Td>
              <Table.Td>{consumer.deliverPolicy}</Table.Td>
              <Table.Td>{consumer.ackPolicy}</Table.Td>
              <Table.Td>{consumer.numPending.toLocaleString()}</Table.Td>
              <Table.Td>{consumer.numAckPending.toLocaleString()}</Table.Td>
              <Table.Td><ConsumerHealthBadge isHealthy={consumer.isHealthy} /></Table.Td>
              <Table.Td onClick={(e) => e.stopPropagation()}>
                <ConsumerActions streamName={streamName} consumerName={consumer.name} />
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      <MessageBrowser streamName={streamName} />
    </Stack>
  );
}
