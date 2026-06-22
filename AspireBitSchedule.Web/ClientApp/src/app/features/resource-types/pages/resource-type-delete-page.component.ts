import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ResourceTypeDataService } from '../../../data-services/resource-type-data.service';
import { ResourceTypeListItem } from '../models/resource-type.models';

@Component({
  selector: 'app-resource-type-delete-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './resource-type-delete-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceTypeDeletePageComponent {
  private readonly resourceTypeDataService = inject(ResourceTypeDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly resourceType = signal<ResourceTypeListItem | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitResourceTypeId = Number(this.route.snapshot.paramMap.get('bitResourceTypeId'));

  public constructor() {
    this.resourceTypeDataService.getResourceType(this.bitResourceTypeId).subscribe({
      next: (resourceType) => {
        this.resourceType.set(resourceType);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load the resource type.');
        this.loading.set(false);
      }
    });
  }

  protected deleteResourceType(): void {
    this.deleting.set(true);
    this.errorMessage.set(null);

    this.resourceTypeDataService.deleteResourceType(this.bitResourceTypeId).subscribe({
      next: () => void this.router.navigate(['/resource-types']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to delete the resource type.');
        this.deleting.set(false);
      }
    });
  }
}
