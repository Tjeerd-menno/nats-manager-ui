import axios from 'axios';
import type { ProblemDetails } from './types';
import type { DataFreshness } from './types';

export const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true,
  xsrfCookieName: 'XSRF-TOKEN',
  xsrfHeaderName: 'X-XSRF-TOKEN',
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      // Don't redirect on initial auth check or when already on login page
      const isAuthCheck = error.config?.url === '/auth/me';
      const isOnLoginPage = window.location.pathname === '/login';
      if (!isAuthCheck && !isOnLoginPage) {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export function extractProblemDetails(error: unknown): ProblemDetails | null {
  if (axios.isAxiosError(error) && error.response?.data) {
    return error.response.data as ProblemDetails;
  }
  return null;
}

export function extractDataFreshness(headers: Record<string, string>) {
  const freshness = headers['x-data-freshness'];
  return {
    freshness: freshness === 'recent' || freshness === 'stale' ? freshness : 'live',
    timestamp: headers['x-data-timestamp'] ?? new Date().toISOString(),
  } satisfies DataFreshness;
}
