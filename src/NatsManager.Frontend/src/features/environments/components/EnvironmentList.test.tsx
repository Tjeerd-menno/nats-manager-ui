import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { EnvironmentList } from './EnvironmentList';

vi.mock('../hooks/useEnvironments', () => ({
  useEnvironments: vi.fn(),
  useDeleteEnvironment: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useTestConnection: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useEnableDisableEnvironment: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}));

import { useEnvironments } from '../hooks/useEnvironments';
const mockUseEnvironments = vi.mocked(useEnvironments);

const mockEnv = {
  id: 'env-1',
  name: 'Production',
  description: 'Prod NATS cluster',
  serverUrl: 'nats://prod:4222',
  connectionStatus: 'Available' as const,
  isProduction: true,
  isEnabled: true,
  lastSuccessfulContact: new Date().toISOString(),
};

describe('EnvironmentList', () => {
  const defaultProps = {
    onEdit: vi.fn(),
    onCreate: vi.fn(),
    onSelect: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state when data is loading', () => {
    mockUseEnvironments.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as ReturnType<typeof useEnvironments>);

    renderWithProviders(<EnvironmentList {...defaultProps} />);
    expect(screen.getByText('Loading environments...')).toBeInTheDocument();
  });

  it('shows empty state when no environments exist', () => {
    mockUseEnvironments.mockReturnValue({
      data: { data: { items: [], totalPages: 0, totalCount: 0, page: 1, pageSize: 25 } },
      isLoading: false,
    } as unknown as ReturnType<typeof useEnvironments>);

    renderWithProviders(<EnvironmentList {...defaultProps} />);
    expect(screen.getByText('No environments registered yet')).toBeInTheDocument();
  });

  it('renders environment list with data', async () => {
    mockUseEnvironments.mockReturnValue({
      data: {
        data: {
          items: [mockEnv],
          totalPages: 1,
          totalCount: 1,
          page: 1,
          pageSize: 25,
        },
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useEnvironments>);

    renderWithProviders(<EnvironmentList {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText('Production')).toBeInTheDocument();
    });
    expect(screen.getByText('Prod NATS cluster')).toBeInTheDocument();
  });

  it('renders Register Environment button', () => {
    mockUseEnvironments.mockReturnValue({
      data: {
        data: {
          items: [mockEnv],
          totalPages: 1,
          totalCount: 1,
          page: 1,
          pageSize: 25,
        },
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useEnvironments>);

    renderWithProviders(<EnvironmentList {...defaultProps} />);
    expect(screen.getByText('Register Environment')).toBeInTheDocument();
  });
});
