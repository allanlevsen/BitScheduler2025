import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnChanges, SimpleChanges, ViewChild, inject, input, signal } from '@angular/core';

import { GoogleMappingConfiguration } from '../../core/config/google-mapping-configuration';
import { GoogleMapsLoaderService } from '../../core/google-maps/google-maps-loader.service';

@Component({
  selector: 'app-google-map',
  standalone: true,
  templateUrl: './google-map.component.html',
  styleUrls: ['./google-map.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GoogleMapComponent implements OnChanges, AfterViewInit {
  private readonly googleMapsLoaderService = inject(GoogleMapsLoaderService);
  private map: google.maps.Map | null = null;
  private marker: google.maps.marker.AdvancedMarkerElement | google.maps.Marker | null = null;
  private viewInitialized = false;
  private initializationInProgress = false;

  public readonly configuration = input<GoogleMappingConfiguration | null>(null);

  @ViewChild('mapHost', { static: true })
  private readonly mapHost?: ElementRef<HTMLDivElement>;

  protected readonly mapError = signal<string | null>(null);
  protected readonly mapStatus = signal('Waiting for Google Maps configuration...');

  public ngOnChanges(changes: SimpleChanges): void {
    if (!changes['configuration'] || !this.configuration()) {
      return;
    }

    void this.tryInitializeMap();
  }

  public ngAfterViewInit(): void {
    this.viewInitialized = true;
    void this.tryInitializeMap();
  }

  private async initializeMap(): Promise<void> {
    const configuration = this.configuration();

    if (!this.mapHost?.nativeElement || !configuration) {
      return;
    }

    this.mapError.set(null);
    this.mapStatus.set('Loading Google Maps...');

    const maps = await this.googleMapsLoaderService.load(configuration);
    const center = {
      lat: configuration.defaultCenter.latitude,
      lng: configuration.defaultCenter.longitude
    };

    const mapOptions: google.maps.MapOptions = {
      center,
      zoom: configuration.defaultZoom,
      disableDefaultUI: false,
      mapId: configuration.mapId || undefined
    };

    this.map ??= new maps.Map(this.mapHost.nativeElement, mapOptions);
    this.map.setOptions(mapOptions);

    const markerLibrary = configuration.libraries.includes('marker')
      ? await this.getMarkerLibrary()
      : null;

    if (markerLibrary?.AdvancedMarkerElement) {
      if (this.marker instanceof markerLibrary.AdvancedMarkerElement) {
        this.marker.position = center;
        this.marker.map = this.map;
      } else {
        this.marker = new markerLibrary.AdvancedMarkerElement({
          map: this.map,
          position: center,
          title: 'Default map center'
        });
      }
    } else if (this.marker instanceof maps.Marker) {
      this.marker.setPosition(center);
      this.marker.setMap(this.map);
    } else {
      this.marker = new maps.Marker({
        map: this.map,
        position: center,
        title: 'Default map center'
      });
    }

    this.mapStatus.set('Google Maps loaded.');
  }

  private async tryInitializeMap(): Promise<void> {
    if (!this.viewInitialized || !this.configuration() || this.initializationInProgress) {
      return;
    }

    this.initializationInProgress = true;

    try {
      await this.initializeMap();
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Failed to initialize Google Maps.';
      this.mapError.set(message);
      this.mapStatus.set(message);
    } finally {
      this.initializationInProgress = false;
    }
  }

  private async getMarkerLibrary(): Promise<google.maps.MarkerLibrary | null> {
    if (typeof google.maps.importLibrary === 'function') {
      return await google.maps.importLibrary('marker') as google.maps.MarkerLibrary;
    }

    return google.maps.marker
      ? google.maps.marker as google.maps.MarkerLibrary
      : null;
  }
}
