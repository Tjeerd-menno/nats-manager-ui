import { screen, within } from '@testing-library/react';
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
    expect(metricValue('Servers')).toHaveTextContent('3');
    expect(metricValue('Degraded')).toHaveTextContent('0');
    expect(screen.getByText('Enabled')).toBeInTheDocument();
    expect(metricValue('Connections')).toHaveTextContent('1,250');
    expect(metricValue('In Msgs')).toHaveTextContent('2.5K/s');
    expect(metricValue('Out Msgs')).toHaveTextContent('1.5M/s');
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

    expect(screen.getByText('Unknown')).toBeInTheDocument();
    expect(screen.getByText('Unavailable')).toBeInTheDocument();
    expect(metricValue('Connections')).toHaveTextContent('—');
    expect(metricValue('In Msgs')).toHaveTextContent('—');
    expect(metricValue('Out Msgs')).toHaveTextContent('—');
  });
});

function metricValue(label: string): HTMLElement {
  const labelElement = screen.getByText(label);
  const section = labelElement.closest('.mantine-Stack-root');

  expect(section).not.toBeNull();
  return within(section as HTMLElement).getByText((content) => content !== label && content.length > 0);
}

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
