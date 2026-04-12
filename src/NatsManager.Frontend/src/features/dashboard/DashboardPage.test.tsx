import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import DashboardPage from './DashboardPage';

vi.mock('./hooks/useDashboard', () => ({
  useDashboard: vi.fn(),
}));

vi.mock('../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(),
}));

import { useDashboard } from './hooks/useDashboard';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
const mockUseDashboard = vi.mocked(useDashboard);
const mockUseEnvironmentContext = vi.mocked(useEnvironmentContext);

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows select environment message when no environment selected', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: null,
      selectEnvironment: vi.fn(),
    });
    mockUseDashboard.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useDashboard>);

    renderWithProviders(<DashboardPage />);
    expect(screen.getByText('Select an environment to view the dashboard.')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseDashboard.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as unknown as ReturnType<typeof useDashboard>);

    const { container } = renderWithProviders(<DashboardPage />);
    expect(container.querySelector('.mantine-Loader-root')).toBeInTheDocument();
  });

  it('renders dashboard with data', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseDashboard.mockReturnValue({
      data: {
        environment: { status: 'Available', name: 'Production', version: '2.10.0' },
        jetStream: { streamCount: 5, consumerCount: 12, unhealthyConsumers: 0, totalMessages: 10000, totalBytes: 1048576 },
        keyValue: { bucketCount: 3, totalKeys: 50 },
        alerts: [],
      },
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useDashboard>);

    renderWithProviders(<DashboardPage />);
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('12 consumers')).toBeInTheDocument();
  });

  it('shows error when data fails to load', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseDashboard.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('fetch failed'),
    } as unknown as ReturnType<typeof useDashboard>);

    renderWithProviders(<DashboardPage />);
    expect(screen.getByText('Failed to load dashboard data.')).toBeInTheDocument();
  });
});
