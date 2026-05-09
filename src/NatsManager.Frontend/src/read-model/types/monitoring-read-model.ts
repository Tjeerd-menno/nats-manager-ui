import type { HealthStatus, ReadModelConnectionStatus } from './common';

export interface MonitoringSnapshotRecord {
  id: string;
  environmentId: string;
  observedAtUtc: string;
  receivedAtUtc: string;

  healthStatus: HealthStatus;
  status: Exclude<HealthStatus, 'Unknown'>;
  connectionStatus: ReadModelConnectionStatus;

  serverVersion: string | null;
  connections: number;
  totalConnections: number;
  maxConnections: number;
  subscriptions: number | null;

  inMessagesPerSecond: number;
  outMessagesPerSecond: number;
  inBytesPerSecond: number;
  outBytesPerSecond: number;
  inMessagesTotal: number;
  outMessagesTotal: number;
  inBytesTotal: number;
  outBytesTotal: number;

  memoryBytes: number | null;
  cpuPercentage: number | null;
  uptimeSeconds: number | null;

  jetStreamEnabled: boolean | null;
  streamCount: number | null;
  consumerCount: number | null;
  totalMessages: number | null;
  totalBytes: number | null;
}

export interface MonitoringEnvironmentStateRecord {
  environmentId: string;
  connectionStatus: ReadModelConnectionStatus;
  healthStatus: HealthStatus;
  latestSnapshotId: string | null;
  lastReceivedAtUtc: string | null;
  staleSinceUtc: string | null;
  errorMessage: string | null;
  isNotConfigured: boolean;
}
