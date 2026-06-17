import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

import { GoogleMappingConfiguration } from '../config/google-mapping-configuration';

@Injectable({
  providedIn: 'root'
})
export class GoogleMapsLoaderService {
  private readonly document = inject(DOCUMENT);
  private loadPromise: Promise<typeof google.maps> | null = null;

  public load(configuration: GoogleMappingConfiguration): Promise<typeof google.maps> {
    if (typeof window === 'undefined') {
      return Promise.reject(new Error('Google Maps can only load in a browser environment.'));
    }

    if ((window as Window & { google?: typeof google }).google?.maps) {
      return Promise.resolve(google.maps);
    }

    if (!configuration.apiKey || configuration.apiKey === 'YOUR_GOOGLE_MAPS_API_KEY') {
      return Promise.reject(new Error('Google Maps API key has not been configured yet.'));
    }

    this.loadPromise ??= new Promise<typeof google.maps>((resolve, reject) => {
      const existingScript = this.document.getElementById('google-maps-script') as HTMLScriptElement | null;
      if (existingScript) {
        existingScript.addEventListener('load', () => resolve(google.maps), { once: true });
        existingScript.addEventListener('error', () => reject(new Error('Failed to load the Google Maps script.')), { once: true });
        return;
      }

      const script = this.document.createElement('script');
      script.id = 'google-maps-script';
      script.async = true;
      script.defer = true;
      script.src = this.buildScriptUrl(configuration);
      script.onload = () => resolve(google.maps);
      script.onerror = () => reject(new Error('Failed to load the Google Maps script.'));

      this.document.head.appendChild(script);
    });

    return this.loadPromise;
  }

  private buildScriptUrl(configuration: GoogleMappingConfiguration): string {
    const parameters = new URLSearchParams({
      key: configuration.apiKey,
      loading: 'async'
    });

    if (configuration.libraries.length > 0) {
      parameters.set('libraries', configuration.libraries.join(','));
    }

    if (configuration.language) {
      parameters.set('language', configuration.language);
    }

    if (configuration.region) {
      parameters.set('region', configuration.region);
    }

    return `https://maps.googleapis.com/maps/api/js?${parameters.toString()}`;
  }
}
