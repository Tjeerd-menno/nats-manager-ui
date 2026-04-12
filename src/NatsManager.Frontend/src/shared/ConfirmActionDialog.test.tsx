import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { ConfirmActionDialog } from './ConfirmActionDialog';

describe('ConfirmActionDialog', () => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
    onConfirm: vi.fn(),
    title: 'Delete Stream',
    message: 'Are you sure you want to delete this stream?',
    resourceName: 'orders',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders confirmation dialog', () => {
    renderWithProviders(<ConfirmActionDialog {...defaultProps} />);
    expect(screen.getByText('Delete Stream')).toBeInTheDocument();
    expect(screen.getByText(/are you sure/i)).toBeInTheDocument();
  });

  it('confirm button is disabled until resource name is typed', () => {
    renderWithProviders(<ConfirmActionDialog {...defaultProps} />);
    const confirmButton = screen.getByRole('button', { name: /confirm|delete/i });
    expect(confirmButton).toBeDisabled();
  });

  it('enables confirm button when resource name matches', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ConfirmActionDialog {...defaultProps} />);

    const input = screen.getByRole('textbox');
    await user.type(input, 'orders');

    const confirmButton = screen.getByRole('button', { name: /confirm|delete/i });
    expect(confirmButton).toBeEnabled();
  });

  it('does not render when closed', () => {
    renderWithProviders(<ConfirmActionDialog {...defaultProps} opened={false} />);
    expect(screen.queryByText('Delete Stream')).not.toBeInTheDocument();
  });
});
