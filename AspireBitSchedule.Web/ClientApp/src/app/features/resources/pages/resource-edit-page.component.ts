import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { ResourceDataService } from '../../../data-services/resource-data.service';
import { ResourceFormComponent } from '../components/resource-form.component';
import { ResourceListItem, ResourceRequest, ResourceTypeListItem } from '../models/resource.models';

@Component({
  selector: 'app-resource-edit-page',
  standalone: true,
  imports: [CommonModule, ResourceFormComponent],
  template: `
    @if (loading()) {
      <div class="card panel-card">
        <div class="card-body p-4">Loading resource details...</div>
      </div>
    } @else {
      <app-resource-form
        title="Update Resource"
        submitLabel="Save Changes"
        [resourceTypes]="resourceTypes()"
        [initialValue]="resource()"
        [saving]="saving()"
        [errorMessage]="errorMessage()"
        (save)="updateResource($event)"
        (cancel)="navigateBack()" />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceEditPageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly resourceDataService = inject(ResourceDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly resourceTypes = signal<ResourceTypeListItem[]>([]);
  protected readonly resource = signal<ResourceListItem | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitResourceId = Number(this.route.snapshot.paramMap.get('bitResourceId'));

  public constructor() {
    this.loadData();
    this.clientContext.clientChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadData());
  }

  protected updateResource(request: ResourceRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.resourceDataService.updateResource(this.bitResourceId, request).subscribe({
      next: () => void this.router.navigate(['/resources']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to update the resource.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/resources']);
  }

  private loadData(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    let pendingLoads = 2;
    const finishLoad = (): void => {
      pendingLoads -= 1;
      if (pendingLoads === 0) {
        this.loading.set(false);
      }
    };

    this.resourceDataService.listResourceTypes().subscribe({
      next: (resourceTypes) => {
        this.resourceTypes.set(resourceTypes);
        finishLoad();
      },
      error: () => {
        this.errorMessage.set('Unable to load resource types.');
        finishLoad();
      }
    });

    this.resourceDataService.getResource(this.bitResourceId).subscribe({
      next: (resource) => {
        this.resource.set(resource);
        finishLoad();
      },
      error: () => {
        this.errorMessage.set('Unable to load the resource.');
        finishLoad();
      }
    });
  }
}
