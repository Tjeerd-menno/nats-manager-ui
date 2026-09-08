import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { EnvironmentForm } from './EnvironmentForm';
import { useEnvironment, useRegisterEnvironment, useUpdateEnvironment } from '../hooks/useEnvironments';

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

  it('submits monitoring settings when editing an environment', async () => {
    const user = userEvent.setup();
    const update = vi.fn().mockResolvedValue(undefined);
    vi.mocked(useUpdateEnvironment).mockReturnValue({
      mutateAsync: update,
      isPending: false,
    } as unknown as ReturnType<typeof useUpdateEnvironment>);
    vi.mocked(useEnvironment).mockReturnValue({
      data: {
        id: 'env-1',
        name: 'Production',
        description: 'Prod cluster',
        serverUrl: 'nats://prod:4222',
        credentialType: 'None',
        isEnabled: true,
        isProduction: true,
        connectionStatus: 'Available',
        lastSuccessfulContact: null,
        createdAt: '2026-05-01T00:00:00Z',
        updatedAt: '2026-05-01T00:00:00Z',
        monitoringUrl: 'http://localhost:8222',
        monitoringPollingIntervalSeconds: 30,
      },
    } as unknown as ReturnType<typeof useEnvironment>);

    renderWithProviders(
      <EnvironmentForm
        {...defaultProps}
        environment={{
          id: 'env-1',
          name: 'Production',
          description: 'Prod cluster',
          connectionStatus: 'Available',
          isProduction: true,
          isEnabled: true,
          lastSuccessfulContact: null,
        }}
      />,
    );

    const monitoringUrlInput = await screen.findByDisplayValue('http://localhost:8222');
    const pollingIntervalInput = screen.getByLabelText('Polling Interval (seconds)');

    await user.clear(monitoringUrlInput);
    await user.type(monitoringUrlInput, 'https://monitoring.example.com:8222');
    await user.clear(pollingIntervalInput);
    await user.type(pollingIntervalInput, '15');
    await user.click(screen.getByRole('button', { name: 'Update' }));

    await waitFor(() => {
      expect(update).toHaveBeenCalledWith(expect.objectContaining({
        id: 'env-1',
        monitoringUrl: 'https://monitoring.example.com:8222',
        monitoringPollingIntervalSeconds: 15,
      }));
    });
  });

  it('validates monitoring URL input', async () => {
    const user = userEvent.setup();
    renderWithProviders(<EnvironmentForm {...defaultProps} />);

    await user.type(screen.getByPlaceholderText('production-us-east'), 'Local');
    await user.type(screen.getByPlaceholderText('nats://localhost:4222'), 'nats://localhost:4222');
    await user.type(screen.getByLabelText('Monitoring URL'), 'nats://localhost:8222');
    await user.click(screen.getByRole('button', { name: 'Register' }));

    expect(await screen.findByText('Monitoring URL must use http:// or https://')).toBeInTheDocument();
    expect(screen.getByText('Override default polling interval (5–300 seconds)')).toBeInTheDocument();
  });

  it('validates NATS server URL input before submitting', async () => {
    const user = userEvent.setup();
    const register = vi.fn();
    vi.mocked(useRegisterEnvironment).mockReturnValue({
      mutateAsync: register,
      isPending: false,
    } as unknown as ReturnType<typeof useRegisterEnvironment>);

    renderWithProviders(<EnvironmentForm {...defaultProps} />);

    await user.type(screen.getByPlaceholderText('production-us-east'), 'Local');
    await user.type(screen.getByPlaceholderText('nats://localhost:4222'), 'tcp://localhost:60933');
    await user.click(screen.getByRole('button', { name: 'Register' }));

    expect(await screen.findByText('Server URL must use nats://, tls://, ws://, or wss://. Use nats:// for standard TCP NATS endpoints.')).toBeInTheDocument();
    expect(register).not.toHaveBeenCalled();
  });

  it('does not render when opened is false', () => {
    renderWithProviders(<EnvironmentForm {...defaultProps} opened={false} />);
    expect(screen.queryByText('Register Environment')).not.toBeInTheDocument();
  });
});
