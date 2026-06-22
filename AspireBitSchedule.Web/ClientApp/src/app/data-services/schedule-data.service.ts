import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ScheduleDataService {
  private readonly apiService = inject(ApiService);

  public readSchedule<TResponse, TRequest>(request: TRequest): Observable<TResponse> {
    return this.apiService.post<TResponse, TRequest>('/schedule/read', request);
  }

  public readScheduleDay<TDay, TRequest>(request: TRequest): Observable<TDay> {
    return this.apiService.post<TDay, TRequest>('/schedule/day/read', request);
  }

  public writeSchedule<TResponse, TRequest>(request: TRequest): Observable<TResponse> {
    return this.apiService.post<TResponse, TRequest>('/schedule/write', request);
  }

  public writeScheduleDay<TDay, TRequest>(request: TRequest): Observable<TDay> {
    return this.apiService.post<TDay, TRequest>('/schedule/write/day', request);
  }
}
