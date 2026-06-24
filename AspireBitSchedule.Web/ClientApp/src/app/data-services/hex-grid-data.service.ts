import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';

export interface HexGridCellModel {
  id: number;
  q: number;
  r: number;
  centerLatitude: number;
  centerLongitude: number;
  hexRadiusMeters: number;
}

@Injectable({
  providedIn: 'root'
})
export class HexGridDataService {
  private readonly apiService = inject(ApiService);

  public getCell(latitude: number, longitude: number): Observable<HexGridCellModel> {
    return this.apiService.get<HexGridCellModel>('/hex-grid/cell', { latitude, longitude });
  }

  public getNeighbors<TNeighbors>(gridId: number): Observable<TNeighbors> {
    return this.apiService.get<TNeighbors>(`/hex-grid/${gridId}/neighbors`);
  }
}
