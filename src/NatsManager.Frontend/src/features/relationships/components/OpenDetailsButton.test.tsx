import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { OpenDetailsButton } from './OpenDetailsButton';
import type { ResourceNode } from '../types';

describe('OpenDetailsButton', () => {
  it('opens details for nodes that have a detail route', async () => {
    const user = userEvent.setup();
    const onOpenDetails = vi.fn();
    const node = resourceNode({ detailRoute: '/streams/orders' });

    renderWithProviders(<OpenDetailsButton node={node} onOpenDetails={onOpenDetails} />);

    await user.click(screen.getByRole('button', { name: 'Open details' }));

    expect(onOpenDetails).toHaveBeenCalledWith(node);
  });

  it('disables navigation when a node has no details page', () => {
    const onOpenDetails = vi.fn();

    renderWithProviders(
      <OpenDetailsButton node={resourceNode({ detailRoute: null })} onOpenDetails={onOpenDetails} />,
    );

    expect(screen.getByRole('button', { name: 'No details page' })).toBeDisabled();
    expect(onOpenDetails).not.toHaveBeenCalled();
  });
});

function resourceNode(overrides: Partial<ResourceNode> = {}): ResourceNode {
  return {
    nodeId: 'stream-orders',
    environmentId: 'env-1',
    resourceType: 'Stream',
    resourceId: 'ORDERS',
    displayName: 'ORDERS',
    status: 'Healthy',
    freshness: 'Live',
    isFocal: false,
    detailRoute: '/streams/orders',
    metadata: {},
    ...overrides,
  };
}
