import { Fragment, useState } from 'react';
import { Card, Group, Stack, Text, Table, NumberInput, Button, Code, Badge, Collapse } from '@mantine/core';
import { IconChevronRight, IconChevronDown } from '@tabler/icons-react';
import { useStreamMessages } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { LoadingState } from '../../../shared/LoadingState';
import { EmptyState } from '../../../shared/EmptyState';
import { formatBytes, formatDateTime } from '../../../shared/formatting';

interface MessageBrowserProps {
  streamName: string;
}

export function MessageBrowser({ streamName }: MessageBrowserProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const [startSequence, setStartSequence] = useState<number | undefined>(undefined);
  const [count, setCount] = useState(25);
  const [expandedSeq, setExpandedSeq] = useState<number | null>(null);

  const { data: messages, isLoading, refetch } = useStreamMessages(
    selectedEnvironmentId,
    streamName,
    startSequence,
    count,
  );

  const toggleExpand = (seq: number) => {
    setExpandedSeq(expandedSeq === seq ? null : seq);
  };

  return (
    <Card withBorder>
      <Stack>
        <Text fw={600} size="lg">Message Browser</Text>
        <Group>
          <NumberInput
            label="Start Sequence"
            placeholder="From beginning"
            value={startSequence ?? ''}
            onChange={(val) => setStartSequence(val === '' ? undefined : Number(val))}
            min={0}
            style={{ width: 180 }}
          />
          <NumberInput
            label="Count"
            value={count}
            onChange={(val) => setCount(Number(val) || 25)}
            min={1}
            max={100}
            style={{ width: 120 }}
          />
          <Button mt="xl" onClick={() => void refetch()}>
            Fetch
          </Button>
        </Group>

        {isLoading && <LoadingState message="Fetching messages..." />}

        {!isLoading && (!messages || messages.length === 0) && (
          <EmptyState message="No messages found" />
        )}

        {messages && messages.length > 0 && (
          <Table striped>
            <Table.Thead>
              <Table.Tr>
                <Table.Th style={{ width: 40 }} />
                <Table.Th>Sequence</Table.Th>
                <Table.Th>Subject</Table.Th>
                <Table.Th>Timestamp</Table.Th>
                <Table.Th>Size</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {messages.map((msg) => (
                <Fragment key={msg.sequence}>
                  <Table.Tr key={msg.sequence} style={{ cursor: 'pointer' }} onClick={() => toggleExpand(msg.sequence)}>
                    <Table.Td>
                      {expandedSeq === msg.sequence ? <IconChevronDown size={16} /> : <IconChevronRight size={16} />}
                    </Table.Td>
                    <Table.Td><Text fw={500}>{msg.sequence}</Text></Table.Td>
                    <Table.Td><Badge variant="light" size="sm">{msg.subject}</Badge></Table.Td>
                    <Table.Td>{formatDateTime(msg.timestamp)}</Table.Td>
                    <Table.Td>{formatBytes(msg.size)}</Table.Td>
                  </Table.Tr>
                  <Table.Tr key={`${msg.sequence}-detail`} style={{ display: expandedSeq === msg.sequence ? undefined : 'none' }}>
                    <Table.Td colSpan={5}>
                      <Collapse expanded={expandedSeq === msg.sequence}>
                        <Stack gap="xs" p="sm">
                          {Object.keys(msg.headers).length > 0 && (
                            <div>
                              <Text size="sm" fw={500} mb={4}>Headers</Text>
                              {Object.entries(msg.headers).map(([key, value]) => (
                                <Group key={key} gap="xs">
                                  <Text size="xs" c="dimmed">{key}:</Text>
                                  <Text size="xs">{value}</Text>
                                </Group>
                              ))}
                            </div>
                          )}
                          <div>
                            <Text size="sm" fw={500} mb={4}>Payload</Text>
                            <Code block style={{ maxHeight: 300, overflow: 'auto' }}>
                              {msg.data ?? '(empty)'}
                            </Code>
                          </div>
                        </Stack>
                      </Collapse>
                    </Table.Td>
                  </Table.Tr>
                </Fragment>
              ))}
            </Table.Tbody>
          </Table>
        )}
      </Stack>
    </Card>
  );
}
