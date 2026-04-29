export interface AuditEvent {
  id: string;
  timestamp: string;
  actorId: string | null;
  actorName: string;
  actionType: string;
  resourceType: string;
  resourceId: string;
  resourceName: string;
  environmentId: string | null;
  outcome: string;
  details: string | null;
  source: string;
}

export interface AuditEventsResult {
  items: AuditEvent[];
  totalCount: number;
  page: number;
  pageSize: number;
}
