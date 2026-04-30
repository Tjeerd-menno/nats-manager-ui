import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../../test-utils';
import { ClusterWarningList } from './ClusterWarningList';
import type { ClusterWarning } from '../types';

describe('ClusterWarningList', () => {
  it('does not render when there are no warnings', () => {
    const { container } = renderWithProviders(<ClusterWarningList warnings={[]} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('renders warning codes, severities, messages, and threshold details', () => {
    renderWithProviders(
      <ClusterWarningList
        warnings={[
          warning({
            code: 'HIGH_CONNECTIONS',
            severity: 'Warning',
            message: 'Connections are above the warning threshold.',
            currentValue: 90,
            thresholdValue: 80,
          }),
          warning({
            code: 'SERVER_DOWN',
            severity: 'Critical',
            message: 'A server is unavailable.',
          }),
        ]}
      />,
    );

    expect(screen.getByText('HIGH_CONNECTIONS')).toBeInTheDocument();
    expect(screen.getByText('Warning')).toBeInTheDocument();
    expect(screen.getByText('Connections are above the warning threshold.')).toBeInTheDocument();
    expect(screen.getByText('Current: 90 / Threshold: 80')).toBeInTheDocument();
    expect(screen.getByText('SERVER_DOWN')).toBeInTheDocument();
    expect(screen.getByText('Critical')).toBeInTheDocument();
    expect(screen.getByText('A server is unavailable.')).toBeInTheDocument();
  });
});

function warning(overrides: Partial<ClusterWarning> = {}): ClusterWarning {
  return {
    code: 'INFO',
    severity: 'Info',
    message: 'Informational warning.',
    serverId: null,
    metricName: null,
    currentValue: null,
    thresholdValue: null,
    ...overrides,
  };
}
