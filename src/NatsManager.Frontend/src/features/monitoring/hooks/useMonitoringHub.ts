import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { apiClient } from '../../../api/client';
import type { MonitoringSnapshot, MonitoringHistoryResult, MonitoringConnectionStatus } from '../types';

const MAX_SNAPSHOTS = 120;

interface UseMonitoringHubResult {
  snapshots: MonitoringSnapshot[];
  latestSnapshot: MonitoringSnapshot | null;
  connectionStatus: MonitoringConnectionStatus;
  error: string | null;
}

export function useMonitoringHub(environmentId: string | null): UseMonitoringHubResult {
  const [snapshots, setSnapshots] = useState<MonitoringSnapshot[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<MonitoringConnectionStatus>('connecting');
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!environmentId) return;

    let cancelled = false;

    const start = async () => {
      // Fetch initial history
      try {
        const response = await apiClient.get<MonitoringHistoryResult>(
          `/environments/${environmentId}/monitoring/metrics/history`
        );
        if (!cancelled) {
          // History comes oldest→newest; we display newest-first in state (prepended)
          setSnapshots([...response.data.snapshots].reverse());
        }
      } catch {
        // 400 = not configured, 404 = not found; both handled by empty state
      }

      if (cancelled) return;

      // Build SignalR connection
      const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/monitoring', { withCredentials: true })
        .withAutomaticReconnect()
        .build();

      connectionRef.current = connection;

      connection.onreconnecting(() => {
        if (!cancelled) setConnectionStatus('reconnecting');
      });

      connection.onreconnected(() => {
        if (!cancelled) {
          setConnectionStatus('connected');
          void connection.invoke('SubscribeToEnvironment', environmentId);
        }
      });

      connection.onclose(() => {
        if (!cancelled) setConnectionStatus('disconnected');
      });

      connection.on('ReceiveMonitoringSnapshot', (snapshot: MonitoringSnapshot) => {
        if (!cancelled) {
          setSnapshots(prev => [snapshot, ...prev].slice(0, MAX_SNAPSHOTS));
          setError(null);
        }
      });

      try {
        await connection.start();
        if (cancelled) {
          await connection.stop();
          return;
        }
        setConnectionStatus('connected');
        await connection.invoke('SubscribeToEnvironment', environmentId);
      } catch (err) {
        if (!cancelled) {
          setConnectionStatus('disconnected');
          setError(err instanceof Error ? err.message : 'Failed to connect');
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

  return {
    snapshots,
    latestSnapshot: snapshots[0] ?? null,
    connectionStatus,
    error,
  };
}
