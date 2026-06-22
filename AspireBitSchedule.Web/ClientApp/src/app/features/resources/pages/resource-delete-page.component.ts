import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ResourceDataService } from '../../../data-services/resource-data.service';
import { ResourceListItem } from '../models/resource.models';

@Component({
  selector: 'app-resource-delete-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './resource-delete-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceDeletePageComponent {
  private readonly resourceDataService = inject(ResourceDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly resource = signal<ResourceListItem | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitResourceId = Number(this.route.snapshot.paramMap.get('bitResourceId'));

  public constructor() {
    this.resourceDataService.getResource(this.bitResourceId).subscribe({
      next: (resource) => {
        this.resource.set(resource);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load the resource.');
        this.loading.set(false);
      }
    });
  }

  protected deleteResource(): void {
    this.deleting.set(true);
    this.errorMessage.set(null);

    this.resourceDataService.deleteResource(this.bitResourceId).subscribe({
      next: () => void this.router.navigate(['/resources']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to delete the resource.');
        this.deleting.set(false);
      }
    });
  }
}
