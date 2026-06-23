import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { EventDataService } from '../../../data-services/event-data.service';
import { ResourceDataService } from '../../../data-services/resource-data.service';
import { EventFormComponent } from '../components/event-form.component';
import { EventModel, EventRequest } from '../models/event.models';
import { ResourceListItem } from '../../resources/models/resource.models';

@Component({
  selector: 'app-event-edit-page',
  standalone: true,
  imports: [CommonModule, EventFormComponent],
  template: `
    @if (loading()) {
      <div class="card panel-card">
        <div class="card-body p-4">Loading event details...</div>
      </div>
    } @else {
      <app-event-form
        title="Update Event"
        submitLabel="Save Changes"
        [resources]="resources()"
        [initialValue]="event()"
        [saving]="saving()"
        [errorMessage]="errorMessage()"
        (save)="updateEvent($event)"
        (cancel)="navigateBack()" />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventEditPageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly eventDataService = inject(EventDataService);
  private readonly resourceDataService = inject(ResourceDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly resources = signal<ResourceListItem[]>([]);
  protected readonly event = signal<EventModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitEventId = Number(this.route.snapshot.paramMap.get('bitEventId'));

  public constructor() {
    this.loadData();
    this.clientContext.clientChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadData());
  }

  protected updateEvent(request: EventRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.eventDataService.updateEvent(this.bitEventId, request).subscribe({
      next: () => void this.router.navigate(['/events']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to update the event.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/events']);
  }

  private loadData(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.resourceDataService.listResources().subscribe({
      next: (resources) => this.resources.set(resources),
      error: () => this.errorMessage.set('Unable to load resources.')
    });

    this.eventDataService.getEvent(this.bitEventId).subscribe({
      next: (event) => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load the event.');
        this.loading.set(false);
      }
    });
  }
}
