export interface StreamListItem {
  name: string;
  description: string;
  subjects: string[];
  retentionPolicy: string;
  storageType: string;
  messages: number;
  bytes: number;
  consumerCount: number;
  created: string;
}

export interface StreamDetail {
  info: StreamInfo;
  config: StreamConfig;
  consumers: ConsumerInfo[];
}

export interface StreamInfo {
  name: string;
  description: string;
  subjects: string[];
  retentionPolicy: string;
  storageType: string;
  messages: number;
  bytes: number;
  consumerCount: number;
  created: string;
  state: StreamState;
}

export interface StreamState {
  messages: number;
  bytes: number;
  firstTimestamp: string | null;
  lastTimestamp: string | null;
  firstSeq: number;
  lastSeq: number;
}

export interface StreamConfig {
  name: string;
  description: string | null;
  subjects: string[];
  retentionPolicy: string;
  maxMessages: number;
  maxBytes: number;
  maxAge: number;
  storageType: string;
  replicas: number;
  discardPolicy: string;
  maxMsgSize: number;
  denyDelete: boolean;
  denyPurge: boolean;
  allowRollup: boolean;
}

export interface ConsumerInfo {
  streamName: string;
  name: string;
  description: string | null;
  deliverPolicy: string;
  ackPolicy: string;
  filterSubject: string | null;
  numPending: number;
  numAckPending: number;
  numRedelivered: number;
  isHealthy: boolean;
  created: string;
  state: ConsumerState;
}

export interface ConsumerState {
  delivered: number;
  ackFloor: number;
  numPending: number;
  numAckPending: number;
  numRedelivered: number;
}

// Write request types

export interface CreateStreamRequest {
  name: string;
  description?: string;
  subjects: string[];
  retentionPolicy?: string;
  storageType?: string;
  maxMessages?: number;
  maxBytes?: number;
  replicas?: number;
  discardPolicy?: string;
}

export interface UpdateStreamRequest {
  description?: string;
  subjects: string[];
  maxMessages?: number;
  maxBytes?: number;
  replicas?: number;
}

export interface CreateConsumerRequest {
  name: string;
  description?: string;
  deliverPolicy?: string;
  ackPolicy?: string;
  filterSubject?: string;
  maxDeliver?: number;
}

export interface StreamMessageItem {
  sequence: number;
  subject: string;
  data: string | null;
  headers: Record<string, string>;
  timestamp: string;
  size: number;
}
