import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ClientListItem, ClientRequest, ClientSelectionRequest } from '../features/clients/models/client.models';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ClientDataService {
  private readonly apiService = inject(ApiService);

  public listClients(): Observable<ClientListItem[]> {
    return this.apiService.get<ClientListItem[]>('/clients');
  }

  public getClient(bitClientId: number): Observable<ClientListItem> {
    return this.apiService.get<ClientListItem>(`/clients/${bitClientId}`);
  }

  public createClient(request: ClientRequest): Observable<ClientListItem> {
    return this.apiService.post<ClientListItem, ClientRequest>('/clients', request);
  }

  public getCurrentClient(): Observable<ClientListItem> {
    return this.apiService.get<ClientListItem>('/clients/current');
  }

  public setCurrentClient(bitClientId: number): Observable<ClientListItem> {
    return this.apiService.post<ClientListItem, ClientSelectionRequest>('/clients/current', { bitClientId });
  }

  public updateClient(bitClientId: number, request: ClientRequest): Observable<ClientListItem> {
    return this.apiService.put<ClientListItem, ClientRequest>(`/clients/${bitClientId}`, request);
  }

  public deleteClient(bitClientId: number): Observable<void> {
    return this.apiService.delete(`/clients/${bitClientId}`);
  }
}
