import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { ObjectBucketInfo, ObjectInfo, CreateObjectBucketRequest } from '../types';

export function useObjectBuckets(environmentId: string | null) {
  return useQuery({
    queryKey: ['object-buckets', environmentId],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/objectstore/buckets`);
      return response.data as ObjectBucketInfo[];
    },
    enabled: !!environmentId,
  });
}

export function useObjectBucket(environmentId: string | null, bucketName: string | undefined) {
  return useQuery({
    queryKey: ['object-buckets', environmentId, bucketName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/objectstore/buckets/${bucketName}`);
      return response.data as ObjectBucketInfo;
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useObjects(environmentId: string | null, bucketName: string | undefined) {
  return useQuery({
    queryKey: ['objects', environmentId, bucketName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/objectstore/buckets/${bucketName}/objects`);
      return response.data as ObjectInfo[];
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useObjectInfo(environmentId: string | null, bucketName: string | undefined, objectName: string | undefined) {
  return useQuery({
    queryKey: ['objects', environmentId, bucketName, objectName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/objectstore/buckets/${bucketName}/objects/${objectName}`);
      return response.data as ObjectInfo;
    },
    enabled: !!environmentId && !!bucketName && !!objectName,
  });
}

export function useCreateObjectBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateObjectBucketRequest) => {
      await apiClient.post(`/environments/${environmentId}/objectstore/buckets`, data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['object-buckets', environmentId] });
    },
  });
}

export function useDeleteObjectBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bucketName: string) => {
      await apiClient.delete(`/environments/${environmentId}/objectstore/buckets/${bucketName}`, {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['object-buckets', environmentId] });
    },
  });
}

export function useUploadObject(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ objectName, file }: { objectName: string; file: File }) => {
      const data = await file.arrayBuffer();
      await apiClient.put(
        `/environments/${environmentId}/objectstore/buckets/${bucketName}/objects/${objectName}`,
        data,
        { headers: { 'Content-Type': file.type || 'application/octet-stream' } }
      );
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['objects', environmentId, bucketName] });
    },
  });
}

export function useDeleteObject(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (objectName: string) => {
      await apiClient.delete(`/environments/${environmentId}/objectstore/buckets/${bucketName}/objects/${objectName}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['objects', environmentId, bucketName] });
    },
  });
}
