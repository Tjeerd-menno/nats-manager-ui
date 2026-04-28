import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import CoreNatsPage from './CoreNatsPage';

vi.mock('./hooks/useCoreNats', () => ({
  useCoreNatsStatus: vi.fn(),
  usePublishMessage: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useSubjects: vi.fn(() => ({
    data: [],
    isLoading: false,
    error: null,
    isMonitoringAvailable: true,
  })),
  useLiveMessages: vi.fn(() => ({
    messages: [],
    isConnected: false,
    isPaused: false,
    pendingCount: 0,
    cap: 100,
    setCap: vi.fn(),
    subscribe: vi.fn(),
    unsubscribe: vi.fn(),
    pause: vi.fn(),
    resume: vi.fn(),
    clear: vi.fn(),
  })),
}));

vi.mock('../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(),
}));

import { useCoreNatsStatus } from './hooks/useCoreNats';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
const mockUseCoreNatsStatus = vi.mocked(useCoreNatsStatus);
const mockUseEnvironmentContext = vi.mocked(useEnvironmentContext);

describe('CoreNatsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows select environment message when no env selected', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: null,
      selectEnvironment: vi.fn(),
    });
    mockUseCoreNatsStatus.mockReturnValue({
      data: undefined,
      isLoading: false,
    } as unknown as ReturnType<typeof useCoreNatsStatus>);

    renderWithProviders(<CoreNatsPage />);
    expect(screen.getByText('Select an environment to view NATS server info.')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseCoreNatsStatus.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as unknown as ReturnType<typeof useCoreNatsStatus>);

    const { container } = renderWithProviders(<CoreNatsPage />);
    expect(container.querySelector('.mantine-Loader-root')).toBeInTheDocument();
  });

  it('renders server info when loaded', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseCoreNatsStatus.mockReturnValue({
      data: {
        serverId: 'NATS-SRV-001',
        serverName: 'nats-cluster-1',
        version: '2.10.0',
        host: 'localhost',
        port: 4222,
        connections: 42,
        maxPayload: 1048576,
        jetStreamEnabled: true,
        inMsgs: 50000,
        inBytes: 1048576,
        outMsgs: 40000,
        outBytes: 819200,
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useCoreNatsStatus>);

    renderWithProviders(<CoreNatsPage />);
    expect(screen.getByText('Core NATS')).toBeInTheDocument();
    expect(screen.getByText('nats-cluster-1')).toBeInTheDocument();
    expect(screen.getByText('Publish Message')).toBeInTheDocument();
  });

  it('shows error when server info not available', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseCoreNatsStatus.mockReturnValue({
      data: undefined,
      isLoading: false,
    } as unknown as ReturnType<typeof useCoreNatsStatus>);

    renderWithProviders(<CoreNatsPage />);
    expect(screen.getByText('Unable to retrieve server information.')).toBeInTheDocument();
  });
});
