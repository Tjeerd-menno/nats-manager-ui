export interface DashboardSummary {
  environment: EnvironmentHealth;
  jetStream: JetStreamSummary;
  keyValue: KvSummary;
  alerts: DashboardAlert[];
}

export interface EnvironmentHealth {
  connectionStatus: string;
  lastSuccessfulContact: string | null;
}

export interface JetStreamSummary {
  streamCount: number;
  consumerCount: number;
  unhealthyConsumers: number;
  totalMessages: number;
  totalBytes: number;
}

export interface KvSummary {
  bucketCount: number;
  totalKeys: number;
}

export interface DashboardAlert {
  severity: string;
  resourceType: string;
  resourceName: string;
  message: string;
}
