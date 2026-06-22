import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { ResourceTypeDataService } from '../../../data-services/resource-type-data.service';
import { ResourceTypeFormComponent } from '../components/resource-type-form.component';
import { ResourceTypeRequest } from '../models/resource-type.models';

@Component({
  selector: 'app-resource-type-create-page',
  standalone: true,
  imports: [CommonModule, ResourceTypeFormComponent],
  template: `
    <app-resource-type-form
      title="Create Resource Type"
      submitLabel="Create Resource Type"
      [saving]="saving()"
      [errorMessage]="errorMessage()"
      (save)="createResourceType($event)"
      (cancel)="navigateBack()" />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceTypeCreatePageComponent {
  private readonly resourceTypeDataService = inject(ResourceTypeDataService);
  private readonly router = inject(Router);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected createResourceType(request: ResourceTypeRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.resourceTypeDataService.createResourceType(request).subscribe({
      next: () => void this.router.navigate(['/resource-types']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to create the resource type.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/resource-types']);
  }
}
