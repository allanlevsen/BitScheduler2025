import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';
import { EventListRequest, EventModel, EventRequest } from '../features/events/models/event.models';

@Injectable({
  providedIn: 'root'
})
export class EventDataService {
  private readonly apiService = inject(ApiService);

  public listEvents(request?: EventListRequest): Observable<EventModel[]> {
    return request
      ? this.apiService.post<EventModel[], EventListRequest>('/events/list', request)
      : this.apiService.get<EventModel[]>('/events');
  }

  public getEvent(bitEventId: number): Observable<EventModel> {
    return this.apiService.get<EventModel>(`/events/${bitEventId}`);
  }

  public createEvent(request: EventRequest): Observable<EventModel> {
    return this.apiService.post<EventModel, EventRequest>('/events', request);
  }

  public updateEvent(bitEventId: number, request: EventRequest): Observable<EventModel> {
    return this.apiService.put<EventModel, EventRequest>(`/events/${bitEventId}`, request);
  }

  public deleteEvent(bitEventId: number): Observable<void> {
    return this.apiService.delete(`/events/${bitEventId}`);
  }
}
