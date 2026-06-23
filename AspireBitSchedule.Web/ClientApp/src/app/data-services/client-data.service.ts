import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ClientListItem, ClientSelectionRequest } from '../features/clients/models/client.models';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ClientDataService {
  private readonly apiService = inject(ApiService);

  public listClients(): Observable<ClientListItem[]> {
    return this.apiService.get<ClientListItem[]>('/clients');
  }

  public getCurrentClient(): Observable<ClientListItem> {
    return this.apiService.get<ClientListItem>('/clients/current');
  }

  public setCurrentClient(bitClientId: number): Observable<ClientListItem> {
    return this.apiService.post<ClientListItem, ClientSelectionRequest>('/clients/current', { bitClientId });
  }
}
