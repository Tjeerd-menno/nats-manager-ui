import type { HealthStatus } from './common';

export interface StreamRecord {
  id: string;
  environmentId: string;
  name: string;
  description: string | null;
  subjects: string[];
  retentionPolicy: string;
  storage: string;
  replicas: number | null;
  messages: number;
  bytes: number;
  consumerCount: number;
  healthStatus: HealthStatus;
  lastObservedAtUtc: string;
}

export interface ConsumerRecord {
  id: string;
  environmentId: string;
  streamName: string;
  name: string;
  description: string | null;
  durableName: string | null;
  deliverPolicy: string;
  ackPolicy: string;
  pendingMessages: number | null;
  redeliveredMessages: number | null;
  ackFloorConsumerSequence: number | null;
  ackFloorStreamSequence: number | null;
  healthStatus: HealthStatus;
  lastObservedAtUtc: string;
}
