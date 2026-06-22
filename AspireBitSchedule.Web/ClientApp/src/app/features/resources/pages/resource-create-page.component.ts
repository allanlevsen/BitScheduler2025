import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { ResourceDataService } from '../../../data-services/resource-data.service';
import { ResourceFormComponent } from '../components/resource-form.component';
import { ResourceRequest, ResourceTypeListItem } from '../models/resource.models';

@Component({
  selector: 'app-resource-create-page',
  standalone: true,
  imports: [CommonModule, ResourceFormComponent],
  template: `
    <app-resource-form
      title="Create Resource"
      submitLabel="Create Resource"
      [resourceTypes]="resourceTypes()"
      [saving]="saving()"
      [errorMessage]="errorMessage()"
      (save)="createResource($event)"
      (cancel)="navigateBack()" />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResourceCreatePageComponent {
  private readonly resourceDataService = inject(ResourceDataService);
  private readonly router = inject(Router);

  protected readonly resourceTypes = signal<ResourceTypeListItem[]>([]);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  public constructor() {
    this.resourceDataService.listResourceTypes().subscribe({
      next: (resourceTypes) => this.resourceTypes.set(resourceTypes),
      error: () => this.errorMessage.set('Unable to load resource types.')
    });
  }

  protected createResource(request: ResourceRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.resourceDataService.createResource(request).subscribe({
      next: () => void this.router.navigate(['/resources']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to create the resource.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/resources']);
  }
}
