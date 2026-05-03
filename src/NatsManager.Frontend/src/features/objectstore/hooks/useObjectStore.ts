import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import { apiEndpoints } from '../../../api/endpoints';
import { queryKeys } from '../../../api/queryKeys';
import type { ListResponse } from '../../../api/types';
import type { ObjectBucketInfo, ObjectInfo, CreateObjectBucketRequest } from '../types';

export function useObjectBuckets(environmentId: string | null) {
  return useQuery({
    queryKey: queryKeys.objectBuckets(environmentId),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.objectBuckets(environmentId));
      return response.data as ListResponse<ObjectBucketInfo>;
    },
    enabled: !!environmentId,
  });
}

export function useObjectBucket(environmentId: string | null, bucketName: string | undefined) {
  return useQuery({
    queryKey: queryKeys.objectBucket(environmentId, bucketName),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.objectBucket(environmentId, bucketName));
      return response.data as ObjectBucketInfo;
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useObjects(environmentId: string | null, bucketName: string | undefined) {
  return useQuery({
    queryKey: queryKeys.objects(environmentId, bucketName),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.objects(environmentId, bucketName));
      return response.data as ListResponse<ObjectInfo>;
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useObjectInfo(environmentId: string | null, bucketName: string | undefined, objectName: string | undefined) {
  return useQuery({
    queryKey: queryKeys.objectInfo(environmentId, bucketName, objectName),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.objectInfo(environmentId, bucketName, objectName));
      return response.data as ObjectInfo;
    },
    enabled: !!environmentId && !!bucketName && !!objectName,
  });
}

export function useCreateObjectBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateObjectBucketRequest) => {
      await apiClient.post(apiEndpoints.objectBuckets(environmentId), data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.objectBuckets(environmentId) });
    },
  });
}

export function useDeleteObjectBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bucketName: string) => {
      await apiClient.delete(apiEndpoints.objectBucket(environmentId, bucketName), {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.objectBuckets(environmentId) });
    },
  });
}

export function useUploadObject(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ objectName, file }: { objectName: string; file: File }) => {
      const data = await file.arrayBuffer();
      await apiClient.post(
        apiEndpoints.objectUpload(environmentId, bucketName, objectName),
        data,
        { headers: { 'Content-Type': file.type || 'application/octet-stream' } }
      );
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.objects(environmentId, bucketName) });
    },
  });
}

export function useDeleteObject(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (objectName: string) => {
      await apiClient.delete(apiEndpoints.objectInfo(environmentId, bucketName, objectName), {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.objects(environmentId, bucketName) });
    },
  });
}
