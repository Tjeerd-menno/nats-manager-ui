import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithSpeedyGloveProviders as render } from '../test-utils';
import { SaveDeviceDialog } from './SaveDeviceDialog';

describe('SaveDeviceDialog', () => {
  const onClose = vi.fn();
  const onSave = vi.fn();

  const defaultProps = {
    isOpen: true,
    onClose,
    onSave,
  };

  it('renders the dialog title and disabled submit button initially', () => {
    render(<SaveDeviceDialog {...defaultProps} />);

    expect(screen.getByRole('dialog', { name: /save device/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save/i })).toBeDisabled();
  });

  it('enables submit after entering a valid name and submits the form', async () => {
    const user = userEvent.setup();

    render(<SaveDeviceDialog {...defaultProps} />);

    await user.type(screen.getByRole('textbox', { name: /device name/i }), 'Dose Calibrator A');

    const saveButton = screen.getByRole('button', { name: /save/i });
    expect(saveButton).toBeEnabled();

    await user.click(saveButton);

    expect(onSave).toHaveBeenCalledWith({
      name: 'Dose Calibrator A',
    });
  });

  it('shows a validation message when the name is required', async () => {
    const user = userEvent.setup();

    render(<SaveDeviceDialog {...defaultProps} />);

    await user.click(screen.getByRole('button', { name: /save/i }));

    expect(screen.getByText(/field is required/i)).toBeInTheDocument();
    expect(onSave).not.toHaveBeenCalled();
  });

  it('calls onClose when cancel is clicked', async () => {
    const user = userEvent.setup();

    render(<SaveDeviceDialog {...defaultProps} />);

    await user.click(screen.getByRole('button', { name: /cancel/i }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
