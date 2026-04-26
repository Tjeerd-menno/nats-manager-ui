import { Card, SimpleGrid, Text, Stack, Title, Alert, Group } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import type { MonitoringSnapshot } from '../types';

interface Props {
  snapshots: MonitoringSnapshot[];
}

function formatLarge(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return n.toString();
}

function formatBytes(n: number): string {
  if (n >= 1_073_741_824) return `${(n / 1_073_741_824).toFixed(1)} GB`;
  if (n >= 1_048_576) return `${(n / 1_048_576).toFixed(1)} MB`;
  if (n >= 1_024) return `${(n / 1_024).toFixed(1)} KB`;
  return `${n} B`;
}

function formatDelta(n: number): string {
  if (n === 0) return 'No change';
  return `${n > 0 ? '+' : ''}${formatLarge(n)}`;
}

function DeltaText({ value }: { value: number }) {
  return (
    <Text size="xs" c={value > 0 ? 'green' : value < 0 ? 'red' : 'dimmed'}>
      {formatDelta(value)}
    </Text>
  );
}

export function JetStreamMetricsCard({ snapshots }: Props) {
  const latestJetStreamSnapshot = snapshots.find(s => s.status !== 'Unavailable' && s.jetStream !== null);
  const js = latestJetStreamSnapshot?.jetStream ?? null;

  if (!js) {
    return (
      <Alert icon={<IconInfoCircle size={16} />} color="blue">
        JetStream is not enabled or not available for this environment.
      </Alert>
    );
  }

  const jetStreamSnapshots = snapshots
    .filter(s => s.status !== 'Unavailable' && s.jetStream !== null);
  const previous = jetStreamSnapshots.find(s => s.timestamp !== latestJetStreamSnapshot?.timestamp)?.jetStream ?? null;
  const streamDelta = previous ? js.streamCount - previous.streamCount : 0;
  const consumerDelta = previous ? js.consumerCount - previous.consumerCount : 0;
  const messageDelta = previous ? js.totalMessages - previous.totalMessages : 0;
  const byteDelta = previous ? js.totalBytes - previous.totalBytes : 0;

  const chartData = [...jetStreamSnapshots].reverse()
    .map(s => ({
      time: new Date(s.timestamp).toLocaleTimeString(),
      totalMessages: s.jetStream!.totalMessages,
      totalBytes: s.jetStream!.totalBytes,
    }));

  return (
    <Stack>
      <Title order={4}>JetStream</Title>
      <SimpleGrid cols={{ base: 2, sm: 4 }}>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Streams</Text>
          <Group justify="space-between" align="end">
            <Text size="xl" fw={700}>{js.streamCount}</Text>
            <DeltaText value={streamDelta} />
          </Group>
        </Card>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Consumers</Text>
          <Group justify="space-between" align="end">
            <Text size="xl" fw={700}>{js.consumerCount}</Text>
            <DeltaText value={consumerDelta} />
          </Group>
        </Card>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Messages</Text>
          <Group justify="space-between" align="end">
            <Text size="xl" fw={700}>{formatLarge(js.totalMessages)}</Text>
            <DeltaText value={messageDelta} />
          </Group>
        </Card>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Bytes</Text>
          <Group justify="space-between" align="end">
            <Text size="xl" fw={700}>{formatBytes(js.totalBytes)}</Text>
            <DeltaText value={byteDelta} />
          </Group>
        </Card>
      </SimpleGrid>

      {chartData.length > 1 && (
        <>
          <Title order={5}>JetStream Trends</Title>
          <ResponsiveContainer width="100%" height={160}>
            <AreaChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={formatLarge} />
              <Tooltip />
              <Area type="monotone" dataKey="totalMessages" stroke="#7950f2" fill="#e5dbff" name="Total Messages" />
              <Area type="monotone" dataKey="totalBytes" stroke="#228be6" fill="#d0ebff" name="Total Bytes" />
            </AreaChart>
          </ResponsiveContainer>
        </>
      )}
    </Stack>
  );
}
