import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { JetStreamMetricsCard } from './JetStreamMetricsCard';
import type { MonitoringSnapshot } from '../types';

describe('JetStreamMetricsCard', () => {
  it('shows an unavailable message when JetStream metrics are absent', () => {
    renderWithProviders(<JetStreamMetricsCard snapshots={[createSnapshot({ jetStream: null })]} />);

    expect(screen.getByText('JetStream is not enabled or not available for this environment.')).toBeInTheDocument();
  });

  it('renders latest JetStream totals and deltas', () => {
    renderWithProviders(
      <JetStreamMetricsCard
        snapshots={[
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
    expect(screen.getByText('1.5K')).toBeInTheDocument();
    expect(screen.getByText('+500')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'JetStream Trends' })).toBeInTheDocument();
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
