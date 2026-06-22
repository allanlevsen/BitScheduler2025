export interface ResourceListItem {
  bitResourceId: number;
  bitResourceTypeId: number;
  resourceTypeName: string;
  firstName: string;
  lastName: string;
  emailAddress: string;
  displayName: string;
}

export interface ResourceRequest {
  bitResourceTypeId: number;
  firstName: string;
  lastName: string;
  emailAddress: string;
}

export interface ResourceTypeListItem {
  bitResourceTypeId: number;
  name: string;
}
