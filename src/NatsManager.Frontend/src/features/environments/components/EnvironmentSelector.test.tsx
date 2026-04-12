import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { EnvironmentSelector } from './EnvironmentSelector';

vi.mock('../hooks/useEnvironments', () => ({
  useEnvironments: vi.fn(() => ({
    data: {
      data: {
        items: [
          { id: 'env-1', name: 'Production', description: 'Prod', connectionStatus: 'Available' },
          { id: 'env-2', name: 'Staging', description: 'Stage', connectionStatus: 'Unknown' },
        ],
      },
    },
    isLoading: false,
  })),
}));

describe('EnvironmentSelector', () => {
  it('renders select input', () => {
    renderWithProviders(
      <EnvironmentSelector selectedId={null} onSelect={vi.fn()} />,
    );
    expect(screen.getByPlaceholderText('Select environment')).toBeInTheDocument();
  });

  it('renders with environment options available', () => {
    renderWithProviders(
      <EnvironmentSelector selectedId="env-1" onSelect={vi.fn()} />,
    );
    expect(screen.getByPlaceholderText('Select environment')).toBeInTheDocument();
  });
});
