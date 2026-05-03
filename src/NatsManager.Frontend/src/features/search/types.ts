export interface SearchResult {
  resourceType: string;
  resourceId: string;
  displayName: string;
  environmentId?: string | null;
  environmentName?: string | null;
  description?: string;
}

export interface BookmarkDto {
  id: string;
  userId: string;
  resourceType: string;
  resourceId: string;
  displayName: string;
  environmentId: string;
  createdAt: string;
}

export interface UserPreferenceDto {
  key: string;
  value: string;
}

export interface CreateBookmarkRequest {
  environmentId: string;
  resourceType: string;
  resourceId: string;
  displayName: string;
}
