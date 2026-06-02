import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { GoogleMappingConfiguration } from './google-mapping-configuration';

@Injectable({
  providedIn: 'root'
})
export class GoogleMappingConfigService {
  private readonly httpClient = inject(HttpClient);
  private readonly configuration$: Observable<GoogleMappingConfiguration> = this.httpClient
    .get<GoogleMappingConfiguration>('/api/config/google-mapping')
    .pipe(shareReplay(1));

  public getConfiguration(): Observable<GoogleMappingConfiguration> {
    return this.configuration$;
  }
}
