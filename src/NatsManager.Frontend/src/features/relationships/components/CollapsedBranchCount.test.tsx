import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { CollapsedBranchCount } from './CollapsedBranchCount';
import type { OmittedCounts } from '../types';

const noOmissions: OmittedCounts = {
  filteredNodes: 0,
  filteredEdges: 0,
  collapsedNodes: 0,
  collapsedEdges: 0,
  unsafeRelationships: 0,
};

describe('CollapsedBranchCount', () => {
  it('does not render when no branches are collapsed', () => {
    renderWithProviders(
      <CollapsedBranchCount
        omittedCounts={noOmissions}
        maxNodes={100}
        maxEdges={500}
        onIncreaseLimits={vi.fn()}
      />,
    );

    expect(screen.queryByText('Branches collapsed')).not.toBeInTheDocument();
  });

  it('summarizes collapsed branches and increases bounds', async () => {
    const user = userEvent.setup();
    const onIncreaseLimits = vi.fn();

    renderWithProviders(
      <CollapsedBranchCount
        omittedCounts={{ ...noOmissions, collapsedNodes: 3, collapsedEdges: 7 }}
        maxNodes={50}
        maxEdges={200}
        onIncreaseLimits={onIncreaseLimits}
      />,
    );

    expect(screen.getByText('Branches collapsed')).toBeInTheDocument();
    expect(screen.getByText('3 node(s) and 7 edge(s) were collapsed by the current bounds.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Show more' }));

    expect(onIncreaseLimits).toHaveBeenCalledWith({ maxNodes: 75, maxEdges: 300 });
  });
});
