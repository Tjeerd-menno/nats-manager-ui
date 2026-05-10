import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { ContextHelp } from './ContextHelp';

describe('ContextHelp', () => {
  it('shows the built-in NATS concept description for known terms', async () => {
    const user = userEvent.setup();

    renderWithProviders(<ContextHelp term="ack-policy" description="Fallback description" />);

    await user.hover(screen.getByRole('button', { name: 'Help: ack-policy' }));

    expect(await screen.findByText(/Controls acknowledgment/i)).toBeInTheDocument();
  });

  it('falls back to the supplied description for unknown terms', async () => {
    const user = userEvent.setup();

    renderWithProviders(<ContextHelp term="custom-term" description="Custom explanatory text" />);

    await user.hover(screen.getByRole('button', { name: 'Help: custom-term' }));

    expect(await screen.findByText('Custom explanatory text')).toBeInTheDocument();
  });
});
