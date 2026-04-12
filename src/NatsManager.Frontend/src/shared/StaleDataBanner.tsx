import { Alert } from '@mantine/core';

interface StaleDataBannerProps {
  lastUpdated?: string | null;
  connectionStatus?: string;
}

export function StaleDataBanner({ lastUpdated, connectionStatus }: StaleDataBannerProps) {
  if (connectionStatus === 'Available') return null;

  const isUnreachable = connectionStatus === 'Unavailable';
  const isDegraded = connectionStatus === 'Degraded';

  if (!isUnreachable && !isDegraded) return null;

  const timestamp = lastUpdated ? new Date(lastUpdated).toLocaleString() : 'unknown';

  return (
    <Alert
      color={isUnreachable ? 'red' : 'yellow'}
      title={isUnreachable ? 'Environment Unreachable' : 'Degraded Connection'}
      mb="md"
    >
      {isUnreachable
        ? `This environment is unreachable. Showing last known data from ${timestamp}. Data may be stale.`
        : `Connection to this environment is degraded. Some data may be outdated. Last successful contact: ${timestamp}.`}
    </Alert>
  );
}
