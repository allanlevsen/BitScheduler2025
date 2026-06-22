import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class EventDataService {
  private readonly apiService = inject(ApiService);

  public getEvent<TEvent>(bitEventId: number): Observable<TEvent> {
    return this.apiService.get<TEvent>(`/events/${bitEventId}`);
  }

  public createEvent<TEvent, TRequest>(request: TRequest): Observable<TEvent> {
    return this.apiService.post<TEvent, TRequest>('/events', request);
  }
}
