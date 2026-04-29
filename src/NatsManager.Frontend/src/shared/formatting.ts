export function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i] ?? 'B'}`;
}

export function formatDateTime(value: string | null | undefined, fallback = '—'): string {
  return value ? new Date(value).toLocaleString() : fallback;
}

export function formatDate(value: string | null | undefined, fallback = '—'): string {
  return value ? new Date(value).toLocaleDateString() : fallback;
}

export function formatTime(value: string | null | undefined, fallback = '—'): string {
  return value ? new Date(value).toLocaleTimeString() : fallback;
}
