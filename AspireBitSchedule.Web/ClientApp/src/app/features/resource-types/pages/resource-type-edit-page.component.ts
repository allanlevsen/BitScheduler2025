import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { ResourceTypeDataService } from '../../../data-services/resource-type-data.service';
import { ResourceTypeFormComponent } from '../components/resource-type-form.component';
import { ResourceTypeListItem, ResourceTypeRequest } from '../models/resource-type.models';

@Component({
  selector: 'app-resource-type-edit-page',
  standalone: true,
  imports: [CommonModule, ResourceTypeFormComponent],
  template: `
    @if (loading()) {
      <div class="card panel-card">
        <div class="card-body p-4">Loading resource type details...</div>
      </div>
    } @else {
      <app-resource-type-form
        title="Update Resource Type"
        submitLabel="Save Changes"
        [initialValue]="resourceType()"
        [saving]="saving()"
        [errorMessage]="errorMessage()"
        (save)="updateResourceType($event)"
        (cancel)="navigateBack()" />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceTypeEditPageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly resourceTypeDataService = inject(ResourceTypeDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly resourceType = signal<ResourceTypeListItem | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitResourceTypeId = Number(this.route.snapshot.paramMap.get('bitResourceTypeId'));

  public constructor() {
    this.loadResourceType();
    this.clientContext.clientChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadResourceType());
  }

  protected updateResourceType(request: ResourceTypeRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.resourceTypeDataService.updateResourceType(this.bitResourceTypeId, request).subscribe({
      next: () => void this.router.navigate(['/resource-types']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to update the resource type.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/resource-types']);
  }

  private loadResourceType(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

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
}
