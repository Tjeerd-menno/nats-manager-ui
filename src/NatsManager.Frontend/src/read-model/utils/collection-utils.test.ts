import { calculateFreshness, makeEnvironmentScopedId } from './collection-utils';

describe('read-model collection utilities', () => {
  it('creates deterministic environment-scoped ids', () => {
    expect(makeEnvironmentScopedId('env-1', 'Stream', 'ORDERS')).toBe('env-1:Stream:ORDERS');
  });

  it('calculates live, recent, stale, and unknown freshness', () => {
    const now = Date.parse('2026-01-01T00:01:00.000Z');

    expect(calculateFreshness('2026-01-01T00:00:55.000Z', now)).toBe('Live');
    expect(calculateFreshness('2026-01-01T00:00:20.000Z', now)).toBe('Recent');
    expect(calculateFreshness('2025-12-31T23:59:59.000Z', now)).toBe('Stale');
    expect(calculateFreshness(null, now)).toBe('Unknown');
  });
});
