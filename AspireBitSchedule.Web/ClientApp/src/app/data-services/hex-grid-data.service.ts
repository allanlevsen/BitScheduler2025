import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class HexGridDataService {
  private readonly apiService = inject(ApiService);

  public getCell<TCell>(latitude: number, longitude: number): Observable<TCell> {
    return this.apiService.get<TCell>('/hex-grid/cell', { latitude, longitude });
  }

  public getNeighbors<TNeighbors>(gridId: number): Observable<TNeighbors> {
    return this.apiService.get<TNeighbors>(`/hex-grid/${gridId}/neighbors`);
  }
}
