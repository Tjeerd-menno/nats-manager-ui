import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { ConsumerHealthBadge } from './ConsumerHealthBadge';

describe('ConsumerHealthBadge', () => {
  it('renders Healthy when isHealthy is true', () => {
    renderWithProviders(<ConsumerHealthBadge isHealthy={true} />);
    expect(screen.getByText('Healthy')).toBeInTheDocument();
  });

  it('renders Unhealthy when isHealthy is false', () => {
    renderWithProviders(<ConsumerHealthBadge isHealthy={false} />);
    expect(screen.getByText('Unhealthy')).toBeInTheDocument();
  });
});
