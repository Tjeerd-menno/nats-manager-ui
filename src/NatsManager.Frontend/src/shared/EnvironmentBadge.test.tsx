import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { EnvironmentBadge } from './EnvironmentBadge';

describe('EnvironmentBadge', () => {
  it('marks production environments explicitly', () => {
    renderWithProviders(
      <EnvironmentBadge name="Production" isProduction={true} connectionStatus="Available" />,
    );

    expect(screen.getByText('Production (PROD)')).toBeInTheDocument();
  });

  it('renders non-production environments without production suffix', () => {
    renderWithProviders(
      <EnvironmentBadge name="Development" isProduction={false} connectionStatus="Unavailable" />,
    );

    expect(screen.getByText('Development')).toBeInTheDocument();
    expect(screen.queryByText(/PROD/)).not.toBeInTheDocument();
  });
});
