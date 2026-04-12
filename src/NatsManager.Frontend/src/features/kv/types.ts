export interface KvBucketInfo {
  bucketName: string;
  history: number;
  maxBytes: number;
  maxValueSize: number;
  ttl: number | null;
  keyCount: number;
  byteCount: number;
}

export interface KvEntry {
  key: string;
  value: string | null;
  revision: number;
  operation: string;
  createdAt: string;
  size: number;
}

export interface KvKeyHistoryEntry {
  revision: number;
  operation: string;
  createdAt: string;
  size: number;
}

export interface CreateKvBucketRequest {
  bucketName: string;
  history?: number;
  maxBytes?: number;
  maxValueSize?: number;
  ttl?: number;
}

export interface PutKvKeyRequest {
  value: string;
  expectedRevision?: number;
}
