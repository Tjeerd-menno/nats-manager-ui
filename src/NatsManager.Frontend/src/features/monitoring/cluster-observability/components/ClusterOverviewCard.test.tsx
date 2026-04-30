import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../../test-utils';
import { ClusterOverviewCard } from './ClusterOverviewCard';
import type { ClusterObservation } from '../types';

describe('ClusterOverviewCard', () => {
  it('renders cluster summary metrics and status badges', () => {
    const observation = clusterObservation({
      status: 'Healthy',
      freshness: 'Live',
      serverCount: 3,
      degradedServerCount: 0,
      jetStreamAvailable: true,
      connectionCount: 1250,
      inMsgsPerSecond: 2500,
      outMsgsPerSecond: 1_500_000,
    });

    renderWithProviders(<ClusterOverviewCard observation={observation} />);

    expect(screen.getByText('Cluster Overview')).toBeInTheDocument();
    expect(screen.getByText('Healthy')).toBeInTheDocument();
    expect(screen.getByText('Live')).toBeInTheDocument();
    expect(screen.getByText('Observed')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('0')).toBeInTheDocument();
    expect(screen.getByText('Enabled')).toBeInTheDocument();
    expect(screen.getByText('1,250')).toBeInTheDocument();
    expect(screen.getByText('2.5K/s')).toBeInTheDocument();
    expect(screen.getByText('1.5M/s')).toBeInTheDocument();
  });

  it('renders placeholders for unknown optional metrics', () => {
    const observation = clusterObservation({
      status: 'Unknown',
      freshness: 'Unavailable',
      jetStreamAvailable: null,
      connectionCount: null,
      inMsgsPerSecond: null,
      outMsgsPerSecond: null,
    });

    renderWithProviders(<ClusterOverviewCard observation={observation} />);

    expect(screen.getAllByText('Unknown')).toHaveLength(2);
    expect(screen.getByText('Unavailable')).toBeInTheDocument();
    expect(screen.getAllByText('—')).toHaveLength(3);
  });
});

function clusterObservation(overrides: Partial<ClusterObservation> = {}): ClusterObservation {
  return {
    environmentId: 'env-1',
    observedAt: '2026-01-02T03:04:05Z',
    status: 'Healthy',
    freshness: 'Live',
    serverCount: 1,
    degradedServerCount: 0,
    jetStreamAvailable: false,
    connectionCount: 0,
    inMsgsPerSecond: 0,
    outMsgsPerSecond: 0,
    servers: [],
    topology: [],
    warnings: [],
    ...overrides,
  };
}
