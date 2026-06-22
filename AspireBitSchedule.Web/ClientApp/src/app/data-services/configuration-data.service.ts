import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { GoogleMappingConfiguration } from '../core/config/google-mapping-configuration';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ConfigurationDataService {
  private readonly apiService = inject(ApiService);
  private readonly googleMappingConfiguration$: Observable<GoogleMappingConfiguration> = this.apiService
    .get<GoogleMappingConfiguration>('/config/google-mapping')
    .pipe(shareReplay(1));

  public getGoogleMappingConfiguration(): Observable<GoogleMappingConfiguration> {
    return this.googleMappingConfiguration$;
  }
}
