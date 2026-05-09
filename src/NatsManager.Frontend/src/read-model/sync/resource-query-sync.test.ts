import { resourceIndexCollection } from '../collections';
import { syncSearchResults } from './resource-query-sync';

describe('resource index sync', () => {
  beforeEach(() => {
    const ids = resourceIndexCollection.toArray.map(record => record.id);
    if (ids.length > 0) resourceIndexCollection.delete(ids);
  });

  it('indexes server search results for local lookup', () => {
    syncSearchResults([
      {
        resourceType: 'Stream',
        resourceId: 'ORDERS',
        displayName: 'ORDERS',
        environmentId: 'env-1',
        environmentName: 'Local',
        description: 'Order events',
      },
    ], '2026-01-01T00:00:00.000Z');

    const record = resourceIndexCollection.get('env-1:Stream:ORDERS');
    expect(record?.searchableText).toContain('order events');
    expect(record?.resourceType).toBe('Stream');
  });
});
