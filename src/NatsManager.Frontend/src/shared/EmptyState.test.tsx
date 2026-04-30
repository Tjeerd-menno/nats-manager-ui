import { Button, type ElementProps } from '@mantine/core';
import type { IconProps } from '@tabler/icons-react';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { EmptyState } from './EmptyState';

function TestIcon(props: IconProps & ElementProps<'svg'>) {
  return <svg aria-label="custom empty icon" {...props} />;
}

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
