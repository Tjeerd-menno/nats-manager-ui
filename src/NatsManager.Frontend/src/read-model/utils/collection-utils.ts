import { FRESHNESS_THRESHOLDS, type Freshness } from '../types/common';

interface WritableCollection {
  readonly toArray: object[];
  has: unknown;
  insert: unknown;
  update: unknown;
  delete: unknown;
}

export function makeEnvironmentScopedId(environmentId: string, ...parts: Array<string | number | null | undefined>) {
  return [environmentId, ...parts.map(part => String(part ?? ''))].join(':');
}

export function calculateFreshness(
  observedAtUtc: string | null | undefined,
  now = Date.now(),
  thresholds = FRESHNESS_THRESHOLDS,
): Freshness {
  if (!observedAtUtc) return 'Unknown';

  const observedAt = Date.parse(observedAtUtc);
  if (Number.isNaN(observedAt)) return 'Unknown';

  const ageMs = Math.max(0, now - observedAt);
  if (ageMs <= thresholds.liveMs) return 'Live';
  if (ageMs <= thresholds.recentMs) return 'Recent';
  return 'Stale';
}

export function upsertRecord<T extends object>(
  collection: WritableCollection,
  key: string | number,
  record: T,
) {
  const has = collection.has as (key: string | number) => boolean;
  const insert = collection.insert as (record: T) => unknown;
  const update = collection.update as (key: string | number, callback: (draft: T) => void) => unknown;

  if (has.call(collection, key)) {
    update.call(collection, key, (draft: T) => {
      Object.assign(draft, record);
    });
    return;
  }

  insert.call(collection, record);
}

export function clearEnvironmentRecords<T extends { environmentId: string }, TKey extends string | number>(
  collection: WritableCollection,
  environmentId: string,
  getKey: (record: T) => TKey,
) {
  const deleteRecords = collection.delete as (keys: TKey[]) => unknown;
  const keys = (collection.toArray as T[])
    .filter(record => record.environmentId === environmentId)
    .map(getKey);

  if (keys.length > 0) {
    deleteRecords.call(collection, keys);
  }
}

export function trimCollectionByEnvironment<T extends { environmentId: string }>(
  collection: WritableCollection,
  environmentId: string,
  limit: number,
  getTimestamp: (record: T) => string,
  getKey: (record: T) => string | number,
) {
  const deleteRecords = collection.delete as (keys: Array<string | number>) => unknown;
  const records = (collection.toArray as T[])
    .filter(record => record.environmentId === environmentId)
    .sort((left, right) => Date.parse(right ? getTimestamp(right) : '') - Date.parse(left ? getTimestamp(left) : ''));

  const staleKeys = records
    .slice(limit)
    .map(getKey);

  if (staleKeys.length > 0) {
    deleteRecords.call(collection, staleKeys);
  }
}
