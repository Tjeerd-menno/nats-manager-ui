import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { EmptyFilterState } from './EmptyFilterState';

describe('EmptyFilterState', () => {
  it('explains the empty filter result and clears filters', async () => {
    const user = userEvent.setup();
    const onClearFilters = vi.fn();

    renderWithProviders(<EmptyFilterState onClearFilters={onClearFilters} />);

    expect(screen.getByText(/No relationships match the current filters/i)).toBeInTheDocument();
    expect(screen.getByText(/The focal resource is still available/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Clear filters' }));

    expect(onClearFilters).toHaveBeenCalledTimes(1);
  });
});
