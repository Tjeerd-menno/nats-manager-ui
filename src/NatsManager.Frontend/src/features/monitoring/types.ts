export interface MonitoringSnapshot {
  environmentId: string;
  timestamp: string;
  server: ServerMetrics;
  jetStream: JetStreamMetrics | null;
  status: 'Ok' | 'Degraded' | 'Unavailable';
  healthStatus: 'Ok' | 'Degraded' | 'Unavailable';
}

export interface ServerMetrics {
  version: string;
  connections: number;
  totalConnections: number;
  maxConnections: number;
  inMsgsTotal: number;
  outMsgsTotal: number;
  inBytesTotal: number;
  outBytesTotal: number;
  inMsgsPerSec: number;
  outMsgsPerSec: number;
  inBytesPerSec: number;
  outBytesPerSec: number;
  uptimeSeconds: number;
  memoryBytes: number;
}

export interface JetStreamMetrics {
  streamCount: number;
  consumerCount: number;
  totalMessages: number;
  totalBytes: number;
}

export interface MonitoringHistoryResult {
  environmentId: string;
  snapshots: MonitoringSnapshot[];
}

export type MonitoringConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';
