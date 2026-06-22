import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ResourceDataService } from '../../../data-services/resource-data.service';
import { ResourceTypeDataService } from '../../../data-services/resource-type-data.service';
import { ResourceListItem } from '../../resources/models/resource.models';
import { ResourceTypeListItem } from '../models/resource-type.models';

@Component({
  selector: 'app-resource-type-list-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './resource-type-list-page.component.html',
  styleUrls: ['./resource-type-list-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceTypeListPageComponent {
  private readonly resourceTypeDataService = inject(ResourceTypeDataService);
  private readonly resourceDataService = inject(ResourceDataService);

  protected readonly resourceTypes = signal<ResourceTypeListItem[]>([]);
  protected readonly resources = signal<ResourceListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly typeCount = computed(() => this.resourceTypes().length);
  protected readonly resourceCount = computed(() => this.resources().length);
  protected readonly unusedTypeCount = computed(() => {
    const usedTypeIds = new Set(this.resources().map((resource) => resource.bitResourceTypeId));
    return this.resourceTypes().filter((resourceType) => !usedTypeIds.has(resourceType.bitResourceTypeId)).length;
  });

  public constructor() {
    let pendingLoads = 2;
    const finishLoad = (): void => {
      pendingLoads -= 1;
      if (pendingLoads === 0) {
        this.loading.set(false);
      }
    };

    this.resourceTypeDataService.listResourceTypes().subscribe({
      next: (resourceTypes) => {
        this.resourceTypes.set(resourceTypes);
        finishLoad();
      },
      error: () => {
        this.errorMessage.set('Unable to load resource types.');
        finishLoad();
      }
    });

    this.resourceDataService.listResources().subscribe({
      next: (resources) => {
        this.resources.set(resources);
        finishLoad();
      },
      error: () => {
        this.errorMessage.set('Unable to load related resource data.');
        finishLoad();
      }
    });
  }
}
