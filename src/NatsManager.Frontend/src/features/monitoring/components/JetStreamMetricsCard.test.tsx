import { screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { renderWithProviders } from '../../../test-utils';
import { JetStreamMetricsCard } from './JetStreamMetricsCard';
import type { MonitoringSnapshot } from '../types';

type JetStreamChartDataPoint = {
  time: string;
  totalMessages: number;
  totalBytes: number;
};

vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  AreaChart: ({ data, children }: { data: unknown; children: ReactNode }) => (
    <div data-testid="jetstream-area-chart" data-chart={JSON.stringify(data)}>
      {children}
    </div>
  ),
  Area: ({ name }: { name: string }) => <div>{name}</div>,
  CartesianGrid: () => null,
  XAxis: () => null,
  YAxis: () => null,
  Tooltip: () => null,
}));

describe('JetStreamMetricsCard', () => {
  it('shows an unavailable message when JetStream metrics are absent', () => {
    renderWithProviders(<JetStreamMetricsCard snapshots={[createSnapshot({ jetStream: null })]} />);

    expect(screen.getByText('JetStream is not enabled or not available for this environment.')).toBeInTheDocument();
  });

  it('renders latest usable JetStream totals and chart trends', () => {
    renderWithProviders(
      <JetStreamMetricsCard
        snapshots={[
          createSnapshot({
            timestamp: '2026-05-03T12:02:00Z',
            status: 'Unavailable',
            jetStream: null,
          }),
          createSnapshot({
            timestamp: '2026-05-03T12:01:00Z',
            jetStream: { streamCount: 3, consumerCount: 5, totalMessages: 1500, totalBytes: 4096 },
          }),
          createSnapshot({
            timestamp: '2026-05-03T12:00:00Z',
            jetStream: { streamCount: 2, consumerCount: 4, totalMessages: 1000, totalBytes: 1024 },
          }),
        ]}
      />
    );

    expect(screen.getByRole('heading', { name: 'JetStream' })).toBeInTheDocument();
    expect(screen.getByText('Streams')).toBeInTheDocument();
    expect(screen.getByText('Consumers')).toBeInTheDocument();
    expect(screen.getByText('Messages')).toBeInTheDocument();
    expect(screen.getByText('Bytes')).toBeInTheDocument();
    expect(screen.getByText('4 KB')).toBeInTheDocument();
    expect(screen.getByText('1.5K')).toBeInTheDocument();
    expect(screen.getByText('+500')).toBeInTheDocument();
    expect(screen.getByText('+3.1K')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'JetStream Trends' })).toBeInTheDocument();
    expect(screen.getByText('Total Messages')).toBeInTheDocument();
    expect(screen.getByText('Total Bytes')).toBeInTheDocument();

    const chartData = JSON.parse(screen.getByTestId('jetstream-area-chart').getAttribute('data-chart') ?? '[]') as JetStreamChartDataPoint[];

    expect(chartData).toHaveLength(2);
    expect(chartData.map((point) => point.totalMessages)).toEqual([1000, 1500]);
    expect(chartData.map((point) => point.totalBytes)).toEqual([1024, 4096]);
  });
});

function createSnapshot(overrides: Partial<MonitoringSnapshot> = {}): MonitoringSnapshot {
  return {
    environmentId: 'env-1',
    timestamp: '2026-05-03T12:00:00Z',
    status: 'Ok',
    healthStatus: 'Ok',
    server: {
      version: '2.10.0',
      connections: 1,
      totalConnections: 1,
      maxConnections: 100,
      inMsgsTotal: 0,
      outMsgsTotal: 0,
      inBytesTotal: 0,
      outBytesTotal: 0,
      inMsgsPerSec: 0,
      outMsgsPerSec: 0,
      inBytesPerSec: 0,
      outBytesPerSec: 0,
      uptimeSeconds: 60,
      memoryBytes: 1024,
    },
    jetStream: { streamCount: 1, consumerCount: 1, totalMessages: 1, totalBytes: 1 },
    ...overrides,
  };
}
