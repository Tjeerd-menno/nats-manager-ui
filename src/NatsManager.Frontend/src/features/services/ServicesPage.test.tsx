import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import ServicesPage from './ServicesPage';

vi.mock('./hooks/useServices', () => ({
  useServices: vi.fn(),
  useService: vi.fn(() => ({ data: undefined, isLoading: false })),
  useTestService: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
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

import { useServices, useService, useTestService } from './hooks/useServices';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { useParams } from 'react-router-dom';
const mockUseServices = vi.mocked(useServices);
const mockUseService = vi.mocked(useService);
const mockUseTestService = vi.mocked(useTestService);
const mockUseEnvironmentContext = vi.mocked(useEnvironmentContext);
const mockUseParams = vi.mocked(useParams);

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

  it('blocks test request when subject is invalid', async () => {
    const user = userEvent.setup();
    mockUseParams.mockReturnValue({ serviceName: 'test-svc' });
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseService.mockReturnValue({
      data: {
        name: 'test-svc',
        version: '1.0.0',
        description: 'A test service',
        endpoints: [{ name: 'ping', subject: 'test.ping', queueGroup: null }],
        stats: { totalRequests: 0, totalErrors: 0, averageProcessingTime: 0 },
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useService>);
    const mutate = vi.fn();
    mockUseTestService.mockReturnValue({
      mutate,
      isPending: false,
    } as unknown as ReturnType<typeof useTestService>);

    renderWithProviders(<ServicesPage />);

    await user.click(screen.getByRole('button', { name: 'Test Request' }));

    const subjectInput = await screen.findByRole('textbox', { name: /subject/i });
    await user.type(subjectInput, 'invalid subject');

    expect(await screen.findByText('Subject must not contain whitespace')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled();
    expect(mutate).not.toHaveBeenCalled();
  });
});
