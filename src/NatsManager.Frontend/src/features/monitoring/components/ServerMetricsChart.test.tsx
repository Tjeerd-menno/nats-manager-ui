import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { ServerMetricsChart } from './ServerMetricsChart';
import type { MonitoringSnapshot } from '../types';

describe('ServerMetricsChart', () => {
  it('shows an empty state when no snapshots are available', () => {
    renderWithProviders(<ServerMetricsChart snapshots={[]} />);

    expect(screen.getByText('No monitoring data yet.')).toBeInTheDocument();
  });

  it('renders connection, message-rate, and byte-rate panels', () => {
    renderWithProviders(
      <ServerMetricsChart
        snapshots={[
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
