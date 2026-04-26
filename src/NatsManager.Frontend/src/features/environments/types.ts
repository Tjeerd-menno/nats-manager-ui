export interface EnvironmentListItem {
  id: string;
  name: string;
  description: string;
  isEnabled: boolean;
  isProduction: boolean;
  connectionStatus: ConnectionStatus;
  lastSuccessfulContact: string | null;
}

export interface EnvironmentDetail {
  id: string;
  name: string;
  description: string;
  serverUrl: string;
  credentialType: CredentialType;
  isEnabled: boolean;
  isProduction: boolean;
  connectionStatus: ConnectionStatus;
  lastSuccessfulContact: string | null;
  createdAt: string;
  updatedAt: string;
  monitoringUrl: string | null;
  monitoringPollingIntervalSeconds: number | null;
}

export interface RegisterEnvironmentRequest {
  name: string;
  description?: string;
  serverUrl: string;
  credentialType: CredentialType;
  credential?: string;
  isProduction: boolean;
}

export interface UpdateEnvironmentRequest {
  name: string;
  description?: string;
  serverUrl: string;
  credentialType: CredentialType;
  credential?: string;
  isProduction: boolean;
  isEnabled: boolean;
  monitoringUrl?: string | null;
  monitoringPollingIntervalSeconds?: number | null;
}

export interface RegisterEnvironmentResult {
  id: string;
  name: string;
}

export interface TestConnectionResult {
  reachable: boolean;
  latencyMs: number | null;
  serverVersion: string | null;
  jetStreamAvailable: boolean;
}

export type ConnectionStatus = 'Unknown' | 'Available' | 'Degraded' | 'Unavailable';

export type CredentialType = 'None' | 'Token' | 'UserPassword' | 'NKey' | 'CredsFile';
