import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import { RelationshipFilters } from './RelationshipFilters';
import type { MapFilter } from './types';

const defaultFilters: MapFilter = {
  depth: 1,
  resourceTypes: null,
  relationshipTypes: null,
  healthStates: null,
  minimumConfidence: 'Low',
  includeInferred: true,
  includeStale: true,
  maxNodes: 100,
  maxEdges: 500,
};

describe('RelationshipFilters', () => {
  it('renders map filter controls', () => {
    renderWithProviders(
      <RelationshipFilters filters={defaultFilters} onChange={vi.fn()} onClear={vi.fn()} />,
    );

    expect(screen.getByText('Filters')).toBeInTheDocument();
    expect(screen.getByText('Depth: 1')).toBeInTheDocument();
    expect(screen.getAllByLabelText('Resource types')[0]).toBeInTheDocument();
    expect(screen.getAllByLabelText('Relationship types')[0]).toBeInTheDocument();
    expect(screen.getAllByLabelText('Health states')[0]).toBeInTheDocument();
    expect(screen.getAllByLabelText('Minimum confidence')[0]).toBeInTheDocument();
    expect(screen.getByLabelText('Include inferred')).toBeChecked();
    expect(screen.getByLabelText('Include stale')).toBeChecked();
    expect(screen.getByRole('textbox', { name: 'Max nodes' })).toHaveValue('100');
    expect(screen.getByRole('textbox', { name: 'Max edges' })).toHaveValue('500');
  });

  it('updates boolean filters', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    renderWithProviders(
      <RelationshipFilters filters={defaultFilters} onChange={onChange} onClear={vi.fn()} />,
    );

    await user.click(screen.getByLabelText('Include stale'));

    expect(onChange).toHaveBeenCalledWith({ ...defaultFilters, includeStale: false });
  });

  it('clears filters', async () => {
    const user = userEvent.setup();
    const onClear = vi.fn();

    renderWithProviders(
      <RelationshipFilters filters={defaultFilters} onChange={vi.fn()} onClear={onClear} />,
    );

    await user.click(screen.getByRole('button', { name: 'Clear' }));

    expect(onClear).toHaveBeenCalledTimes(1);
  });
});
