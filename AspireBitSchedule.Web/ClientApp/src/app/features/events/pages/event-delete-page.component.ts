import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { extractApiErrorMessage } from '../../../core/http/extract-api-error-message';
import { ToastService } from '../../../core/toast/toast.service';
import { EventDataService } from '../../../data-services/event-data.service';
import { EventModel } from '../models/event.models';

@Component({
  selector: 'app-event-delete-page',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './event-delete-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventDeletePageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly eventDataService = inject(EventDataService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly event = signal<EventModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitEventId = Number(this.route.snapshot.paramMap.get('bitEventId'));

  public constructor() {
    this.loadEvent();
    this.clientContext.clientChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadEvent());
  }

  protected deleteEvent(): void {
    this.deleting.set(true);
    this.errorMessage.set(null);

    this.eventDataService.deleteEvent(this.bitEventId).subscribe({
      next: () => {
        this.toastService.success('The event was removed successfully.', 'Event deleted');
        void this.router.navigate(['/events']);
      },
      error: (error: unknown) => {
        const message = extractApiErrorMessage(error, 'Unable to delete the event.');
        this.errorMessage.set(message);
        this.toastService.error(message, 'Unable to delete event');
        this.deleting.set(false);
      }
    });
  }

  private loadEvent(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.eventDataService.getEvent(this.bitEventId).subscribe({
      next: (event) => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: () => {
        const message = 'Unable to load the event.';
        this.errorMessage.set(message);
        this.toastService.error(message, 'Load failed');
        this.loading.set(false);
      }
    });
  }
}
