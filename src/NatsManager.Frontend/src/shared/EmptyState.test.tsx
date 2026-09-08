import { Button } from '@mantine/core';
import type { IconProps } from '@tabler/icons-react';
import type { FunctionComponent } from 'react';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { EmptyState } from './EmptyState';

// EmptyState only needs an Icon-shaped component; the test asserts on the label,
// so the tabler-specific props (size/stroke as numbers) are deliberately not forwarded.
const TestIcon: FunctionComponent<IconProps> = () => <svg aria-label="custom empty icon" />;

describe('EmptyState', () => {
  it('renders the default empty message', () => {
    renderWithProviders(<EmptyState />);

    expect(screen.getByText('No items found')).toBeInTheDocument();
  });

  it('renders custom message, icon, and action', () => {
    renderWithProviders(
      <EmptyState
        message="No streams found"
        icon={TestIcon}
        action={<Button>Create stream</Button>}
      />,
    );

    expect(screen.getByText('No streams found')).toBeInTheDocument();
    expect(screen.getByLabelText('custom empty icon')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create stream' })).toBeInTheDocument();
  });
});
