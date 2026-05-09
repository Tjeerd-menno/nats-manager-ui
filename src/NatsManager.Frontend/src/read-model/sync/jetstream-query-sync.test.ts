import { consumersCollection, resourceIndexCollection, streamsCollection } from '../collections';
import { syncConsumer, syncStream } from './jetstream-query-sync';

describe('JetStream read-model sync', () => {
  beforeEach(() => {
    clearCollection(streamsCollection, record => record.id);
    clearCollection(consumersCollection, record => record.id);
    clearCollection(resourceIndexCollection, record => record.id);
  });

  it('reconciles stream and consumer metadata from backend-confirmed data', () => {
    syncStream('env-1', {
      name: 'ORDERS',
      description: 'Order events',
      subjects: ['orders.*'],
      retentionPolicy: 'Limits',
      storageType: 'File',
      messages: 10,
      bytes: 1024,
      consumerCount: 1,
      created: '2026-01-01T00:00:00.000Z',
    });
    syncConsumer('env-1', 'ORDERS', {
      streamName: 'ORDERS',
      name: 'worker',
      description: null,
      deliverPolicy: 'All',
      ackPolicy: 'Explicit',
      filterSubject: null,
      numPending: 5,
      numAckPending: 0,
      numRedelivered: 0,
      isHealthy: true,
      created: '2026-01-01T00:00:00.000Z',
      state: {
        delivered: 10,
        ackFloor: 5,
        numPending: 5,
        numAckPending: 0,
        numRedelivered: 0,
      },
    });

    expect(streamsCollection.get('env-1:Stream:ORDERS')?.messages).toBe(10);
    expect(consumersCollection.get('env-1:Consumer:ORDERS:worker')?.pendingMessages).toBe(5);
    expect(resourceIndexCollection.get('env-1:Consumer:ORDERS:worker')?.resourceType).toBe('Consumer');
  });
});

function clearCollection<T extends object>(collection: { toArray: T[]; delete: (keys: string[]) => unknown }, getKey: (record: T) => string) {
  const ids = collection.toArray.map(getKey);
  if (ids.length > 0) collection.delete(ids);
}
