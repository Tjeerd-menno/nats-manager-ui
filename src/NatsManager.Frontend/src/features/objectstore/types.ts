export interface ObjectBucketInfo {
  bucketName: string;
  objectCount: number;
  totalSize: number;
  description: string | null;
}

export interface ObjectInfo {
  name: string;
  size: number;
  description: string | null;
  contentType: string | null;
  lastModified: string | null;
  chunks: number;
  digest: string | null;
}

export interface CreateObjectBucketRequest {
  bucketName: string;
  description?: string;
  maxBucketSize?: number;
}
