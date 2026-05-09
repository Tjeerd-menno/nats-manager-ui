import { consumersCollection, resourceIndexCollection, streamsCollection } from '../collections';
import type { ConsumerInfo, StreamDetail, StreamInfo, StreamListItem } from '../../features/jetstream/types';
import type { ConsumerRecord, StreamRecord } from '../types/jetstream-read-model';
import { makeEnvironmentScopedId, upsertRecord } from '../utils/collection-utils';

export function syncStreams(environmentId: string, streams: StreamListItem[], observedAtUtc = new Date().toISOString()) {
  for (const stream of streams) {
    syncStream(environmentId, stream, observedAtUtc);
  }
}

export function syncStreamDetail(environmentId: string, detail: StreamDetail, observedAtUtc = new Date().toISOString()) {
  syncStream(environmentId, detail.info, observedAtUtc, detail.config.replicas);
  syncConsumers(environmentId, detail.info.name, detail.consumers, observedAtUtc);
}

export function syncStream(environmentId: string, stream: StreamListItem | StreamInfo, observedAtUtc = new Date().toISOString(), replicas: number | null = null) {
  const record: StreamRecord = {
    id: makeEnvironmentScopedId(environmentId, 'Stream', stream.name),
    environmentId,
    name: stream.name,
    description: stream.description || null,
    subjects: stream.subjects,
    retentionPolicy: stream.retentionPolicy,
    storage: stream.storageType,
    replicas,
    messages: stream.messages,
    bytes: stream.bytes,
    consumerCount: stream.consumerCount,
    healthStatus: 'Ok',
    lastObservedAtUtc: observedAtUtc,
  };

  upsertRecord(streamsCollection, record.id, record);
  upsertRecord(resourceIndexCollection, record.id, {
    id: record.id,
    environmentId,
    resourceType: 'Stream',
    resourceId: record.name,
    name: record.name,
    displayName: record.name,
    description: record.description,
    status: record.healthStatus,
    freshness: 'Live',
    detailRoute: `/environments/${environmentId}/jetstream/streams/${encodeURIComponent(record.name)}`,
    searchableText: [record.name, record.description, ...record.subjects, 'stream'].filter(Boolean).join(' ').toLowerCase(),
    lastObservedAtUtc: observedAtUtc,
    indexedAtUtc: observedAtUtc,
  });
}

export function syncConsumers(environmentId: string, streamName: string, consumers: ConsumerInfo[], observedAtUtc = new Date().toISOString()) {
  for (const consumer of consumers) {
    syncConsumer(environmentId, streamName, consumer, observedAtUtc);
  }
}

export function syncConsumer(environmentId: string, streamName: string, consumer: ConsumerInfo, observedAtUtc = new Date().toISOString()) {
  const record: ConsumerRecord = {
    id: makeEnvironmentScopedId(environmentId, 'Consumer', streamName, consumer.name),
    environmentId,
    streamName,
    name: consumer.name,
    description: consumer.description,
    durableName: consumer.name,
    deliverPolicy: consumer.deliverPolicy,
    ackPolicy: consumer.ackPolicy,
    pendingMessages: consumer.numPending,
    redeliveredMessages: consumer.numRedelivered,
    ackFloorConsumerSequence: consumer.state.ackFloor,
    ackFloorStreamSequence: null,
    healthStatus: consumer.isHealthy ? 'Ok' : 'Degraded',
    lastObservedAtUtc: observedAtUtc,
  };

  upsertRecord(consumersCollection, record.id, record);
  upsertRecord(resourceIndexCollection, record.id, {
    id: record.id,
    environmentId,
    resourceType: 'Consumer',
    resourceId: `${streamName}/${consumer.name}`,
    name: consumer.name,
    displayName: consumer.name,
    description: consumer.description,
    status: record.healthStatus,
    freshness: 'Live',
    detailRoute: `/environments/${environmentId}/jetstream/streams/${encodeURIComponent(streamName)}/consumers/${encodeURIComponent(consumer.name)}`,
    searchableText: [consumer.name, consumer.description, streamName, 'consumer'].filter(Boolean).join(' ').toLowerCase(),
    lastObservedAtUtc: observedAtUtc,
    indexedAtUtc: observedAtUtc,
  });
}
