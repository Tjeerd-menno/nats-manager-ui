import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../../test-utils';
import { ClusterServerList } from './ClusterServerList';
import { useClusterServers } from '../hooks/useClusterServers';
import type { ServerObservation } from '../types';

vi.mock('../hooks/useClusterServers', () => ({
  useClusterServers: vi.fn(),
}));

const mockUseClusterServers = vi.mocked(useClusterServers);

describe('ClusterServerList', () => {
  beforeEach(() => {
    vi.spyOn(Date, 'now').mockReturnValue(new Date('2026-01-02T03:05:00Z').getTime());
    mockUseClusterServers.mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders server metrics and relationship map actions', () => {
    mockUseClusterServers.mockReturnValue({
      servers: [
        serverObservation({
          serverId: 'srv-a',
          serverName: 'nats-a',
          version: '2.12.1',
          status: 'Healthy',
          connections: 8,
          slowConsumers: 1,
          memoryBytes: 1_073_741_824,
          inMsgsPerSecond: 12.345,
          outMsgsPerSecond: 6.7,
          lastObservedAt: '2026-01-02T03:04:15Z',
        }),
      ],
      isLoading: false,
      isError: false,
      error: null,
    });

    renderWithProviders(<ClusterServerList envId="env-1" />);

    expect(screen.getByText('Healthy')).toBeInTheDocument();
    expect(screen.getByText('nats-a')).toBeInTheDocument();
    expect(screen.getByText('srv-a')).toBeInTheDocument();
    expect(screen.getByText('2.12.1')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('1.0 GB')).toBeInTheDocument();
    expect(screen.getByText('12.3/s')).toBeInTheDocument();
    expect(screen.getByText('6.7/s')).toBeInTheDocument();
    expect(screen.getByText('45s ago')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Map' })).toBeInTheDocument();
  });

  it('passes search text to the server query and shows filtered empty state', async () => {
    const user = userEvent.setup();
    mockUseClusterServers.mockReturnValue({
      servers: [],
      isLoading: false,
      isError: false,
      error: null,
    });

    renderWithProviders(<ClusterServerList envId="env-1" />);

    expect(screen.getByText('No servers available.')).toBeInTheDocument();

    await user.type(screen.getByPlaceholderText('Search by server ID or name…'), 'missing');

    expect(mockUseClusterServers).toHaveBeenLastCalledWith('env-1', { search: 'missing' });
    expect(screen.getByText('No servers match your search.')).toBeInTheDocument();
  });
});

function serverObservation(overrides: Partial<ServerObservation> = {}): ServerObservation {
  return {
    environmentId: 'env-1',
    serverId: 'srv-a',
    serverName: null,
    clusterName: 'nats',
    version: null,
    uptimeSeconds: null,
    status: 'Healthy',
    freshness: 'Live',
    connections: null,
    maxConnections: null,
    slowConsumers: null,
    memoryBytes: null,
    storageBytes: null,
    inMsgsPerSecond: null,
    outMsgsPerSecond: null,
    inBytesPerSecond: null,
    outBytesPerSecond: null,
    lastObservedAt: '2026-01-02T03:04:05Z',
    metricStates: [],
    ...overrides,
  };
}
