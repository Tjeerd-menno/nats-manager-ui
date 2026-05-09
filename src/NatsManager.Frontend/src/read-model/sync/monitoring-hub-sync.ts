import { monitoringEnvironmentStateCollection, monitoringSnapshotsCollection, resourceIndexCollection } from '../collections';
import type { MonitoringSnapshot } from '../../features/monitoring/types';
import type { MonitoringSnapshotRecord } from '../types/monitoring-read-model';
import type { HealthStatus, ReadModelConnectionStatus } from '../types/common';
import { makeEnvironmentScopedId, trimCollectionByEnvironment, upsertRecord } from '../utils/collection-utils';

export const MAX_MONITORING_SNAPSHOTS_PER_ENVIRONMENT = 120;

export function toMonitoringSnapshotRecord(
  snapshot: MonitoringSnapshot,
  connectionStatus: ReadModelConnectionStatus,
  receivedAtUtc = new Date().toISOString(),
): MonitoringSnapshotRecord {
  const observedAtUtc = snapshot.timestamp;
  const healthStatus = normalizeHealthStatus(snapshot.healthStatus);

  return {
    id: getMonitoringSnapshotId(snapshot.environmentId, observedAtUtc),
    environmentId: snapshot.environmentId,
    observedAtUtc,
    receivedAtUtc,
    healthStatus,
    status: snapshot.status,
    connectionStatus,
    serverVersion: snapshot.server.version,
    connections: snapshot.server.connections,
    totalConnections: snapshot.server.totalConnections,
    maxConnections: snapshot.server.maxConnections,
    subscriptions: null,
    inMessagesPerSecond: snapshot.server.inMsgsPerSec,
    outMessagesPerSecond: snapshot.server.outMsgsPerSec,
    inBytesPerSecond: snapshot.server.inBytesPerSec,
    outBytesPerSecond: snapshot.server.outBytesPerSec,
    inMessagesTotal: snapshot.server.inMsgsTotal,
    outMessagesTotal: snapshot.server.outMsgsTotal,
    inBytesTotal: snapshot.server.inBytesTotal,
    outBytesTotal: snapshot.server.outBytesTotal,
    memoryBytes: snapshot.server.memoryBytes,
    cpuPercentage: null,
    uptimeSeconds: snapshot.server.uptimeSeconds,
    jetStreamEnabled: snapshot.jetStream !== null,
    streamCount: snapshot.jetStream?.streamCount ?? null,
    consumerCount: snapshot.jetStream?.consumerCount ?? null,
    totalMessages: snapshot.jetStream?.totalMessages ?? null,
    totalBytes: snapshot.jetStream?.totalBytes ?? null,
  };
}

export function toMonitoringSnapshot(record: MonitoringSnapshotRecord): MonitoringSnapshot {
  return {
    environmentId: record.environmentId,
    timestamp: record.observedAtUtc,
    server: {
      version: record.serverVersion ?? '',
      connections: record.connections,
      totalConnections: record.totalConnections,
      maxConnections: record.maxConnections,
      inMsgsTotal: record.inMessagesTotal,
      outMsgsTotal: record.outMessagesTotal,
      inBytesTotal: record.inBytesTotal,
      outBytesTotal: record.outBytesTotal,
      inMsgsPerSec: record.inMessagesPerSecond,
      outMsgsPerSec: record.outMessagesPerSecond,
      inBytesPerSec: record.inBytesPerSecond,
      outBytesPerSec: record.outBytesPerSecond,
      uptimeSeconds: record.uptimeSeconds ?? 0,
      memoryBytes: record.memoryBytes ?? 0,
    },
    jetStream: record.jetStreamEnabled
      ? {
          streamCount: record.streamCount ?? 0,
          consumerCount: record.consumerCount ?? 0,
          totalMessages: record.totalMessages ?? 0,
          totalBytes: record.totalBytes ?? 0,
        }
      : null,
    status: record.status,
    healthStatus: record.healthStatus === 'Unknown' ? 'Unavailable' : record.healthStatus,
  };
}

export function writeMonitoringSnapshot(
  snapshot: MonitoringSnapshot,
  connectionStatus: ReadModelConnectionStatus = 'Connected',
  receivedAtUtc = new Date().toISOString(),
) {
  const record = toMonitoringSnapshotRecord(snapshot, connectionStatus, receivedAtUtc);
  upsertRecord(monitoringSnapshotsCollection, record.id, record);
  trimCollectionByEnvironment<MonitoringSnapshotRecord>(
    monitoringSnapshotsCollection,
    record.environmentId,
    MAX_MONITORING_SNAPSHOTS_PER_ENVIRONMENT,
    item => item.observedAtUtc,
    item => item.id,
  );

  upsertRecord(monitoringEnvironmentStateCollection, record.environmentId, {
    environmentId: record.environmentId,
    connectionStatus,
    healthStatus: record.healthStatus,
    latestSnapshotId: record.id,
    lastReceivedAtUtc: record.receivedAtUtc,
    staleSinceUtc: null,
    errorMessage: null,
    isNotConfigured: false,
  });

  upsertRecord(resourceIndexCollection, makeEnvironmentScopedId(record.environmentId, 'Server', record.serverVersion ?? 'server'), {
    id: makeEnvironmentScopedId(record.environmentId, 'Server', record.serverVersion ?? 'server'),
    environmentId: record.environmentId,
    resourceType: 'Server',
    resourceId: record.serverVersion ?? 'server',
    name: record.serverVersion ?? 'Server',
    displayName: record.serverVersion ? `NATS ${record.serverVersion}` : 'NATS Server',
    description: `${record.connections} active connections`,
    status: record.healthStatus,
    freshness: 'Live',
    detailRoute: `/environments/${record.environmentId}/monitoring`,
    searchableText: ['server', 'monitoring', record.serverVersion, record.healthStatus].filter(Boolean).join(' ').toLowerCase(),
    lastObservedAtUtc: record.observedAtUtc,
    indexedAtUtc: receivedAtUtc,
  });
}

export function writeMonitoringHistory(environmentId: string, snapshots: MonitoringSnapshot[]) {
  for (const snapshot of snapshots) {
    writeMonitoringSnapshot(snapshot, 'Connected', snapshot.timestamp);
  }

  if (snapshots.length === 0) {
    writeMonitoringState(environmentId, 'Connected', null, false);
  }
}

export function writeMonitoringState(
  environmentId: string,
  connectionStatus: ReadModelConnectionStatus,
  errorMessage: string | null,
  isNotConfigured: boolean,
) {
  const existing = monitoringEnvironmentStateCollection.get(environmentId);
  upsertRecord(monitoringEnvironmentStateCollection, environmentId, {
    environmentId,
    connectionStatus,
    healthStatus: existing?.healthStatus ?? (isNotConfigured ? 'Unavailable' : 'Unknown'),
    latestSnapshotId: existing?.latestSnapshotId ?? null,
    lastReceivedAtUtc: existing?.lastReceivedAtUtc ?? null,
    staleSinceUtc: connectionStatus === 'Disconnected' ? new Date().toISOString() : null,
    errorMessage,
    isNotConfigured,
  });
}

export function getMonitoringSnapshotId(environmentId: string, observedAtUtc: string) {
  return makeEnvironmentScopedId(environmentId, observedAtUtc);
}

function normalizeHealthStatus(status: MonitoringSnapshot['healthStatus']): HealthStatus {
  return status;
}
