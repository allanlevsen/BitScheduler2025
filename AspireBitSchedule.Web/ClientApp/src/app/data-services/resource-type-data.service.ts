import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ResourceTypeListItem, ResourceTypeRequest } from '../features/resource-types/models/resource-type.models';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ResourceTypeDataService {
  private readonly apiService = inject(ApiService);

  public listResourceTypes(): Observable<ResourceTypeListItem[]> {
    return this.apiService.get<ResourceTypeListItem[]>('/resource-types');
  }

  public getResourceType(bitResourceTypeId: number): Observable<ResourceTypeListItem> {
    return this.apiService.get<ResourceTypeListItem>(`/resource-types/${bitResourceTypeId}`);
  }

  public createResourceType(request: ResourceTypeRequest): Observable<ResourceTypeListItem> {
    return this.apiService.post<ResourceTypeListItem, ResourceTypeRequest>('/resource-types', request);
  }

  public updateResourceType(bitResourceTypeId: number, request: ResourceTypeRequest): Observable<ResourceTypeListItem> {
    return this.apiService.put<ResourceTypeListItem, ResourceTypeRequest>(`/resource-types/${bitResourceTypeId}`, request);
  }

  public deleteResourceType(bitResourceTypeId: number): Observable<void> {
    return this.apiService.delete(`/resource-types/${bitResourceTypeId}`);
  }
}
