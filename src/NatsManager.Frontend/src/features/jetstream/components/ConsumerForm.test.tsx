import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { ConsumerForm } from './ConsumerForm';
import { useCreateConsumer } from '../hooks/useJetStream';

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

  it('validates consumer name and filter subject before submitting', async () => {
    const user = userEvent.setup();
    const createConsumer = vi.fn();
    vi.mocked(useCreateConsumer).mockReturnValue({
      mutateAsync: createConsumer,
      isPending: false,
    } as unknown as ReturnType<typeof useCreateConsumer>);

    renderWithProviders(<ConsumerForm {...defaultProps} />);

    await user.type(screen.getByPlaceholderText('my-consumer'), 'consumer#bad');
    await user.type(screen.getByPlaceholderText('Optional subject filter'), 'orders with spaces');
    await user.click(screen.getByRole('button', { name: 'Create' }));

    expect(await screen.findByText('Consumer name can only contain letters, numbers, dots, hyphens, and underscores')).toBeInTheDocument();
    expect(await screen.findByText('Filter subject must not contain whitespace')).toBeInTheDocument();
    expect(createConsumer).not.toHaveBeenCalled();
  });
});
