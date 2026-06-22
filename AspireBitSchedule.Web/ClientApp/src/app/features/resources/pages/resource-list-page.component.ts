import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ResourceDataService } from '../../../data-services/resource-data.service';
import { ResourceListItem } from '../models/resource.models';

@Component({
  selector: 'app-resource-list-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './resource-list-page.component.html',
  styleUrls: ['./resource-list-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceListPageComponent {
  private readonly resourceDataService = inject(ResourceDataService);

  protected readonly resources = signal<ResourceListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly resourceCount = computed(() => this.resources().length);
  protected readonly uniqueTypeCount = computed(() => new Set(this.resources().map((resource) => resource.bitResourceTypeId)).size);
  protected readonly namedEmailCount = computed(() => this.resources().filter((resource) => resource.emailAddress.trim().length > 0).length);

  public constructor() {
    this.resourceDataService.listResources().subscribe({
      next: (resources) => {
        this.resources.set(resources);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load resources.');
        this.loading.set(false);
      }
    });
  }
}
