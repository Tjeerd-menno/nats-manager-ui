import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { ConnectionStatusBadge } from './ConnectionStatusBadge';

describe('ConnectionStatusBadge', () => {
  it('renders Available status with correct text', () => {
    renderWithProviders(<ConnectionStatusBadge status="Available" />);
    expect(screen.getByText('Available')).toBeInTheDocument();
  });

  it('renders Degraded status', () => {
    renderWithProviders(<ConnectionStatusBadge status="Degraded" />);
    expect(screen.getByText('Degraded')).toBeInTheDocument();
  });

  it('renders Unavailable status', () => {
    renderWithProviders(<ConnectionStatusBadge status="Unavailable" />);
    expect(screen.getByText('Unavailable')).toBeInTheDocument();
  });

  it('renders Unknown status', () => {
    renderWithProviders(<ConnectionStatusBadge status="Unknown" />);
    expect(screen.getByText('Unknown')).toBeInTheDocument();
  });
});
