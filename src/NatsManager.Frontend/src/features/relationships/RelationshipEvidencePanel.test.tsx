import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import { RelationshipEvidencePanel } from './RelationshipEvidencePanel';
import type { RelationshipEdge, ResourceNode } from './types';

describe('RelationshipEvidencePanel', () => {
  it('prompts users to select a relationship item when nothing is selected', () => {
    renderWithProviders(<RelationshipEvidencePanel selectedNode={null} selectedEdge={null} />);

    expect(screen.getByText('Click a node or edge to view details.')).toBeInTheDocument();
  });

  it('renders node details, metadata, and opens node details', async () => {
    const user = userEvent.setup();
    const onOpenDetails = vi.fn();
    const node = resourceNode({
      displayName: 'Orders stream',
      metadata: {
        subjects: 'orders.*',
        storage: 'File',
      },
      isFocal: true,
    });

    renderWithProviders(
      <RelationshipEvidencePanel selectedNode={node} onOpenDetails={onOpenDetails} />,
    );

    expect(screen.getByRole('heading', { name: 'Orders stream' })).toBeInTheDocument();
    expect(screen.getByText('Stream · ORDERS')).toBeInTheDocument();
    expect(screen.getByText('Healthy')).toBeInTheDocument();
    expect(screen.getByText('Live')).toBeInTheDocument();
    expect(screen.getByText('Focal')).toBeInTheDocument();
    expect(screen.getByText('Metadata')).toBeInTheDocument();
    expect(screen.getByText('subjects:')).toBeInTheDocument();
    expect(screen.getByText('orders.*')).toBeInTheDocument();
    expect(screen.getByText('storage:')).toBeInTheDocument();
    expect(screen.getByText('File')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Open details' }));

    expect(onOpenDetails).toHaveBeenCalledWith(node);
  });

  it('renders edge details and evidence summaries', () => {
    const edge = relationshipEdge();

    renderWithProviders(<RelationshipEvidencePanel selectedNode={null} selectedEdge={edge} />);

    expect(screen.getByRole('heading', { name: 'ConsumesFrom' })).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
    expect(screen.getByText('Outbound · Observed')).toBeInTheDocument();
    expect(screen.getByText('Partial')).toBeInTheDocument();
    expect(screen.getByText('Evidence (2)')).toBeInTheDocument();
    expect(screen.getByText('JetStream')).toBeInTheDocument();
    expect(screen.getByText('consumer-config')).toBeInTheDocument();
    expect(screen.getByText('Consumer ORDERS_WORKER reads from ORDERS.')).toBeInTheDocument();
    expect(screen.getByText('Monitoring')).toBeInTheDocument();
    expect(screen.getByText('runtime-observation')).toBeInTheDocument();
    expect(screen.getByText('Consumer has pending messages.')).toBeInTheDocument();
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

function relationshipEdge(): RelationshipEdge {
  return {
    edgeId: 'edge-1',
    environmentId: 'env-1',
    sourceNodeId: 'consumer-orders-worker',
    targetNodeId: 'stream-orders',
    relationshipType: 'ConsumesFrom',
    direction: 'Outbound',
    observationKind: 'Observed',
    confidence: 'High',
    freshness: 'Partial',
    status: 'Warning',
    evidence: [
      {
        sourceModule: 'JetStream',
        evidenceType: 'consumer-config',
        observedAt: '2026-01-02T03:04:05Z',
        freshness: 'Live',
        summary: 'Consumer ORDERS_WORKER reads from ORDERS.',
        safeFields: { consumer: 'ORDERS_WORKER' },
      },
      {
        sourceModule: 'Monitoring',
        evidenceType: 'runtime-observation',
        observedAt: '',
        freshness: 'Partial',
        summary: 'Consumer has pending messages.',
        safeFields: { pending: '42' },
      },
    ],
  };
}
