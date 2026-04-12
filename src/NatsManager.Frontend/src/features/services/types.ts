export interface ServiceInfo {
  name: string;
  id: string;
  version: string;
  description: string;
  endpoints: ServiceEndpoint[];
  stats: ServiceStats;
}

export interface ServiceEndpoint {
  name: string;
  subject: string;
  queueGroup: string;
}

export interface ServiceStats {
  totalRequests: number;
  totalErrors: number;
  averageProcessingTime: string;
  started: string;
}
