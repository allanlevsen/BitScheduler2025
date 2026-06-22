import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ResourceListItem, ResourceRequest, ResourceTypeListItem } from '../features/resources/models/resource.models';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ResourceDataService {
  private readonly apiService = inject(ApiService);

  public listResources(): Observable<ResourceListItem[]> {
    return this.apiService.get<ResourceListItem[]>('/resources');
  }

  public listResourceTypes(): Observable<ResourceTypeListItem[]> {
    return this.apiService.get<ResourceTypeListItem[]>('/resources/types');
  }

  public getResource(bitResourceId: number): Observable<ResourceListItem> {
    return this.apiService.get<ResourceListItem>(`/resources/${bitResourceId}`);
  }

  public createResource(request: ResourceRequest): Observable<ResourceListItem> {
    return this.apiService.post<ResourceListItem, ResourceRequest>('/resources', request);
  }

  public updateResource(bitResourceId: number, request: ResourceRequest): Observable<ResourceListItem> {
    return this.apiService.put<ResourceListItem, ResourceRequest>(`/resources/${bitResourceId}`, request);
  }

  public deleteResource(bitResourceId: number): Observable<void> {
    return this.apiService.delete(`/resources/${bitResourceId}`);
  }
}
