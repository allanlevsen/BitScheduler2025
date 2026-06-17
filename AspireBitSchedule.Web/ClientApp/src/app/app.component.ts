import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';

import { GoogleMappingConfiguration } from './core/config/google-mapping-configuration';
import { GoogleMappingConfigService } from './core/config/google-mapping-config.service';
import { GoogleMapComponent } from './features/google-map/google-map.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [GoogleMapComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent implements OnInit {
  private readonly googleMappingConfigService = inject(GoogleMappingConfigService);

  protected readonly googleMapping = signal<GoogleMappingConfiguration | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  public ngOnInit(): void {
    this.googleMappingConfigService.getConfiguration().subscribe({
      next: (configuration) => {
        this.googleMapping.set(configuration);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load Google Maps configuration.');
        this.loading.set(false);
      }
    });
  }
}
