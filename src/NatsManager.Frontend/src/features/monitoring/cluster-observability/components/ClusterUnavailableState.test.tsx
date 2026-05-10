import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../../test-utils';
import { ClusterUnavailableState } from './ClusterUnavailableState';

describe('ClusterUnavailableState', () => {
  it('renders the default unavailable message without retry action', () => {
    renderWithProviders(<ClusterUnavailableState />);

    expect(screen.getByText('Cluster observability data is currently unavailable.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retry' })).not.toBeInTheDocument();
  });

  it('renders custom guidance and invokes retry', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();

    renderWithProviders(
      <ClusterUnavailableState message="Monitoring endpoint timed out." onRetry={onRetry} />,
    );

    expect(screen.getByText('Monitoring endpoint timed out.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Retry' }));

    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
