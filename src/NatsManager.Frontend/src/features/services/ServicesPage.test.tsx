import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import ServicesPage from './ServicesPage';

vi.mock('./hooks/useServices', () => ({
  useServices: vi.fn(),
  useService: vi.fn(() => ({ data: undefined, isLoading: false })),
  useTestService: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
}));

vi.mock('../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useParams: vi.fn(() => ({})),
    useNavigate: vi.fn(() => vi.fn()),
  };
});

import { useServices } from './hooks/useServices';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
const mockUseServices = vi.mocked(useServices);
const mockUseEnvironmentContext = vi.mocked(useEnvironmentContext);

describe('ServicesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows select environment message when no env selected', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: null,
      selectEnvironment: vi.fn(),
    });
    mockUseServices.mockReturnValue({
      data: undefined,
      isLoading: false,
    } as unknown as ReturnType<typeof useServices>);

    renderWithProviders(<ServicesPage />);
    expect(screen.getByText('Select an environment to view services.')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseServices.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as unknown as ReturnType<typeof useServices>);

    const { container } = renderWithProviders(<ServicesPage />);
    expect(container.querySelector('.mantine-Loader-root')).toBeInTheDocument();
  });

  it('renders service list with data', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseServices.mockReturnValue({
      data: [
        {
          id: 'svc-1',
          name: 'auth-service',
          version: '1.0.0',
          description: 'Auth',
          endpoints: [{ name: 'login', subject: 'auth.login' }],
          stats: { totalRequests: 100, totalErrors: 0, averageProcessingTime: 10 },
        },
      ],
      isLoading: false,
    } as unknown as ReturnType<typeof useServices>);

    renderWithProviders(<ServicesPage />);
    expect(screen.getByText('auth-service')).toBeInTheDocument();
    expect(screen.getByText('1.0.0')).toBeInTheDocument();
  });
});
