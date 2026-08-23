import { screen } from '@testing-library/react';
import { Children, isValidElement, type ReactNode } from 'react';
import { renderWithProviders } from '../../../test-utils';
import { ServerMetricsChart } from './ServerMetricsChart';
import type { MonitoringSnapshot } from '../types';

type ServerChartDataPoint = {
  time: string;
  connections: number;
  inMsgsPerSec: number;
  outMsgsPerSec: number;
  inBytesPerSec: number;
  outBytesPerSec: number;
};

function getChartTestIdFromSeriesNames(children: ReactNode): string {
  const names = Children.toArray(children)
    .map((child) => (isValidElement<{ name?: string }>(child) ? child.props.name : undefined))
    .filter((name): name is string => !!name);

  if (names.includes('In bytes/s')) {
    return 'byte-rates-chart';
  }

  if (names.includes('In msgs/s')) {
    return 'message-rates-chart';
  }

  return 'connections-chart';
}

vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  LineChart: ({ data, children }: { data: unknown; children: ReactNode }) => (
    <div data-testid={getChartTestIdFromSeriesNames(children)} data-chart={JSON.stringify(data)}>
      {children}
    </div>
  ),
  Line: ({ name }: { name: string }) => <div>{name}</div>,
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: () => null,
  Legend: () => null,
}));

describe('ServerMetricsChart', () => {
  it('shows an empty state when no snapshots are available', () => {
    renderWithProviders(<ServerMetricsChart snapshots={[]} />);

    expect(screen.getByText('No monitoring data yet.')).toBeInTheDocument();
  });

  it('renders connection, message-rate, and byte-rate panels with byte-rate series', () => {
    renderWithProviders(
      <ServerMetricsChart
        snapshots={[
          createSnapshot({
            status: 'Unavailable',
            server: {
              ...createSnapshot().server,
              connections: 9,
            },
          }),
          createSnapshot({
            server: {
              ...createSnapshot().server,
              connections: 4,
              inMsgsPerSec: 10.12,
              outMsgsPerSec: 5.5,
              inBytesPerSec: 2048,
              outBytesPerSec: 1024,
            },
          }),
        ]}
      />
    );

    expect(screen.getByRole('heading', { name: 'Connections' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Message Rates' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Byte Rates' })).toBeInTheDocument();
    expect(screen.getAllByText('Connections')).toHaveLength(2);
    expect(screen.getByText('In msgs/s')).toBeInTheDocument();
    expect(screen.getByText('Out msgs/s')).toBeInTheDocument();
    expect(screen.getByText('In bytes/s')).toBeInTheDocument();
    expect(screen.getByText('Out bytes/s')).toBeInTheDocument();

    const chartData = JSON.parse(screen.getByTestId('byte-rates-chart').getAttribute('data-chart') ?? '[]') as ServerChartDataPoint[];

    expect(chartData).toHaveLength(1);
    expect(chartData[0]).toEqual(expect.objectContaining({
      connections: 4,
      inMsgsPerSec: 10.12,
      outMsgsPerSec: 5.5,
      inBytesPerSec: 2048,
      outBytesPerSec: 1024,
    }));
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
    jetStream: null,
    ...overrides,
  };
}
