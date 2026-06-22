import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ResourceListItem } from '../features/resources/models/resource.models';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ResourceDataService {
  private readonly apiService = inject(ApiService);

  public listResources(): Observable<ResourceListItem[]> {
    return this.apiService.get<ResourceListItem[]>('/resources');
  }
}
