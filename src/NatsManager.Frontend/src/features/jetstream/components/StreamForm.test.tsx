import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { StreamForm } from './StreamForm';

vi.mock('../hooks/useJetStream', () => ({
  useCreateStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useUpdateStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
}));

vi.mock('../../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: 'env-1',
    selectEnvironment: vi.fn(),
  })),
}));

describe('StreamForm', () => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders create stream form', () => {
    renderWithProviders(<StreamForm {...defaultProps} />);
    expect(screen.getByText('Create Stream')).toBeInTheDocument();
  });

  it('renders edit stream form when existingConfig provided', () => {
    const config = {
      name: 'orders',
      description: 'Order events',
      subjects: ['orders.>'],
      retentionPolicy: 'Limits',
      maxMessages: -1,
      maxBytes: -1,
      maxAge: 0,
      storageType: 'File',
      replicas: 1,
      discardPolicy: 'Old',
      maxMsgSize: -1,
      denyDelete: false,
      denyPurge: false,
      allowRollup: false,
    };

    renderWithProviders(<StreamForm {...defaultProps} existingConfig={config} />);
    expect(screen.getByText('Update Stream')).toBeInTheDocument();
  });

  it('does not render when closed', () => {
    renderWithProviders(<StreamForm {...defaultProps} opened={false} />);
    expect(screen.queryByText('Create Stream')).not.toBeInTheDocument();
  });
});
