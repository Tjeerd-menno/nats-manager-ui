import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { StaleDataBanner } from './StaleDataBanner';

describe('StaleDataBanner', () => {
  it('does not render for available connections', () => {
    renderWithProviders(<StaleDataBanner connectionStatus="Available" />);

    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('shows unreachable environment messaging with the last update timestamp', () => {
    const lastUpdated = '2026-01-02T03:04:05Z';

    renderWithProviders(<StaleDataBanner connectionStatus="Unavailable" lastUpdated={lastUpdated} />);

    expect(screen.getByText('Environment Unreachable')).toBeInTheDocument();
    expect(screen.getByText(new RegExp(new Date(lastUpdated).toLocaleString()))).toBeInTheDocument();
  });

  it('shows degraded connection messaging when the connection is degraded', () => {
    renderWithProviders(<StaleDataBanner connectionStatus="Degraded" />);

    expect(screen.getByText('Degraded Connection')).toBeInTheDocument();
    expect(screen.getByText(/Last successful contact: unknown/i)).toBeInTheDocument();
  });
});
