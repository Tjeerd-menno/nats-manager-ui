import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { EnvironmentForm } from './EnvironmentForm';

vi.mock('../hooks/useEnvironments', () => ({
  useRegisterEnvironment: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useUpdateEnvironment: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useEnvironment: vi.fn(() => ({ data: undefined })),
}));

describe('EnvironmentForm', () => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders create form when no environment provided', () => {
    renderWithProviders(<EnvironmentForm {...defaultProps} />);
    expect(screen.getByText('Register Environment')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('production-us-east')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('nats://localhost:4222')).toBeInTheDocument();
  });

  it('renders edit form when environment is provided', () => {
    const env = {
      id: 'env-1',
      name: 'Production',
      description: 'Prod cluster',
      serverUrl: 'nats://prod:4222',
      connectionStatus: 'Available' as const,
      isProduction: true,
      isEnabled: true,
      lastSuccessfulContact: null,
    };

    renderWithProviders(<EnvironmentForm {...defaultProps} environment={env} />);
    expect(screen.getByText('Edit Environment')).toBeInTheDocument();
  });

  it('renders monitoring fields', () => {
    renderWithProviders(<EnvironmentForm {...defaultProps} />);

    expect(screen.getByLabelText('Monitoring URL')).toBeInTheDocument();
    expect(screen.getByLabelText('Polling Interval (seconds)')).toBeInTheDocument();
  });

  it('does not render when opened is false', () => {
    renderWithProviders(<EnvironmentForm {...defaultProps} opened={false} />);
    expect(screen.queryByText('Register Environment')).not.toBeInTheDocument();
  });
});
