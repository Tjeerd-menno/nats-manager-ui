import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import MonitoringPage from './MonitoringPage';

vi.mock('../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(),
}));

vi.mock('./hooks/useMonitoringHub', () => ({
  useMonitoringHub: vi.fn(),
}));

vi.mock('./components/ServerMetricsChart', () => ({
  ServerMetricsChart: () => <div>server chart</div>,
}));

vi.mock('./components/JetStreamMetricsCard', () => ({
  JetStreamMetricsCard: () => <div>jetstream card</div>,
}));

import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { useMonitoringHub } from './hooks/useMonitoringHub';

const mockUseEnvironmentContext = vi.mocked(useEnvironmentContext);
const mockUseMonitoringHub = vi.mocked(useMonitoringHub);

describe('MonitoringPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows not configured empty state', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseMonitoringHub.mockReturnValue({
      snapshots: [],
      latestSnapshot: null,
      connectionStatus: 'disconnected',
      isNotConfigured: true,
      error: 'Monitoring is not configured for this environment.',
    });

    renderWithProviders(<MonitoringPage />);

    expect(screen.getByText('Monitoring is not configured for this environment. Edit the environment to add a Monitoring URL.')).toBeInTheDocument();
  });

  it('shows unavailable alert while keeping charts rendered', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseMonitoringHub.mockReturnValue({
      snapshots: [snapshot('Unavailable')],
      latestSnapshot: snapshot('Unavailable'),
      connectionStatus: 'connected',
      isNotConfigured: false,
      error: null,
    });

    renderWithProviders(<MonitoringPage />);

    expect(screen.getByText('Monitoring Endpoint Unavailable')).toBeInTheDocument();
    expect(screen.getByText('server chart')).toBeInTheDocument();
    expect(screen.getByText('jetstream card')).toBeInTheDocument();
  });
});

function snapshot(status: 'Ok' | 'Degraded' | 'Unavailable') {
  return {
    environmentId: 'env-1',
    timestamp: '2026-01-01T00:00:00Z',
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
      uptimeSeconds: 0,
      memoryBytes: 0,
    },
    jetStream: null,
    status,
    healthStatus: status,
  };
}
