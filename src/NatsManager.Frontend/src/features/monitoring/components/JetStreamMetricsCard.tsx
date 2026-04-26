import { Card, SimpleGrid, Text, Stack, Title, Alert } from '@mantine/core';
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
  latestSnapshot: MonitoringSnapshot | null;
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

export function JetStreamMetricsCard({ snapshots, latestSnapshot }: Props) {
  const js = latestSnapshot?.jetStream ?? null;

  if (!js) {
    return (
      <Alert icon={<IconInfoCircle size={16} />} color="blue">
        JetStream is not enabled or not available for this environment.
      </Alert>
    );
  }

  const chartData = [...snapshots].reverse()
    .filter(s => s.jetStream !== null)
    .map(s => ({
      time: new Date(s.timestamp).toLocaleTimeString(),
      totalMessages: s.jetStream!.totalMessages,
    }));

  return (
    <Stack>
      <Title order={4}>JetStream</Title>
      <SimpleGrid cols={{ base: 2, sm: 4 }}>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Streams</Text>
          <Text size="xl" fw={700}>{js.streamCount}</Text>
        </Card>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Consumers</Text>
          <Text size="xl" fw={700}>{js.consumerCount}</Text>
        </Card>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Messages</Text>
          <Text size="xl" fw={700}>{formatLarge(js.totalMessages)}</Text>
        </Card>
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Text size="sm" c="dimmed">Bytes</Text>
          <Text size="xl" fw={700}>{formatBytes(js.totalBytes)}</Text>
        </Card>
      </SimpleGrid>

      {chartData.length > 1 && (
        <>
          <Title order={5}>Message Trend</Title>
          <ResponsiveContainer width="100%" height={160}>
            <AreaChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={formatLarge} />
              <Tooltip />
              <Area type="monotone" dataKey="totalMessages" stroke="#7950f2" fill="#e5dbff" name="Total Messages" />
            </AreaChart>
          </ResponsiveContainer>
        </>
      )}
    </Stack>
  );
}
