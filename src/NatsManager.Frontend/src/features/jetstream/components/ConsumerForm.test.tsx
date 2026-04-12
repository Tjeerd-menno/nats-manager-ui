import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { ConsumerForm } from './ConsumerForm';

vi.mock('../hooks/useJetStream', () => ({
  useCreateConsumer: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
}));

vi.mock('../../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: 'env-1',
    selectEnvironment: vi.fn(),
  })),
}));

describe('ConsumerForm', () => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
    streamName: 'orders',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders create consumer form', () => {
    renderWithProviders(<ConsumerForm {...defaultProps} />);
    expect(screen.getByText('Create Consumer')).toBeInTheDocument();
  });

  it('does not render when closed', () => {
    renderWithProviders(<ConsumerForm {...defaultProps} opened={false} />);
    expect(screen.queryByText('Create Consumer')).not.toBeInTheDocument();
  });
});
