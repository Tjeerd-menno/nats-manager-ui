import { useQuery, useMutation } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { ServiceInfo } from '../types';

export function useServices(environmentId: string | null) {
  return useQuery({
    queryKey: ['services', environmentId],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/services`);
      return response.data as ServiceInfo[];
    },
    enabled: !!environmentId,
  });
}

export function useService(environmentId: string | null, serviceName: string | undefined) {
  return useQuery({
    queryKey: ['services', environmentId, serviceName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/services/${serviceName}`);
      return response.data as ServiceInfo;
    },
    enabled: !!environmentId && !!serviceName,
  });
}

export function useTestService(environmentId: string | null) {
  return useMutation({
    mutationFn: async ({ serviceName, subject, payload }: { serviceName: string; subject: string; payload?: string }) => {
      const response = await apiClient.post(`/environments/${environmentId}/services/${serviceName}/test`, {
        subject,
        payload,
      });
      return response.data as string;
    },
  });
}
