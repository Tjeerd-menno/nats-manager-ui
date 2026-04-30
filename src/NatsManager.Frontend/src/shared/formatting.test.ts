import { formatBytes, formatDate, formatDateTime, formatTime } from './formatting';

describe('formatting utilities', () => {
  it('formats byte values using binary units', () => {
    expect(formatBytes(0)).toBe('0 B');
    expect(formatBytes(512)).toBe('512 B');
    expect(formatBytes(1024)).toBe('1 KB');
    expect(formatBytes(1536)).toBe('1.5 KB');
    expect(formatBytes(1024 ** 2)).toBe('1 MB');
    expect(formatBytes(1024 ** 3 * 2.5)).toBe('2.5 GB');
  });

  it('returns fallback values for missing dates', () => {
    expect(formatDateTime(null)).toBe('—');
    expect(formatDate(undefined, 'n/a')).toBe('n/a');
    expect(formatTime(null, 'unknown')).toBe('unknown');
  });

  it('formats date values with the local runtime locale', () => {
    const value = '2026-01-02T03:04:05Z';
    const date = new Date(value);

    expect(formatDateTime(value)).toBe(date.toLocaleString());
    expect(formatDate(value)).toBe(date.toLocaleDateString());
    expect(formatTime(value)).toBe(date.toLocaleTimeString());
  });
});
