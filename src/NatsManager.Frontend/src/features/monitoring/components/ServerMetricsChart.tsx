import { Stack, Title } from '@mantine/core';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { EmptyState } from '../../../shared/EmptyState';
import { IconChartLine } from '@tabler/icons-react';
import type { MonitoringSnapshot } from '../types';
import { formatTime } from '../../../shared/formatting';

interface Props {
  snapshots: MonitoringSnapshot[];
}

function formatByteRate(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}MB/s`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(1)}KB/s`;
  return `${value.toFixed(0)}B/s`;
}

export function ServerMetricsChart({ snapshots }: Props) {
  const availableSnapshots = snapshots.filter(snapshot => snapshot.status !== 'Unavailable');

  if (availableSnapshots.length === 0) {
    return <EmptyState message="No monitoring data yet." icon={IconChartLine} />;
  }

  const chartData = [...availableSnapshots].reverse().map(snapshot => ({
    time: formatTime(snapshot.timestamp),
    connections: snapshot.server.connections,
    inMsgsPerSec: Number(snapshot.server.inMsgsPerSec.toFixed(2)),
    outMsgsPerSec: Number(snapshot.server.outMsgsPerSec.toFixed(2)),
    inBytesPerSec: snapshot.server.inBytesPerSec,
    outBytesPerSec: snapshot.server.outBytesPerSec,
  }));

  return (
    <Stack>
      <Title order={4}>Connections</Title>
      <ResponsiveContainer width="100%" height={200}><LineChart data={chartData}><CartesianGrid strokeDasharray="3 3" /><XAxis dataKey="time" tick={{ fontSize: 11 }} /><YAxis allowDecimals={false} /><Tooltip /><Legend /><Line type="monotone" dataKey="connections" stroke="#228be6" name="Connections" dot={false} /></LineChart></ResponsiveContainer>

      <Title order={4}>Message Rates</Title>
      <ResponsiveContainer width="100%" height={200}><LineChart data={chartData}><CartesianGrid strokeDasharray="3 3" /><XAxis dataKey="time" tick={{ fontSize: 11 }} /><YAxis /><Tooltip /><Legend /><Line type="monotone" dataKey="inMsgsPerSec" stroke="#40c057" name="In msgs/s" dot={false} /><Line type="monotone" dataKey="outMsgsPerSec" stroke="#fa8231" name="Out msgs/s" dot={false} /></LineChart></ResponsiveContainer>

      <Title order={4}>Byte Rates</Title>
      <ResponsiveContainer width="100%" height={200}><LineChart data={chartData}><CartesianGrid strokeDasharray="3 3" /><XAxis dataKey="time" tick={{ fontSize: 11 }} /><YAxis tickFormatter={formatByteRate} /><Tooltip formatter={(value) => typeof value === 'number' ? formatByteRate(value) : String(value)} /><Legend /><Line type="monotone" dataKey="inBytesPerSec" stroke="#7950f2" name="In bytes/s" dot={false} /><Line type="monotone" dataKey="outBytesPerSec" stroke="#e64980" name="Out bytes/s" dot={false} /></LineChart></ResponsiveContainer>
    </Stack>
  );
}
