import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';

export interface ResolvedLocationModel {
  address: string;
  latitude: number;
  longitude: number;
  hexGridId?: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class LocationDataService {
  private readonly apiService = inject(ApiService);

  public geocodeAddress(address: string): Observable<ResolvedLocationModel> {
    return this.apiService.get<ResolvedLocationModel>('/locations/geocode', { address });
  }

  public resolveHexGrid(address: string): Observable<ResolvedLocationModel> {
    return this.apiService.get<ResolvedLocationModel>('/locations/hex-grid', { address });
  }
}
