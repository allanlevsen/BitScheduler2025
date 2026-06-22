import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { EventDataService } from '../../../data-services/event-data.service';
import { ResourceDataService } from '../../../data-services/resource-data.service';
import { EventFormComponent } from '../components/event-form.component';
import { EventRequest } from '../models/event.models';
import { ResourceListItem } from '../../resources/models/resource.models';

@Component({
  selector: 'app-event-create-page',
  standalone: true,
  imports: [CommonModule, EventFormComponent],
  template: `
    <app-event-form
      title="Create Event"
      submitLabel="Create Event"
      [resources]="resources()"
      [saving]="saving()"
      [errorMessage]="errorMessage()"
      (save)="createEvent($event)"
      (cancel)="navigateBack()" />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventCreatePageComponent {
  private readonly eventDataService = inject(EventDataService);
  private readonly resourceDataService = inject(ResourceDataService);
  private readonly router = inject(Router);

  protected readonly resources = signal<ResourceListItem[]>([]);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  public constructor() {
    this.resourceDataService.listResources().subscribe({
      next: (resources) => this.resources.set(resources),
      error: () => this.errorMessage.set('Unable to load resources.')
    });
  }

  protected createEvent(request: EventRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.eventDataService.createEvent(request).subscribe({
      next: () => void this.router.navigate(['/events']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to create the event.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/events']);
  }
}
