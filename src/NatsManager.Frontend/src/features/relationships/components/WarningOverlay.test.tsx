import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { WarningOverlay } from './WarningOverlay';

describe('WarningOverlay', () => {
  it('does not render a badge for healthy resources', () => {
    renderWithProviders(<WarningOverlay status="Healthy" />);

    expect(screen.queryByText('Healthy')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Resource status Healthy')).not.toBeInTheDocument();
  });

  it('renders an accessible badge for degraded resources', () => {
    renderWithProviders(<WarningOverlay status="Degraded" />);

    expect(screen.getByLabelText('Resource status Degraded')).toBeInTheDocument();
    expect(screen.getByText('Degraded')).toBeInTheDocument();
  });
});
