import { useEffect, useMemo, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import { apiClient } from '../../../api/client';
import type { MonitoringSnapshot, MonitoringHistoryResult, MonitoringConnectionStatus } from '../types';
import { useMonitoringEnvironmentState, useRecentMonitoringSnapshots } from '../../../read-model/hooks';
import {
  toMonitoringSnapshot,
  writeMonitoringHistory,
  writeMonitoringSnapshot,
  writeMonitoringState,
} from '../../../read-model/sync/monitoring-hub-sync';
import type { ReadModelConnectionStatus } from '../../../read-model/types/common';

interface UseMonitoringHubResult {
  snapshots: MonitoringSnapshot[];
  latestSnapshot: MonitoringSnapshot | null;
  connectionStatus: MonitoringConnectionStatus;
  isNotConfigured: boolean;
  error: string | null;
}

export function useMonitoringHub(environmentId: string | null): UseMonitoringHubResult {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const snapshotsQuery = useRecentMonitoringSnapshots(environmentId, 120);
  const stateQuery = useMonitoringEnvironmentState(environmentId);
  const [localConnectionStatus, setLocalConnectionStatus] = useState<MonitoringConnectionStatus>('connecting');
  const [localIsNotConfigured, setLocalIsNotConfigured] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!environmentId) return;

    let cancelled = false;

    const start = async () => {
      setLocalConnectionStatus('connecting');
      setLocalIsNotConfigured(false);
      setLocalError(null);
      writeMonitoringState(environmentId, 'Connecting', null, false);

      // Fetch initial history
      try {
        const response = await apiClient.get<MonitoringHistoryResult>(
          `/environments/${environmentId}/monitoring/metrics/history`
        );
        if (!cancelled) {
          writeMonitoringHistory(environmentId, response.data.snapshots);
        }
      } catch (err) {
        if (axios.isAxiosError(err) && err.response?.status === 400) {
          if (!cancelled) {
            setLocalIsNotConfigured(true);
            setLocalConnectionStatus('disconnected');
            setLocalError('Monitoring is not configured for this environment.');
            writeMonitoringState(environmentId, 'Disconnected', 'Monitoring is not configured for this environment.', true);
          }
          return;
        }

        if (axios.isAxiosError(err) && err.response?.status === 404) {
          if (!cancelled) {
            setLocalConnectionStatus('disconnected');
            setLocalError('Environment not found.');
            writeMonitoringState(environmentId, 'Disconnected', 'Environment not found.', false);
          }
          return;
        }
      }

      if (cancelled) return;

      // Build SignalR connection
      const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/monitoring', { withCredentials: true })
        .withAutomaticReconnect()
        .build();

      connectionRef.current = connection;

      connection.onreconnecting(() => {
        if (!cancelled) {
          setLocalConnectionStatus('reconnecting');
          writeMonitoringState(environmentId, 'Reconnecting', null, false);
        }
      });

      connection.onreconnected(() => {
        if (!cancelled) {
          setLocalConnectionStatus('connected');
          writeMonitoringState(environmentId, 'Connected', null, false);
          void connection.invoke('SubscribeToEnvironment', environmentId);
        }
      });

      connection.onclose(() => {
        if (!cancelled) {
          setLocalConnectionStatus('disconnected');
          writeMonitoringState(environmentId, 'Disconnected', 'Connection lost. Attempting to reconnect…', false);
        }
      });

      connection.on('ReceiveMonitoringSnapshot', (snapshot: MonitoringSnapshot) => {
        if (!cancelled) {
          writeMonitoringSnapshot(snapshot, 'Connected');
          setLocalIsNotConfigured(false);
          setLocalError(null);
        }
      });

      try {
        await connection.start();
        if (cancelled) {
          await connection.stop();
          return;
        }
        setLocalConnectionStatus('connected');
        writeMonitoringState(environmentId, 'Connected', null, false);
        await connection.invoke('SubscribeToEnvironment', environmentId);
      } catch (err) {
        if (!cancelled) {
          const message = err instanceof Error ? err.message : 'Failed to connect';
          setLocalConnectionStatus('disconnected');
          setLocalError(message);
          writeMonitoringState(environmentId, 'Disconnected', message, false);
        }
      }
    };

    void start();

    return () => {
      cancelled = true;
      const conn = connectionRef.current;
      if (conn) {
        const envId = environmentId;
        void conn.invoke('UnsubscribeFromEnvironment', envId).catch(() => {}).finally(() => {
          void conn.stop();
        });
        connectionRef.current = null;
      }
    };
  }, [environmentId]);

  const snapshots = useMemo(
    () => snapshotsQuery.data.map(toMonitoringSnapshot),
    [snapshotsQuery.data],
  );
  const state = stateQuery.data;
  const connectionStatus = toMonitoringConnectionStatus(state?.connectionStatus) ?? localConnectionStatus;

  return {
    snapshots,
    latestSnapshot: snapshots[0] ?? null,
    connectionStatus,
    isNotConfigured: state?.isNotConfigured ?? localIsNotConfigured,
    error: state?.errorMessage ?? localError,
  };
}

function toMonitoringConnectionStatus(status: ReadModelConnectionStatus | undefined): MonitoringConnectionStatus | null {
  switch (status) {
    case 'Connected': return 'connected';
    case 'Connecting': return 'connecting';
    case 'Disconnected': return 'disconnected';
    case 'Reconnecting': return 'reconnecting';
    default: return null;
  }
}
