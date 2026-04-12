export interface SearchResult {
  resourceType: string;
  resourceId: string;
  name: string;
  environmentId: string;
  environmentName: string;
  description?: string;
  navigationUrl: string;
}

export interface BookmarkDto {
  id: string;
  userId: string;
  resourceType: string;
  resourceId: string;
  displayName: string;
  navigationUrl: string;
  createdAt: string;
}

export interface UserPreferenceDto {
  key: string;
  value: string;
}

export interface CreateBookmarkRequest {
  resourceType: string;
  resourceId: string;
  displayName: string;
  navigationUrl: string;
}
