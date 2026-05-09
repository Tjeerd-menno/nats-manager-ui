import { monitoringEnvironmentStateCollection, monitoringSnapshotsCollection } from '../collections';
import type { MonitoringSnapshot } from '../../features/monitoring/types';
import { writeMonitoringSnapshot } from './monitoring-hub-sync';

describe('monitoring read-model sync', () => {
  beforeEach(() => {
    clearMonitoringCollections();
  });

  it('upserts duplicate snapshots', () => {
    const snapshot = createSnapshot({ environmentId: 'env-1', timestamp: '2026-01-01T00:00:00.000Z' });

    writeMonitoringSnapshot(snapshot);
    writeMonitoringSnapshot({ ...snapshot, server: { ...snapshot.server, connections: 42 } });

    const records = monitoringSnapshotsCollection.toArray.filter(record => record.environmentId === 'env-1');
    expect(records).toHaveLength(1);
    expect(records[0]?.connections).toBe(42);
  });

  it('trims snapshots per environment without leaking between environments', () => {
    for (let index = 0; index < 121; index += 1) {
      writeMonitoringSnapshot(createSnapshot({
        environmentId: 'env-1',
        timestamp: new Date(Date.parse('2026-01-01T00:00:00.000Z') + index * 1000).toISOString(),
      }));
    }
    writeMonitoringSnapshot(createSnapshot({ environmentId: 'env-2', timestamp: '2026-01-01T00:00:00.000Z' }));

    expect(monitoringSnapshotsCollection.toArray.filter(record => record.environmentId === 'env-1')).toHaveLength(120);
    expect(monitoringSnapshotsCollection.toArray.filter(record => record.environmentId === 'env-2')).toHaveLength(1);
    expect(monitoringEnvironmentStateCollection.get('env-1')?.latestSnapshotId).toBe('env-1:2026-01-01T00:02:00.000Z');
  });
});

function clearMonitoringCollections() {
  const snapshotIds = monitoringSnapshotsCollection.toArray.map(record => record.id);
  if (snapshotIds.length > 0) monitoringSnapshotsCollection.delete(snapshotIds);

  const stateIds = monitoringEnvironmentStateCollection.toArray.map(record => record.environmentId);
  if (stateIds.length > 0) monitoringEnvironmentStateCollection.delete(stateIds);
}

function createSnapshot(overrides: Partial<MonitoringSnapshot> = {}): MonitoringSnapshot {
  return {
    environmentId: overrides.environmentId ?? 'env-1',
    timestamp: overrides.timestamp ?? '2026-01-01T00:00:00.000Z',
    server: {
      version: '2.10.0',
      connections: 1,
      totalConnections: 1,
      maxConnections: 100,
      inMsgsTotal: 0,
      outMsgsTotal: 0,
      inBytesTotal: 0,
      outBytesTotal: 0,
      inMsgsPerSec: 0,
      outMsgsPerSec: 0,
      inBytesPerSec: 0,
      outBytesPerSec: 0,
      uptimeSeconds: 0,
      memoryBytes: 0,
    },
    jetStream: null,
    status: 'Ok',
    healthStatus: 'Ok',
    ...overrides,
  };
}
