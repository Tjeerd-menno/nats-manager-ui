import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import { apiClient } from '../../../api/client';
import type { MonitoringSnapshot, MonitoringHistoryResult, MonitoringConnectionStatus } from '../types';

const MAX_SNAPSHOTS = 120;

interface UseMonitoringHubResult {
  snapshots: MonitoringSnapshot[];
  latestSnapshot: MonitoringSnapshot | null;
  connectionStatus: MonitoringConnectionStatus;
  isNotConfigured: boolean;
  error: string | null;
}

export function useMonitoringHub(environmentId: string | null): UseMonitoringHubResult {
  const [snapshots, setSnapshots] = useState<MonitoringSnapshot[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<MonitoringConnectionStatus>('connecting');
  const [isNotConfigured, setIsNotConfigured] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!environmentId) return;

    let cancelled = false;
    setConnectionStatus('connecting');
    setIsNotConfigured(false);
    setError(null);

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
      } catch (err) {
        if (axios.isAxiosError(err) && err.response?.status === 400) {
          if (!cancelled) {
            setSnapshots([]);
            setIsNotConfigured(true);
            setConnectionStatus('disconnected');
            setError('Monitoring is not configured for this environment.');
          }
          return;
        }

        if (axios.isAxiosError(err) && err.response?.status === 404) {
          if (!cancelled) {
            setSnapshots([]);
            setConnectionStatus('disconnected');
            setError('Environment not found.');
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
          setIsNotConfigured(false);
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
    isNotConfigured,
    error,
  };
}
