export type HealthStatus = 'Ok' | 'Degraded' | 'Unavailable' | 'Unknown';
export type Freshness = 'Live' | 'Recent' | 'Stale' | 'Unknown';
export type ReadModelConnectionStatus = 'Connected' | 'Connecting' | 'Disconnected' | 'Reconnecting';

export interface FreshnessFields {
  observedAtUtc: string | null;
  receivedAtUtc: string | null;
  freshness: Freshness;
}

export const FRESHNESS_THRESHOLDS = {
  liveMs: 10_000,
  recentMs: 60_000,
} as const;

export const READ_MODEL_UNKNOWN_ENVIRONMENT_ID = '__no-environment__';
