import type { Freshness } from './common';

export type EnvironmentConnectionStatus = 'Available' | 'Unavailable' | 'Unknown' | 'Checking' | 'Degraded';

export interface EnvironmentStatusRecord {
  environmentId: string;
  displayName: string;
  connectionStatus: EnvironmentConnectionStatus;
  freshness: Freshness;
  lastSuccessfulInteractionUtc: string | null;
  lastCheckedAtUtc: string | null;
  lastErrorMessage: string | null;
}

export interface SelectedEnvironmentRecord {
  id: 'selected-environment';
  environmentId: string | null;
  selectedAtUtc: string | null;
}
